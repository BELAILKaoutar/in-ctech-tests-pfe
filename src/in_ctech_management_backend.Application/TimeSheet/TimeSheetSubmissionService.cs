using in_ctech_management_backend.Application.Shared;
using in_ctech_management_backend.Application.TimeSheets.DTOs;
using in_ctech_management_backend.Domain;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.Holidays.Repositories;
using in_ctech_management_backend.Domain.LeaveRequests;
using in_ctech_management_backend.Domain.LeaveRequests.Enums;
using in_ctech_management_backend.Domain.TimeSheets;
using in_ctech_management_backend.Domain.TimeSheets.Submissions;
using in_ctech_management_backend.Domain.TimeSheets.Submissions.Enums;
using in_ctech_management_backend.Domain.TimeSheets.Submissions.Repositories;
// Alias pour éviter la collision avec les namespaces Application.LeaveRequest / Application.Holiday
using DomainLeaveRequest = in_ctech_management_backend.Domain.LeaveRequests.LeaveRequest;
using DomainHoliday = in_ctech_management_backend.Domain.Holidays.Holiday;

namespace in_ctech_management_backend.Application.TimeSheets
{
    public class TimeSheetSubmissionService : ITimeSheetSubmissionService
    {
        // Jour du mois à partir duquel la soumission est clôturée pour le collaborateur.
        private const int MonthlyClosingDay = 28;

        private readonly ITimeSheetSubmissionRepository _submissionRepository;
        private readonly ITimeSheetRepository _timeSheetRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeInfosService _employeeInfosService;
        private readonly IEmailService _emailService;
        private readonly ILeaveRequestRepository _leaveRequestRepository;
        private readonly IHolidayRepository _holidayRepository;

        public TimeSheetSubmissionService(
            ITimeSheetSubmissionRepository submissionRepository,
            ITimeSheetRepository timeSheetRepository,
            IEmployeeRepository employeeRepository,
            IEmployeeInfosService employeeInfosService,
            IEmailService emailService,
            ILeaveRequestRepository leaveRequestRepository,
            IHolidayRepository holidayRepository)
        {
            _submissionRepository = submissionRepository;
            _timeSheetRepository = timeSheetRepository;
            _employeeRepository = employeeRepository;
            _employeeInfosService = employeeInfosService;
            _emailService = emailService;
            _leaveRequestRepository = leaveRequestRepository;
            _holidayRepository = holidayRepository;
        }

        // ─── SUBMIT (collab) ──────────────────────────────────────────────
        public async Task<TimeSheetSubmissionDetailDto> SubmitMonthAsync(
            Guid employeeId,
            int year,
            int month,
            CancellationToken ct = default)
        {
            if (month < 1 || month > 12)
                throw new DomainException($"Mois invalide : {month}.");

            // Clôture mensuelle : à partir du 28, le collaborateur ne peut plus soumettre.
            if (DateTime.UtcNow.Day >= MonthlyClosingDay)
                throw new DomainException(
                    $"La saisie des feuilles de temps est clôturée à partir du {MonthlyClosingDay} du mois.");

            var employeeIdVo = new EmployeeId(employeeId);

            var sheets = await _timeSheetRepository.GetByEmployeeAndMonthAsync(employeeIdVo, year, month, ct);

            if (sheets.Count == 0)
                throw new DomainException("Aucune feuille de temps à soumettre pour ce mois.");

            var existing = await _submissionRepository.GetByEmployeeAndMonthAsync(employeeIdVo, year, month, ct)
                ?? throw new DomainException(
                    "Aucune feuille de temps n'a été sauvegardée pour ce mois.");

            TimeSheetSubmission submission;

            if (existing.Status == SubmissionStatus.Draft)
            {
                // Première soumission : Draft -> Pending
                existing.Submit(submittedBy: employeeId.ToString());
                submission = existing;
                await _submissionRepository.UpdateAsync(submission, ct);
            }
            else if (existing.Status == SubmissionStatus.ToCorrect)
            {
                // Re-soumission après correction
                existing.Resubmit(submittedBy: employeeId.ToString());
                submission = existing;
                await _submissionRepository.UpdateAsync(submission, ct);
            }
            else
            {
                throw new DomainException(existing.Status == SubmissionStatus.Pending
                        ? "Cette feuille a déjà été soumise et est en attente de validation."
                        : "Cette feuille a déjà été validée.");
            }

            // Verrouille toutes les feuilles du mois
            foreach (var sheet in sheets)
                sheet.Lock();

            await _timeSheetRepository.UpdateRangeAsync(sheets, ct);

            // Notification email au manager (best-effort, n'annule pas la soumission)
            await NotifyManagerAsync(employeeId, year, month, submission.SubmittedAt, ct);

            var projectNames = await GetProjectNamesAsync(employeeId, ct);

            var approvedLeaves = await _leaveRequestRepository.GetAsync(
                employeeId, LeaveRequestStatus.Approved, ct);
            var holidays = await _holidayRepository.GetByYearAsync(year, ct);

            var imputed = ComputeImputedDays(sheets);
            var leaveDays = ComputeLeaveDaysInMonth(year, month, approvedLeaves, holidays);
            var working = ComputeBusinessDaysInMonth(year, month, holidays);

            return TimeSheetSubmissionMapper.ToDetailDto(
                submission, sheets, projectNames, imputed, leaveDays, working);
        }

        // ─── LIST (manager) ───────────────────────────────────────────────
        public async Task<IReadOnlyList<TimeSheetSubmissionDetailDto>> GetByManagerAsync(
            Guid? managerId,
            int? month,
            Guid? employeeId,
            SubmissionStatus? status,
            CancellationToken ct = default)
        {
            if (month.HasValue && (month.Value < 1 || month.Value > 12))
                throw new DomainException($"Mois invalide : {month.Value}.");

            if (status == SubmissionStatus.Draft)
                throw new DomainException(
                    "Les brouillons ne sont pas accessibles au manager.");

            var year = DateTime.UtcNow.Year;

            var employeeIdVo = employeeId.HasValue ? new EmployeeId(employeeId.Value) : null;

            IReadOnlyList<TimeSheetSubmission> submissions;

            if (managerId.HasValue)
            {
                // Manager → filtre par son équipe uniquement
                submissions = await _submissionRepository.GetByManagerAsync(
                    new EmployeeId(managerId.Value), year, month, employeeIdVo, status, ct);
            }
            else
            {
                // Admin / RH → toutes les soumissions sans filtre manager
                submissions = await _submissionRepository.GetAllAsync(
                    year, month, employeeIdVo, status, ct);
            }


            if (submissions.Count == 0)
                return Array.Empty<TimeSheetSubmissionDetailDto>();

            var uniqueEmployeeIds = submissions
                .Select(s => (Guid)s.EmployeeId)
                .Distinct()
                .ToList();

            var uniqueMonths = submissions
                .Select(s => s.Month)
                .Distinct()
                .ToList();

            var names = await _employeeInfosService.GetEmployeeNamesByIdsAsync(uniqueEmployeeIds, ct);

            var projectNamesByEmployee = await _employeeRepository.GetProjectsByEmployeeIdsAsync(uniqueEmployeeIds, ct);

            // Bornes de chaque mois concerné (sert à la sélection par chevauchement et au clipping).
            var monthBounds = uniqueMonths.ToDictionary(
                m => m,
                m =>
                {
                    var start = new DateOnly(year, m, 1);
                    return (start, end: start.AddDays(DateTime.DaysInMonth(year, m) - 1));
                });

            // Récupération par chevauchement de dates (inclut les semaines à cheval),
            // sur la plage couvrant tous les mois demandés.
            var rangeStart = monthBounds.Values.Min(b => b.start);
            var rangeEnd = monthBounds.Values.Max(b => b.end);

            var allSheets = await _timeSheetRepository.GetByEmployeesAndDateRangeAsync(
                uniqueEmployeeIds, rangeStart, rangeEnd, ct);

            // Regroupe par employé ; l'affectation au mois se fait ensuite par chevauchement.
            var sheetsByEmployee = allSheets
                .GroupBy(s => (Guid)s.EmployeeId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<TimeSheet>)g.ToList());

            // Charge une fois les congés approuvés (tous les employés) et les fériés de l'année,
            // puis regroupe par employé pour O(1) à l'itération.
            var allApprovedLeaves = await _leaveRequestRepository.GetAsync(
                employeeId: null, LeaveRequestStatus.Approved, ct);
            var leavesByEmployee = allApprovedLeaves
                .Where(l => uniqueEmployeeIds.Contains((Guid)l.EmployeeId))
                .GroupBy(l => (Guid)l.EmployeeId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<DomainLeaveRequest>)g.ToList());

            var holidays = await _holidayRepository.GetByYearAsync(year, ct);

            // Pré-calcule les jours ouvrés par mois (indépendant du collab).
            var businessDaysByMonth = uniqueMonths
                .ToDictionary(m => m, m => ComputeBusinessDaysInMonth(year, m, holidays));

            var emptyProjects = new Dictionary<Guid, string>();
            var emptyLeaves = (IReadOnlyList<DomainLeaveRequest>)Array.Empty<DomainLeaveRequest>();

            return submissions
                .Select(submission =>
                {
                    var empGuid = (Guid)submission.EmployeeId;
                    var (mStart, mEnd) = monthBounds[submission.Month];

                    var empSheets = sheetsByEmployee.TryGetValue(empGuid, out var all)
                        ? all
                        : (IReadOnlyList<TimeSheet>)Array.Empty<TimeSheet>();

                    // Semaines chevauchant le mois de la soumission (même prédicat que côté collab).
                    var sheets = empSheets
                        .Where(s => s.WeekStartDate <= mEnd && s.WeekEndDate >= mStart)
                        .ToList();

                    var projects = projectNamesByEmployee.TryGetValue(empGuid, out var p)
                        ? p
                        : emptyProjects;
                    var leaves = leavesByEmployee.TryGetValue(empGuid, out var l)
                        ? l
                        : emptyLeaves;

                    var imputed = ComputeImputedDays(sheets, mStart, mEnd);
                    var leaveDays = ComputeLeaveDaysInMonth(year, submission.Month, leaves, holidays);
                    var working = businessDaysByMonth[submission.Month];

                    return TimeSheetSubmissionMapper.ToDetailDto(
                        submission,
                        sheets,
                        projects,
                        imputed,
                        leaveDays,
                        working,
                        names.GetValueOrDefault(empGuid));
                })
                .ToList();
        }

        // ─── LIST (collab) ────────────────────────────────────────────────
        public async Task<IReadOnlyList<TimeSheetSubmissionDetailDto>> GetByEmployeeAndMonthAsync(
            Guid employeeId,
            int month,
            CancellationToken ct = default)
        {
            if (month < 1 || month > 12)
                throw new DomainException($"Mois invalide : {month}.");

            var year = DateTime.UtcNow.Year;
            var employeeIdVo = new EmployeeId(employeeId);

            var submissions = await _submissionRepository.GetByEmployeeAsync(
                employeeIdVo, year, month, ct);

            if (submissions.Count == 0)
                return Array.Empty<TimeSheetSubmissionDetailDto>();

            // Récupération par chevauchement de dates : inclut les semaines à
            // cheval dont seuls quelques jours tombent dans le mois demandé.
            var monthStart = new DateOnly(year, month, 1);
            var monthEnd = monthStart.AddDays(DateTime.DaysInMonth(year, month) - 1);

            var sheets = await _timeSheetRepository.GetByEmployeeAndDateRangeAsync(
                employeeIdVo, monthStart, monthEnd, ct);

            var projectNames = await GetProjectNamesAsync(employeeId, ct);

            var names = await _employeeInfosService.GetEmployeeNamesByIdsAsync(
                new List<Guid> { employeeId }, ct);

            var employeeName = names.GetValueOrDefault(employeeId);

            var approvedLeaves = await _leaveRequestRepository.GetAsync(
                employeeId, LeaveRequestStatus.Approved, ct);
            var holidays = await _holidayRepository.GetByYearAsync(year, ct);

            var imputed = ComputeImputedDays(sheets, monthStart, monthEnd);
            var leaveDays = ComputeLeaveDaysInMonth(year, month, approvedLeaves, holidays);
            var working = ComputeBusinessDaysInMonth(year, month, holidays);

            return submissions
                .Select(s => TimeSheetSubmissionMapper.ToDetailDto(
                    s, sheets, projectNames, imputed, leaveDays, working, employeeName))
                .ToList();
        }

        // ─── DETAIL ───────────────────────────────────────────────────────
        public async Task<TimeSheetSubmissionDetailDto> GetByIdAsync(
            Guid submissionId,
            CancellationToken ct = default)
        {
            var submission = await _submissionRepository.GetByIdAsync(new SubmissionId(submissionId), ct) ?? throw new DomainException("Soumission introuvable.");

            // Récupération par chevauchement de dates (cf. GetByEmployeeAndMonthAsync) :
            // inclut les semaines à cheval rattachées au mois de la soumission.
            var monthStart = new DateOnly(submission.Year, submission.Month, 1);
            var monthEnd = monthStart.AddDays(DateTime.DaysInMonth(submission.Year, submission.Month) - 1);

            var sheets = await _timeSheetRepository.GetByEmployeeAndDateRangeAsync(
                submission.EmployeeId, monthStart, monthEnd, ct);

            var projectNames = await GetProjectNamesAsync((Guid)submission.EmployeeId, ct);

            var names = await _employeeInfosService.GetEmployeeNamesByIdsAsync(
                new List<Guid> { (Guid)submission.EmployeeId }, ct);

            var approvedLeaves = await _leaveRequestRepository.GetAsync(
                (Guid)submission.EmployeeId, LeaveRequestStatus.Approved, ct);
            var holidays = await _holidayRepository.GetByYearAsync(submission.Year, ct);

            var imputed = ComputeImputedDays(sheets, monthStart, monthEnd);
            var leaveDays = ComputeLeaveDaysInMonth(submission.Year, submission.Month, approvedLeaves, holidays);
            var working = ComputeBusinessDaysInMonth(submission.Year, submission.Month, holidays);

            return TimeSheetSubmissionMapper.ToDetailDto(
                submission,
                sheets,
                projectNames,
                imputed,
                leaveDays,
                working,
                names.GetValueOrDefault((Guid)submission.EmployeeId));
        }

        // ─── UPDATE STATUS (manager) ──────────────────────────────────────
        public async Task<TimeSheetSubmissionDto> UpdateStatusAsync(
            Guid submissionId,
            string reviewedBy,
            UpdateSubmissionStatusDto dto,
            CancellationToken ct = default)
        {
            var submission = await _submissionRepository.GetByIdAsync(
                new SubmissionId(submissionId), ct)
                ?? throw new DomainException("Soumission introuvable.");

            switch (dto.Status)
            {
                case SubmissionStatus.Approved:
                    submission.Approve(reviewedBy);
                    await _submissionRepository.UpdateAsync(submission, ct);
                    break;

                case SubmissionStatus.ToCorrect:
                    if (string.IsNullOrWhiteSpace(dto.Reason))
                        throw new DomainException(
                            "Le motif est obligatoire pour marquer comme À corriger.");

                    submission.MarkToCorrect(reviewedBy, dto.Reason);
                    await _submissionRepository.UpdateAsync(submission, ct);

                    // Déverrouille toutes les feuilles du mois pour permettre la correction
                    var sheets = await _timeSheetRepository.GetByEmployeeAndMonthAsync(submission.EmployeeId, submission.Year, submission.Month, ct);

                    foreach (var sheet in sheets)
                        sheet.Unlock();

                    await _timeSheetRepository.UpdateRangeAsync(sheets, ct);

                    await NotifyCollaboratorToCorrectAsync(submission, dto.Reason, ct);
                    break;

                default:
                    throw new DomainException($"Statut cible non supporté : {dto.Status}.");
            }

            return TimeSheetSubmissionMapper.ToDto(submission);
        }

        // ─── HELPERS ──────────────────────────────────────────────────────
        private async Task NotifyCollaboratorToCorrectAsync(
            TimeSheetSubmission submission,
            string reason,
            CancellationToken ct)
        {
            var info = await _employeeInfosService.GetCollaboratorEmailAsync((Guid)submission.EmployeeId, ct);

            if (string.IsNullOrWhiteSpace(info.Email))
                return;

            await _emailService.SendTimeSheetToCorrectEmailAsync(
                info.Email,
                info.FullName ?? "N/A",
                submission.Year,
                submission.Month,
                submission.ReviewedAt ?? DateTime.UtcNow,
                reason);
        }

        private async Task NotifyManagerAsync(
            Guid employeeId,
            int year,
            int month,
            DateTime submittedAt,
            CancellationToken ct)
        {
            var info = await _employeeInfosService.GetCollaboratorWithManagerEmailAsync(employeeId, ct);

            if (string.IsNullOrWhiteSpace(info.ManagerEmail))
                return;

            await _emailService.SendTimeSheetSubmittedEmailAsync(
                info.ManagerEmail,
                info.FullName ?? "N/A",
                info.Email ?? "N/A",
                year,
                month,
                submittedAt);
        }

        private async Task<Dictionary<Guid, string>> GetProjectNamesAsync(
            Guid employeeId,
            CancellationToken ct)
        {
            var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), ct);

            return employee?.Projects.ToDictionary(p => (Guid)p.ProjectId, p => p.Name) ?? new Dictionary<Guid, string>();
        }

        // ─── METRICS ──────────────────────────────────────────────────────
        private static decimal ComputeImputedDays(IEnumerable<TimeSheet> sheets)
            => sheets.Sum(s => s.Entries.Sum(e => e.TotalDays));

        // Jours imputés dont la date tombe dans [from, to].
        // Sert à clipper les semaines à cheval sur deux mois : une feuille
        // récupérée par chevauchement ne compte que ses jours du mois courant.
        private static decimal ComputeImputedDays(
            IEnumerable<TimeSheet> sheets, DateOnly from, DateOnly to)
            => sheets.Sum(s => s.Entries.Sum(e => e.DailyValues
                .Where(kv => kv.Key >= from && kv.Key <= to)
                .Sum(kv => kv.Value)));

        // Jours de congé approuvés du collab qui tombent dans le mois (lun-ven, hors fériés).
        // Tient compte des demi-journées via StartPeriod / EndPeriod.
        private static decimal ComputeLeaveDaysInMonth(
            int year,
            int month,
            IEnumerable<DomainLeaveRequest> approvedLeaves,
            IEnumerable<DomainHoliday> holidays)
        {
            var monthStart = new DateOnly(year, month, 1);
            var monthEnd = monthStart.AddDays(DateTime.DaysInMonth(year, month) - 1);

            var holidayDates = holidays
                .Select(h => h.Date)
                .Where(d => d >= monthStart && d <= monthEnd)
                .ToHashSet();

            decimal total = 0m;

            foreach (var leave in approvedLeaves)
            {
                if (leave.EndDate < monthStart || leave.StartDate > monthEnd)
                    continue;

                var rangeStart = leave.StartDate < monthStart ? monthStart : leave.StartDate;
                var rangeEnd = leave.EndDate > monthEnd ? monthEnd : leave.EndDate;

                for (var d = rangeStart; d <= rangeEnd; d = d.AddDays(1))
                {
                    if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                        continue;
                    if (holidayDates.Contains(d))
                        continue;

                    decimal dayValue = 1m;

                    if (d == leave.StartDate && leave.StartPeriod == DayPeriod.Afternoon)
                        dayValue -= 0.5m;

                    if (d == leave.EndDate && leave.EndPeriod == DayPeriod.Morning)
                        dayValue -= 0.5m;

                    total += dayValue;
                }
            }

            return total;
        }

        // Jours ouvrés (lun-ven) du mois - jours fériés tombant un jour ouvré.
        private static int ComputeBusinessDaysInMonth(
            int year,
            int month,
            IEnumerable<DomainHoliday> holidays)
        {
            var monthStart = new DateOnly(year, month, 1);
            var monthEnd = monthStart.AddDays(DateTime.DaysInMonth(year, month) - 1);

            var holidayDates = holidays
                .Select(h => h.Date)
                .Where(d => d >= monthStart && d <= monthEnd)
                .ToHashSet();

            int count = 0;
            for (var d = monthStart; d <= monthEnd; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                    continue;
                if (holidayDates.Contains(d))
                    continue;
                count++;
            }
            return count;
        }
    }
}
