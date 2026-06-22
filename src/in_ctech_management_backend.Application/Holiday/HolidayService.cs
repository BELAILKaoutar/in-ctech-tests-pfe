using in_ctech_management_backend.Application.Holiday.DTOs;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.Holidays;
using in_ctech_management_backend.Domain.Holidays.Repositories;
using in_ctech_management_backend.Domain.LeaveRequests;
using in_ctech_management_backend.Domain.LeaveRequests.Enums;
using in_ctech_management_backend.Domain.TimeSheets;
using DomainLeaveRequest = in_ctech_management_backend.Domain.LeaveRequests.LeaveRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.Holiday
{
    public class HolidayService : IHolidayService
    {
        private readonly IHolidayRepository _repository;
        private readonly ITimeSheetRepository _timeSheetRepository;
        private readonly ILeaveRequestRepository _leaveRequestRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public HolidayService(
            IHolidayRepository repository,
            ITimeSheetRepository timeSheetRepository,
            ILeaveRequestRepository leaveRequestRepository,
            IEmployeeRepository employeeRepository)
        {
            _repository = repository;
            _timeSheetRepository = timeSheetRepository;
            _leaveRequestRepository = leaveRequestRepository;
            _employeeRepository = employeeRepository;
        }

        public async Task<Guid> CreateAsync(CreateHolidayRequest dto, CancellationToken cancellationToken = default)
        {
            // Vérifier unicité de la date
            var existing = await _repository.GetByDateAsync(dto.Date, cancellationToken);
            if (existing != null)
                throw new Exceptions.ApplicationException($"Un jour férié existe déjà à cette date");

            var holiday = Domain.Holidays.Holiday.Create(
                dto.Title,
                dto.Date
            );

            await _repository.AddAsync(holiday, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            // ── Écraser les imputations existantes sur ce jour ────────────────
            await ClearTimesheetEntriesForHolidayAsync(dto.Date, cancellationToken);
            // ─────────────────────────────────────────────────────────────────

            // ── Recalculer les demandes de congé qui chevauchent ce jour ──────
            await RecalculateLeaveRequestsForDateAsync(dto.Date, cancellationToken);
            // ─────────────────────────────────────────────────────────────────

            return holiday.HolidayId.Value;
        }

        public async Task<HolidayDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var holiday = await _repository.GetByIdAsync(new HolidayId(id), cancellationToken);
            return holiday == null ? null : MapToDto(holiday);
        }

        public async Task<List<HolidayDto>> GetAllAsync(string? title = null, DateOnly? date = null, CancellationToken cancellationToken = default)
        {
            // Récupérer par année si date fournie, sinon tout récupérer
            var holidays = date.HasValue
                ? await _repository.GetByYearAsync(date.Value.Year, cancellationToken)
                : await _repository.GetAllAsync(cancellationToken);

            // Filtrer par titre
            if (!string.IsNullOrWhiteSpace(title))
                holidays = holidays
                    .Where(h => h.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            // Filtrer par date exacte si fournie
            if (date.HasValue)
                holidays = holidays
                    .Where(h => h.Date == date.Value)
                    .ToList();

            return holidays.Select(MapToDto).ToList();
        }

        public async Task UpdateAsync(Guid id,UpdateHolidayRequest dto,CancellationToken cancellationToken = default)
        {
            var holiday = await _repository.GetByIdAsync(new HolidayId(id), cancellationToken);
            if (holiday == null)
                throw new ApplicationException($"Holiday with id '{id}' not found");

            var oldDate = holiday.Date;

            holiday.Update(
                dto.Title ?? holiday.Title,
                dto.Date ?? holiday.Date);

            await _repository.UpdateAsync(holiday, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            // ── Si la date a changé, écraser les imputations sur la nouvelle date
            // (l'ancienne date redevient éditable — rien à faire côté timesheet)
            if (dto.Date.HasValue && dto.Date.Value != oldDate)
            {
                await ClearTimesheetEntriesForHolidayAsync(dto.Date.Value, cancellationToken);

                // Recalcul des demandes de congé : ancienne date (jour redevenu ouvré)
                // et nouvelle date (jour devenu férié).
                await RecalculateLeaveRequestsForDateAsync(oldDate, cancellationToken);
                await RecalculateLeaveRequestsForDateAsync(dto.Date.Value, cancellationToken);
            }
            // ─────────────────────────────────────────────────────────────────
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var holiday = await _repository.GetByIdAsync(new HolidayId(id), cancellationToken);
            if (holiday == null)
                throw new Exceptions.ApplicationException($"Holiday with id '{id}' not found");

            var deletedDate = holiday.Date;

            await _repository.DeleteAsync(new HolidayId(id), cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            // Le jour redevient ouvré : recalculer les demandes de congé qui le couvraient.
            await RecalculateLeaveRequestsForDateAsync(deletedDate, cancellationToken);
        }

        private static HolidayDto MapToDto(Domain.Holidays.Holiday h)
        {
            return new HolidayDto(
                h.HolidayId.Value,
                h.Title,
                h.Date,
                h.CreatedAt,
                h.UpdatedAt,
                h.CreatedBy,
                h.UpdatedBy
            );
        }

        private async Task ClearTimesheetEntriesForHolidayAsync(
            DateOnly date,
            CancellationToken cancellationToken)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday)
                return;

            // Récupère toutes les feuilles de tous les employés qui couvrent ce jour
            var affectedSheets = await _timeSheetRepository.GetAllByDateRangeAsync(
                date, date, cancellationToken);

            if (affectedSheets.Count == 0)
                return;

            foreach (var sheet in affectedSheets)
                sheet.ClearDaysForLeave(new[] { date });

            await _timeSheetRepository.UpdateRangeAsync(affectedSheets, cancellationToken);
        }

        // Recalcule NumberOfDays pour toutes les demandes de congé actives (Pending/Approved,
        // non supprimées) qui chevauchent la date donnée, en utilisant l'état courant des
        // jours fériés (IsDeleted=false) et des congés approuvés. Ajuste le solde de l'employé
        // pour les demandes Approved dont NumberOfDays a changé.
        private async Task RecalculateLeaveRequestsForDateAsync(
            DateOnly date,
            CancellationToken cancellationToken)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday)
                return;

            var impacted = await _leaveRequestRepository.GetActiveOverlappingDateAsync(date, cancellationToken);
            if (impacted.Count == 0)
                return;

            // Cache jours fériés par année pour éviter les requêtes répétées.
            var holidaysByYear = new Dictionary<int, List<Domain.Holidays.Holiday>>();

            const string updatedBy = "System";
            var updatedLeaves = new List<DomainLeaveRequest>();

            foreach (var leave in impacted)
            {
                var excluded = await BuildExcludedDatesAsync(
                    leave,
                    holidaysByYear,
                    cancellationToken);

                var delta = leave.Recalculate(excluded, updatedBy);
                if (delta == 0m)
                    continue;

                updatedLeaves.Add(leave);

                if (leave.Status == LeaveRequestStatus.Approved)
                {
                    var employee = await _employeeRepository.GetByIdAsync(
                        new EmployeeId((Guid)leave.EmployeeId),
                        cancellationToken);

                    if (employee is null)
                        continue;

                    employee.AdjustConsumedLeaves(delta);
                    await _employeeRepository.UpdateAsync(employee, cancellationToken);
                }
            }

            if (updatedLeaves.Count > 0)
                await _leaveRequestRepository.UpdateRangeAsync(updatedLeaves, cancellationToken);
        }

        private async Task<IReadOnlySet<DateOnly>> BuildExcludedDatesAsync(
            DomainLeaveRequest leave,
            Dictionary<int, List<Domain.Holidays.Holiday>> holidaysByYear,
            CancellationToken cancellationToken)
        {
            var excluded = new HashSet<DateOnly>();

            for (var year = leave.StartDate.Year; year <= leave.EndDate.Year; year++)
            {
                if (!holidaysByYear.TryGetValue(year, out var yearHolidays))
                {
                    yearHolidays = await _repository.GetByYearAsync(year, cancellationToken);
                    holidaysByYear[year] = yearHolidays;
                }

                foreach (var h in yearHolidays)
                    if (h.Date >= leave.StartDate && h.Date <= leave.EndDate)
                        excluded.Add(h.Date);
            }

            // Inclure les autres congés approuvés qui chevauchent (hors la demande courante).
            var approvedSameEmployee = await _leaveRequestRepository.GetAsync(
                (Guid)leave.EmployeeId,
                LeaveRequestStatus.Approved,
                cancellationToken);

            foreach (var other in approvedSameEmployee)
            {
                if (other.LeaveRequestId == leave.LeaveRequestId)
                    continue;
                if (other.EndDate < leave.StartDate || other.StartDate > leave.EndDate)
                    continue;

                for (var d = other.StartDate; d <= other.EndDate; d = d.AddDays(1))
                    if (d >= leave.StartDate && d <= leave.EndDate)
                        excluded.Add(d);
            }

            return excluded;
        }
    }
}
