using in_ctech_management_backend.Application.LeaveRequest.DTOs;
using in_ctech_management_backend.Application.Shared;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.Holidays.Repositories;
using in_ctech_management_backend.Domain.LeaveRequests;
using in_ctech_management_backend.Domain.LeaveRequests.Enums;
using in_ctech_management_backend.Domain.TimeSheets;
using DomainLeaveRequest = in_ctech_management_backend.Domain.LeaveRequests.LeaveRequest;

namespace in_ctech_management_backend.Application.LeaveRequest
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly ILeaveRequestRepository _leaveRequestRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmailService _emailService;
        private readonly IEmployeeInfosService _employeeInfosService;
        private readonly ITimeSheetRepository _timeSheetRepository;
        private readonly IHolidayRepository _holidayRepository;

        public LeaveRequestService(
            ILeaveRequestRepository leaveRequestRepository,
            IEmployeeRepository employeeRepository,
            IEmailService emailService,
            IEmployeeInfosService employeeInfosService,
            ITimeSheetRepository timeSheetRepository,
            IHolidayRepository holidayRepository)
        {
            _leaveRequestRepository = leaveRequestRepository;
            _employeeRepository = employeeRepository;
            _emailService = emailService;
            _employeeInfosService = employeeInfosService;
            _timeSheetRepository = timeSheetRepository;
            _holidayRepository = holidayRepository;
        }

        public async Task<IReadOnlyList<LeaveRequestDto>> GetLeaveRequestsAsync(
            Guid? employeeId,
            LeaveRequestStatus? status,
            CancellationToken cancellationToken = default)
        {
            var leaveRequests = await _leaveRequestRepository.GetAsync(
                employeeId,
                status,
                cancellationToken);

            return leaveRequests
                .Select(lr => LeaveRequestMapper.ToDto(lr))
                .ToList();
        }

        public async Task<IReadOnlyList<LeaveRequestDto>> GetLeaveRequestsByManagerAsync(
            Guid? managerId,          // ← nullable : null = RH/Admin voit tout
            Guid? employeeId,
            LeaveRequestStatus? status,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<DomainLeaveRequest> leaveRequests;

            if (managerId.HasValue)
            {
                // Manager → filtre par son équipe uniquement
                leaveRequests = await _leaveRequestRepository.GetByManagerAsync(
                    managerId.Value,
                    employeeId,
                    status,
                    cancellationToken);
            }
            else
            {
                // RH / Admin → toutes les demandes sans filtre manager
                leaveRequests = await _leaveRequestRepository.GetAsync(
                    employeeId,   // filtre optionnel par employé spécifique
                    status,
                    cancellationToken);
            }

            var employeeIds = leaveRequests
                .Select(lr => (Guid)lr.EmployeeId)
                .Distinct()
                .ToList();

            var employeeSummaries = await _employeeInfosService.GetEmployeeSummariesByIdsAsync(
                employeeIds,
                cancellationToken);

            return leaveRequests
                .Select(lr => LeaveRequestMapper.ToDto(
                    lr,
                    employeeSummaries.GetValueOrDefault((Guid)lr.EmployeeId)))
                .ToList();
        }


        public async Task<LeaveRequestDto> CreateAsync(
            Guid employeeId,
            CreateLeaveRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("La raison de la demande est obligatoire");

            if (dto.StartDate > dto.EndDate)
                throw new ArgumentException("La date de début ne peut pas être après la date de fin.");

            var overlappingActive = await GetActiveOverlappingLeavesAsync(
                employeeId,
                dto.StartDate,
                dto.EndDate,
                excludeLeaveId: null,
                cancellationToken);

            EnsureNoActiveOverlap(overlappingActive);

            var excludedDates = await BuildExcludedDatesAsync(
                dto.StartDate,
                dto.EndDate,
                overlappingActive,
                cancellationToken);

            var leaveRequest = LeaveRequestMapper.ToEntity(employeeId, dto, excludedDates);

            await _leaveRequestRepository.CreateLeaveRequestAsync(leaveRequest, cancellationToken);

            var employeeInfo = await _employeeInfosService.GetCollaboratorWithManagerEmailAsync(employeeId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(employeeInfo.ManagerEmail))
            {
                await _emailService.SendLeaveRequestCreatedEmailAsync(
                    employeeInfo.ManagerEmail,
                    employeeInfo.FullName ?? "N/A",
                    employeeInfo.Email ?? "N/A",
                    LeaveRequestMapper.GetLeaveTypeLabel(leaveRequest.LeaveType),
                    leaveRequest.StartDate,
                    leaveRequest.EndDate,
                    leaveRequest.NumberOfDays,
                    LeaveRequestMapper.GetStatusLabel(leaveRequest.Status));
            }

            return LeaveRequestMapper.ToDto(leaveRequest);
        }

        public async Task<LeaveRequestPreviewDto> PreviewAsync(
            Guid employeeId,
            PreviewLeaveRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto.StartDate > dto.EndDate)
                throw new ArgumentException("La date de début ne peut pas être après la date de fin.");

            var overlappingActive = await GetActiveOverlappingLeavesAsync(
                employeeId,
                dto.StartDate,
                dto.EndDate,
                excludeLeaveId: null,
                cancellationToken);

            var holidayDates = await GetHolidayDatesInRangeAsync(
                dto.StartDate,
                dto.EndDate,
                cancellationToken);

            var overlappingDates = new HashSet<DateOnly>();
            foreach (var lr in overlappingActive)
                for (var d = lr.StartDate; d <= lr.EndDate; d = d.AddDays(1))
                    if (d >= dto.StartDate && d <= dto.EndDate)
                        overlappingDates.Add(d);

            var numberOfDays = DomainLeaveRequest.PreviewNumberOfDays(
                dto.StartDate,
                dto.EndDate,
                dto.StartPeriod,
                dto.EndPeriod,
                holidayDates);

            return new LeaveRequestPreviewDto
            {
                NumberOfDays = numberOfDays,
                HolidayDates = holidayDates.OrderBy(d => d).ToList(),
                OverlappingApprovedDates = overlappingDates.OrderBy(d => d).ToList()
            };
        }

        private async Task<HashSet<DateOnly>> GetHolidayDatesInRangeAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken)
        {
            var dates = new HashSet<DateOnly>();
            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                var holidays = await _holidayRepository.GetByYearAsync(year, cancellationToken);
                foreach (var h in holidays)
                    if (h.Date >= startDate && h.Date <= endDate)
                        dates.Add(h.Date);
            }
            return dates;
        }

        public async Task<LeaveRequestDto> UpdateByCollaboratorAsync(
            Guid leaveRequestId,
            Guid employeeId,
            CreateLeaveRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("La raison de la demande est obligatoire");

            if (dto.StartDate > dto.EndDate)
                throw new ArgumentException("La date de début ne peut pas être après la date de fin.");

            var leaveRequest = await _leaveRequestRepository.GetByIdAsync(
                new LeaveRequestId(leaveRequestId),
                cancellationToken);

            if (leaveRequest is null)
                throw new ArgumentException("Demande de congé introuvable.");

            if ((Guid)leaveRequest.EmployeeId != employeeId)
                throw new UnauthorizedAccessException("Cette demande de congé n'appartient pas au collaborateur connecté.");

            if (leaveRequest.Status != LeaveRequestStatus.Pending)
                throw new InvalidOperationException("Seules les demandes en attente peuvent être modifiées.");

            var overlappingActive = await GetActiveOverlappingLeavesAsync(
                employeeId,
                dto.StartDate,
                dto.EndDate,
                excludeLeaveId: leaveRequest.LeaveRequestId,
                cancellationToken);

            EnsureNoActiveOverlap(overlappingActive);

            var excludedDates = await BuildExcludedDatesAsync(
                dto.StartDate,
                dto.EndDate,
                overlappingActive,
                cancellationToken);

            leaveRequest.UpdateByCollaborator(
                dto.LeaveType,
                dto.StartDate,
                dto.EndDate,
                dto.StartPeriod,
                dto.EndPeriod,
                dto.Reason,
                updatedBy: employeeId.ToString(),
                excludedDates: excludedDates);

            await _leaveRequestRepository.UpdateAsync(leaveRequest, cancellationToken);

            var employeeInfo = await _employeeInfosService.GetCollaboratorWithManagerEmailAsync(employeeId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(employeeInfo.ManagerEmail))
            {
                await _emailService.SendLeaveRequestUpdatedEmailAsync(
                    employeeInfo.ManagerEmail,
                    employeeInfo.FullName ?? "N/A",
                    employeeInfo.Email ?? "N/A",
                    LeaveRequestMapper.GetLeaveTypeLabel(leaveRequest.LeaveType),
                    leaveRequest.StartDate,
                    leaveRequest.EndDate,
                    leaveRequest.NumberOfDays,
                    LeaveRequestMapper.GetStatusLabel(leaveRequest.Status));
            }

            return LeaveRequestMapper.ToDto(leaveRequest);
        }

        public async Task<LeaveRequestDto> CancelAsync(
            Guid leaveRequestId,
            Guid employeeId,
            CancellationToken cancellationToken = default)
        {
            var leaveRequest = await _leaveRequestRepository.GetByIdAsync(
                new LeaveRequestId(leaveRequestId),
                cancellationToken);

            if (leaveRequest is null)
                throw new ArgumentException("Demande de congé introuvable.");

            if ((Guid)leaveRequest.EmployeeId != employeeId)
                throw new UnauthorizedAccessException("Cette demande de congé n'appartient pas au collaborateur connecté.");

            if (leaveRequest.Status != LeaveRequestStatus.Pending)
                throw new InvalidOperationException("Seules les demandes en attente peuvent être annulées.");

            leaveRequest.Cancel(updatedBy: employeeId.ToString());

            await _leaveRequestRepository.UpdateAsync(leaveRequest, cancellationToken);

            var employeeInfo = await _employeeInfosService.GetCollaboratorWithManagerEmailAsync(employeeId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(employeeInfo.ManagerEmail))
            {
                await _emailService.SendLeaveRequestCancelledEmailAsync(
                    employeeInfo.ManagerEmail,
                    employeeInfo.FullName ?? "N/A",
                    employeeInfo.Email ?? "N/A",
                    LeaveRequestMapper.GetLeaveTypeLabel(leaveRequest.LeaveType),
                    leaveRequest.StartDate,
                    leaveRequest.EndDate,
                    leaveRequest.NumberOfDays);
            }

            return LeaveRequestMapper.ToDto(leaveRequest);
        }

        // Demandes Pending+Approved du même employé qui chevauchent la période donnée, en option
        // sans la demande dont l'ID est passé (cas de l'update où on s'exclut soi-même).
        private async Task<List<DomainLeaveRequest>> GetActiveOverlappingLeavesAsync(
            Guid employeeId,
            DateOnly startDate,
            DateOnly endDate,
            LeaveRequestId? excludeLeaveId,
            CancellationToken cancellationToken)
        {
            var pending = await _leaveRequestRepository.GetAsync(employeeId, LeaveRequestStatus.Pending, cancellationToken);
            var approved = await _leaveRequestRepository.GetAsync(employeeId, LeaveRequestStatus.Approved, cancellationToken);

            return pending.Concat(approved)
                .Where(lr => lr.StartDate <= endDate
                          && lr.EndDate >= startDate
                          && (excludeLeaveId is null || lr.LeaveRequestId != excludeLeaveId))
                .ToList();
        }

        private static void EnsureNoActiveOverlap(IReadOnlyList<DomainLeaveRequest> overlappingActive)
        {
            if (overlappingActive.Count == 0)
                return;

            var hasApproved = overlappingActive.Any(lr => lr.Status == LeaveRequestStatus.Approved);
            throw new ArgumentException(hasApproved
                ? "Cet employé a déjà un congé approuvé qui chevauche cette période."
                : "Cet employé a déjà une demande de congé en attente qui chevauche cette période.");
        }

        public async Task<LeaveRequestDto> UpdateStatusAsync(
            Guid leaveRequestId,
            string updatedBy,
            UpdateLeaveRequestStatusDto dto,
            CancellationToken cancellationToken = default)
        {
            var leaveRequest = await _leaveRequestRepository.GetByIdAsync(
                new LeaveRequestId(leaveRequestId),
                cancellationToken);

            if (leaveRequest is null)
                throw new Exception("Demande de congé introuvable.");

            var employeeInfo = await _employeeInfosService.GetCollaboratorEmailAsync(
                (Guid)leaveRequest.EmployeeId,
                cancellationToken);

            switch (dto.Status)
            {
                case LeaveRequestStatus.Approved:
                    await HandleApproveAsync(leaveRequest, updatedBy, employeeInfo);
                    break;

                case LeaveRequestStatus.Rejected:
                    await HandleRejectAsync(leaveRequest, updatedBy, dto, employeeInfo);
                    break;

                default:
                    throw new ArgumentException("Statut invalide.");
            }

            await _leaveRequestRepository.UpdateAsync(leaveRequest, cancellationToken);

            return LeaveRequestMapper.ToDto(leaveRequest);
        }

        // ─── Private Handlers ────────────────────────────────────────────────────────

        private async Task HandleApproveAsync(
            DomainLeaveRequest leaveRequest,
            string updatedBy,
            EmployeeInfosDto employeeInfo)
        {
            leaveRequest.Approve(updatedBy);

            var employee = await _employeeRepository.GetByIdAsync(
                new EmployeeId((Guid)leaveRequest.EmployeeId));

            if (employee is null)
                throw new InvalidOperationException("Employé introuvable.");

            employee.AddConsumedLeaves(leaveRequest.NumberOfDays);
            await _employeeRepository.UpdateAsync(employee);

            // ── Écraser les imputations sur les jours du congé ────────────────
            await ClearTimesheetEntriesForLeaveAsync(leaveRequest);
            // ─────────────────────────────────────────────────────────────────

            if (!string.IsNullOrWhiteSpace(employeeInfo.Email))
            {
                await _emailService.SendLeaveRequestApprovedEmailAsync(
                    employeeInfo.Email,
                    employeeInfo.FullName ?? "N/A",
                    leaveRequest.LeaveType,
                    leaveRequest.StartDate,
                    leaveRequest.EndDate,
                    leaveRequest.NumberOfDays);
            }
        }

        private async Task ClearTimesheetEntriesForLeaveAsync(DomainLeaveRequest leaveRequest)
        {
            var leaveDays = GetWorkingDays(leaveRequest.StartDate, leaveRequest.EndDate).ToList();

            if (leaveDays.Count == 0)
                return;

            var affectedSheets = await _timeSheetRepository.GetByEmployeeAndDateRangeAsync(
                new EmployeeId((Guid)leaveRequest.EmployeeId),
                leaveDays.First(),
                leaveDays.Last());

            if (affectedSheets.Count == 0)
                return;

            foreach (var sheet in affectedSheets)
                sheet.ClearDaysForLeave(leaveDays);

            await _timeSheetRepository.UpdateRangeAsync(affectedSheets);
        }

        private static IEnumerable<DateOnly> GetWorkingDays(DateOnly start, DateOnly end)
        {
            for (var d = start; d <= end; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    yield return d;
        }

        private async Task<IReadOnlySet<DateOnly>> BuildExcludedDatesAsync(
            DateOnly startDate,
            DateOnly endDate,
            IEnumerable<DomainLeaveRequest> approvedOverlaps,
            CancellationToken cancellationToken)
        {
            var excluded = new HashSet<DateOnly>();

            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                var holidays = await _holidayRepository.GetByYearAsync(year, cancellationToken);
                foreach (var h in holidays)
                    if (h.Date >= startDate && h.Date <= endDate)
                        excluded.Add(h.Date);
            }

            foreach (var lr in approvedOverlaps)
                for (var d = lr.StartDate; d <= lr.EndDate; d = d.AddDays(1))
                    if (d >= startDate && d <= endDate)
                        excluded.Add(d);

            return excluded;
        }

        private async Task HandleRejectAsync(
            DomainLeaveRequest leaveRequest,
            string updatedBy,
            UpdateLeaveRequestStatusDto dto,
            EmployeeInfosDto employeeInfo)
        {
            if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                throw new ArgumentException("Le motif de refus est obligatoire.");

            leaveRequest.Reject(updatedBy, dto.RejectionReason);

            if (!string.IsNullOrWhiteSpace(employeeInfo.Email))
            {
                await _emailService.SendLeaveRequestRejectedEmailAsync(
                    employeeInfo.Email,
                    employeeInfo.FullName ?? "N/A",
                    leaveRequest.LeaveType,
                    leaveRequest.StartDate,
                    leaveRequest.EndDate,
                    leaveRequest.NumberOfDays,
                    dto.RejectionReason);
            }
        }
    }
}