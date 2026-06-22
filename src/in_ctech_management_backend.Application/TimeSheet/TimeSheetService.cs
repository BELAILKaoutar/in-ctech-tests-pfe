using in_ctech_management_backend.Application.TimeSheets.DTOs;
using in_ctech_management_backend.Domain;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.Holidays.Repositories;
using in_ctech_management_backend.Domain.LeaveRequests;
using in_ctech_management_backend.Domain.LeaveRequests.Enums;
using in_ctech_management_backend.Domain.TimeSheets;
using in_ctech_management_backend.Domain.TimeSheets.Submissions;
using in_ctech_management_backend.Domain.TimeSheets.Submissions.Repositories;

namespace in_ctech_management_backend.Application.TimeSheets
{
    public class TimeSheetService : ITimeSheetService
    {
        private readonly ITimeSheetRepository _timeSheetRepository;
        private readonly IHolidayRepository _holidayRepository;
        private readonly ILeaveRequestRepository _leaveRequestRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ITimeSheetSubmissionRepository _submissionRepository;

        public TimeSheetService(
            ITimeSheetRepository timeSheetRepository,
            IHolidayRepository holidayRepository,
            ILeaveRequestRepository leaveRequestRepository,
            IEmployeeRepository employeeRepository,
            ITimeSheetSubmissionRepository submissionRepository)
        {
            _timeSheetRepository = timeSheetRepository;
            _holidayRepository = holidayRepository;
            _leaveRequestRepository = leaveRequestRepository;
            _employeeRepository = employeeRepository;
            _submissionRepository = submissionRepository;
        }

        // ─── GET ──────────────────────────────────────────────────────────────
        public async Task<TimeSheetDto> GetAsync(
            Guid employeeId,
            int year,
            int weekNumber,
            CancellationToken cancellationToken = default)
        {
            // Lecture : aucune restriction de période, on peut consulter
            // les feuilles de temps des semaines passées comme à venir.

            var (weekStart, weekEnd, month) = GetWeekBounds(year, weekNumber);

            var holidayDays = await GetHolidayDaysAsync(year, weekNumber, cancellationToken);
            var absenceDays = await GetAbsenceDaysAsync(employeeId, year, weekNumber, cancellationToken);
            var nonEditable = BuildNonEditableDays(holidayDays, absenceDays);


            var projectNames = await GetProjectNamesAsync(employeeId, cancellationToken);

            var existing = await _timeSheetRepository.GetByEmployeeAndWeekAsync(
                new EmployeeId(employeeId), year, weekNumber, cancellationToken);

            // Si elle existe en DB → on la retourne telle quelle
            if (existing is not null)
                return TimeSheetMapper.ToDto(existing, projectNames, holidayDays, absenceDays, nonEditable);

            // Sinon → DTO vide, RIEN n'est créé en DB
            var employee = await _employeeRepository.GetByIdAsync(
                new EmployeeId(employeeId), cancellationToken)
                ?? throw new Exception("Collaborateur introuvable.");

            return TimeSheetMapper.ToEmptyDto(
                employeeId, year, month, weekNumber,
                weekStart, weekEnd,
                employee.Projects, holidayDays, absenceDays, nonEditable);
        }

        // ─── SAVE ───────────────────────────────────────────────
        public async Task<TimeSheetDto> SaveAsync(
            Guid employeeId,
            SaveTimeSheetDto dto,
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Clôture mensuelle : à partir du 28, la saisie du collaborateur est gelée.
            ValidateNotClosed(today);

            // Restriction : uniquement l'année et le mois courants
            ValidateYearAndWeek(dto.Year, dto.WeekNumber, today);

            var (weekStart, weekEnd, month) = GetWeekBounds(dto.Year, dto.WeekNumber);

            var holidayDays = await GetHolidayDaysAsync(dto.Year, dto.WeekNumber, cancellationToken);
            var absenceDays = await GetAbsenceDaysAsync(employeeId, dto.Year, dto.WeekNumber, cancellationToken);
            var nonEditable = BuildNonEditableDays(holidayDays, absenceDays);

            var projectNames = await GetProjectNamesAsync(employeeId, cancellationToken);

            var employeeIdVo = new EmployeeId(employeeId);

            var existing = await _timeSheetRepository.GetByEmployeeAndWeekAsync(
                employeeIdVo, dto.Year, dto.WeekNumber, cancellationToken);

            // Mois réellement saisi (semaine à cheval). null = aucun jour saisi.
            var savedMonth = DetermineMonthFromEntries(dto.Entries);

            // Rien de saisi : la semaine doit être vide → on supprime la feuille
            // existante (et la soumission devenue orpheline) et on ne crée rien.
            if (savedMonth is null)
            {
                if (existing is not null)
                {
                    if (existing.IsLocked)
                        throw new DomainException(
                            "Cette feuille est verrouillée (soumise pour validation) et ne peut pas être modifiée.");

                    await DeleteSheetAndCleanupAsync(existing, cancellationToken);
                }

                var emptyEmployee = await _employeeRepository.GetByIdAsync(
                    employeeIdVo, cancellationToken)
                    ?? throw new Exception("Collaborateur introuvable.");

                return TimeSheetMapper.ToEmptyDto(
                    employeeId, dto.Year, month, dto.WeekNumber,
                    weekStart, weekEnd,
                    emptyEmployee.Projects, holidayDays, absenceDays, nonEditable);
            }

            // Données déplacées vers un autre mois (semaine à cheval ré-affectée) :
            // on supprime l'ancienne feuille pour la recréer sous le bon mois.
            if (existing is not null && existing.Month != savedMonth.Value)
            {
                if (existing.IsLocked)
                    throw new DomainException(
                        "Cette feuille est verrouillée (soumise pour validation) et ne peut pas être modifiée.");

                await DeleteSheetAndCleanupAsync(existing, cancellationToken);
                existing = null;
            }

            month = savedMonth.Value;

            TimeSheet timeSheet;

            if (existing is null)
            {
                // ── POST : première sauvegarde ────────────────────────────────
                var employee = await _employeeRepository.GetByIdAsync(
                    employeeIdVo, cancellationToken)
                    ?? throw new Exception("Collaborateur introuvable.");

                // Récupère (ou crée) la soumission mensuelle en Draft.
                // Toutes les feuilles d'un même mois partagent la même Submission.
                var submission = await _submissionRepository.GetByEmployeeAndMonthAsync(
                    employeeIdVo, dto.Year, month, cancellationToken);

                if (submission is null)
                {
                    submission = TimeSheetSubmission.CreateDraft(
                        employeeIdVo, dto.Year, month, createdBy: employeeId.ToString());
                    await _submissionRepository.CreateAsync(submission, cancellationToken);
                }

                timeSheet = TimeSheet.Create(
                    employeeIdVo,
                    dto.Year, month, dto.WeekNumber,
                    weekStart, weekEnd,
                    submission.Id,
                    createdBy: employeeId.ToString());

                foreach (var project in employee.Projects)
                    timeSheet.AddEntry(TimeSheetMapper.ToEntryEntity(
                        timeSheet.TimeSheetId, project.ProjectId));

                ApplyEntries(timeSheet, dto.Entries, holidayDays, absenceDays, nonEditable);
                timeSheet.Save(updatedBy: employeeId.ToString());

                await _timeSheetRepository.CreateTimeSheetAsync(timeSheet, cancellationToken);
            }
            else
            {
                // ── PUT : mise à jour des valeurs uniquement ──────────────────
                timeSheet = existing;

                ApplyEntries(timeSheet, dto.Entries, holidayDays, absenceDays, nonEditable);
                timeSheet.Save(updatedBy: employeeId.ToString());

                await _timeSheetRepository.UpdateTimeSheetAsync(timeSheet, cancellationToken);
            }

            return TimeSheetMapper.ToDto(timeSheet, projectNames, holidayDays, absenceDays, nonEditable);
        }

        // ─── SAVE (manager) ─────────────────────────────────────────────────
        // Le manager édite la feuille d'un de ses collaborateurs.
        // Différences avec SaveAsync : pas de restriction de mois et contourne le
        // verrou. Le statut de la soumission n'est PAS modifié (pas de validation
        // automatique) et les feuilles restent déverrouillées après l'édition.
        public async Task<TimeSheetDto> SaveByManagerAsync(
            Guid managerId,
            Guid employeeId,
            SaveTimeSheetDto dto,
            CancellationToken cancellationToken = default)
        {
            var employeeIdVo = new EmployeeId(employeeId);

            // Autorisation : le collaborateur doit être rattaché à ce manager.
            var employee = await _employeeRepository.GetByIdAsync(employeeIdVo, cancellationToken)
                ?? throw new DomainException("Collaborateur introuvable.");

            if (employee.ManagerId != new EmployeeId(managerId))
                throw new DomainException("Vous ne pouvez modifier que les feuilles de vos collaborateurs.");

            // NB : aucune restriction de mois — le manager peut corriger un mois passé.

            var (weekStart, weekEnd, isoMonth) = GetWeekBounds(dto.Year, dto.WeekNumber);

            var holidayDays = await GetHolidayDaysAsync(dto.Year, dto.WeekNumber, cancellationToken);
            var absenceDays = await GetAbsenceDaysAsync(employeeId, dto.Year, dto.WeekNumber, cancellationToken);
            var nonEditable = BuildNonEditableDays(holidayDays, absenceDays);

            var projectNames = await GetProjectNamesAsync(employeeId, cancellationToken);

            var existing = await _timeSheetRepository.GetByEmployeeAndWeekAsync(
                employeeIdVo, dto.Year, dto.WeekNumber, cancellationToken);

            // Mois réellement saisi (semaine à cheval). null = aucun jour saisi.
            var savedMonth = DetermineMonthFromEntries(dto.Entries);

            // Rien de saisi : la semaine doit être vide → on supprime la feuille
            // existante (et la soumission orpheline) et on ne crée/valide rien.
            if (savedMonth is null)
            {
                if (existing is not null)
                    await DeleteSheetAndCleanupAsync(existing, cancellationToken);

                return TimeSheetMapper.ToEmptyDto(
                    employeeId, dto.Year, isoMonth, dto.WeekNumber,
                    weekStart, weekEnd,
                    employee.Projects, holidayDays, absenceDays, nonEditable);
            }

            // Données déplacées vers un autre mois : on supprime l'ancienne feuille
            // (et sa soumission orpheline) pour la recréer sous le bon mois.
            if (existing is not null && existing.Month != savedMonth.Value)
            {
                await DeleteSheetAndCleanupAsync(existing, cancellationToken);
                existing = null;
            }

            int month = savedMonth.Value;

            // Récupère (ou crée) la soumission mensuelle.
            var submission = await _submissionRepository.GetByEmployeeAndMonthAsync(
                employeeIdVo, dto.Year, month, cancellationToken);

            if (submission is null)
            {
                submission = TimeSheetSubmission.CreateDraft(
                    employeeIdVo, dto.Year, month, createdBy: managerId.ToString());
                await _submissionRepository.CreateAsync(submission, cancellationToken);
            }

            TimeSheet timeSheet;

            if (existing is null)
            {
                timeSheet = TimeSheet.Create(
                    employeeIdVo,
                    dto.Year, month, dto.WeekNumber,
                    weekStart, weekEnd,
                    submission.Id,
                    createdBy: managerId.ToString());

                foreach (var project in employee.Projects)
                    timeSheet.AddEntry(TimeSheetMapper.ToEntryEntity(
                        timeSheet.TimeSheetId, project.ProjectId));

                ApplyEntries(timeSheet, dto.Entries, holidayDays, absenceDays, nonEditable);
                timeSheet.Save(updatedBy: managerId.ToString());

                await _timeSheetRepository.CreateTimeSheetAsync(timeSheet, cancellationToken);
            }
            else
            {
                timeSheet = existing;

                // Contourne le verrou : le manager peut éditer une feuille déjà soumise/validée.
                // La feuille reste déverrouillée après l'édition (pas de validation automatique).
                timeSheet.Unlock();
                ApplyEntries(timeSheet, dto.Entries, holidayDays, absenceDays, nonEditable);
                timeSheet.Save(updatedBy: managerId.ToString());

                await _timeSheetRepository.UpdateTimeSheetAsync(timeSheet, cancellationToken);
            }

            // Le statut de la soumission n'est pas modifié : l'édition du manager ne
            // vaut pas validation. Le manager validera explicitement le cas échéant.

            return TimeSheetMapper.ToDto(timeSheet, projectNames, holidayDays, absenceDays, nonEditable);
        }

        // ─── GET MONTH ────────────────────────────────────────────────────────
        public async Task<IReadOnlyList<TimeSheetDto>> GetMonthAsync(
            Guid employeeId,
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Restriction : uniquement l'année et le mois courants
            if (year != today.Year || month != today.Month)
                throw new DomainException(
                    "Vous ne pouvez consulter que le mois en cours.");

            if (month < 1 || month > 12)
                throw new DomainException($"Mois invalide : {month}.");

            var sheets = await _timeSheetRepository.GetByEmployeeAndMonthAsync(
                new EmployeeId(employeeId), year, month, cancellationToken);

            var projectNames = await GetProjectNamesAsync(employeeId, cancellationToken);

            var result = new List<TimeSheetDto>();

            foreach (var sheet in sheets)
            {
                var holidayDays = await GetHolidayDaysAsync(year, sheet.WeekNumber, cancellationToken);
                var absenceDays = await GetAbsenceDaysAsync(employeeId, year, sheet.WeekNumber, cancellationToken);
                var nonEditable = BuildNonEditableDays(holidayDays, absenceDays);

                result.Add(TimeSheetMapper.ToDto(sheet, projectNames, holidayDays, absenceDays, nonEditable));
            }

            return result;
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────

        // Jour du mois à partir duquel la feuille de temps est clôturée pour le collaborateur.
        private const int MonthlyClosingDay = 28;

        // Semaine à cheval sur deux mois : on rattache la feuille au mois où des
        // jours sont réellement saisis (valeur > 0), pas au mois ISO du jeudi.
        // Renvoie null si aucun jour n'est saisi (→ on ne crée rien).
        private static int? DetermineMonthFromEntries(List<SaveTimeEntryDto> entries)
        {
            var daysPerMonth = new Dictionary<int, decimal>();

            foreach (var entry in entries)
            {
                foreach (var (dateStr, value) in entry.DailyValues)
                {
                    if (value <= 0) continue;
                    var date = DateOnly.Parse(dateStr);
                    daysPerMonth[date.Month] = daysPerMonth.GetValueOrDefault(date.Month) + value;
                }
            }

            if (daysPerMonth.Count == 0)
                return null;

            // Mois totalisant le plus de jours ; égalité → mois le plus tôt.
            return daysPerMonth
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .First().Key;
        }

        // Supprime une feuille de temps, puis nettoie la soumission de son mois
        // si elle est devenue orpheline (plus aucune feuille rattachée).
        private async Task DeleteSheetAndCleanupAsync(TimeSheet sheet, CancellationToken ct)
        {
            var employeeIdVo = sheet.EmployeeId;
            var year = sheet.Year;
            var month = sheet.Month;

            await _timeSheetRepository.DeleteTimeSheetAsync(sheet, ct);
            await CleanupOrphanSubmissionAsync(employeeIdVo, year, month, ct);
        }

        // Supprime la soumission d'un mois dès qu'il n'y reste plus aucune feuille.
        // Une soumission sans feuille n'a aucun sens (quel que soit son statut),
        // et la conserver ferait apparaître une ligne fantôme à zéro dans les listes.
        private async Task CleanupOrphanSubmissionAsync(
            EmployeeId employeeIdVo, int year, int month, CancellationToken ct)
        {
            var remaining = await _timeSheetRepository.GetByEmployeeAndMonthAsync(
                employeeIdVo, year, month, ct);
            if (remaining.Count > 0)
                return;

            var submission = await _submissionRepository.GetByEmployeeAndMonthAsync(
                employeeIdVo, year, month, ct);
            if (submission is not null)
                await _submissionRepository.DeleteAsync(submission, ct);
        }

        // Clôture mensuelle : dès le 28 (inclus), le collaborateur ne peut plus
        // sauvegarder/soumettre. Le manager n'est pas concerné (il passe par ses
        // propres méthodes qui n'appellent pas ce contrôle).
        private static void ValidateNotClosed(DateOnly today)
        {
            if (today.Day >= MonthlyClosingDay)
                throw new DomainException(
                    $"La saisie des feuilles de temps est clôturée à partir du {MonthlyClosingDay} du mois.");
        }

        private static void ValidateYearAndWeek(int year, int weekNumber, DateOnly today)
        {
            var (weekStart, weekEnd, _) = GetWeekBounds(year, weekNumber);

            var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
            var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

            // Chevauchement de l'intervalle [weekStart, weekEnd] avec le mois courant
            bool overlapsCurrentMonth = weekStart <= lastOfMonth && weekEnd >= firstOfMonth;

            if (!overlapsCurrentMonth)
                throw new DomainException("Vous ne pouvez accéder qu'aux semaines du mois en cours.");
        }

        private static void ApplyEntries(
            TimeSheet timeSheet,
            List<SaveTimeEntryDto> entries,
            List<DateOnly> holidayDays,
            Dictionary<DateOnly, decimal> absenceDays,
            List<DateOnly> nonEditableDays)
        {
            foreach (var entryDto in entries)
            {
                var entry = timeSheet.Entries
                    .FirstOrDefault(e => (Guid)e.ProjectId == entryDto.ProjectId)
                    ?? throw new Exception($"Entrée introuvable pour le projet {entryDto.ProjectId}.");

                foreach (var (dateStr, value) in entryDto.DailyValues)
                {
                    var date = DateOnly.Parse(dateStr);

                    if (nonEditableDays.Contains(date))
                    {
                        if (value != 0)
                            throw new DomainException($"Le jour {date} est non éditable (férié ou congé validé).");
                        continue;
                    }

                    timeSheet.UpdateEntry(entry.TimeEntryId, date, value, nonEditableDays);
                }
            }
        }
        private async Task<List<DateOnly>> GetHolidayDaysAsync(
            int year,
            int weekNumber,
            CancellationToken cancellationToken)
        {
            var (weekStart, weekEnd, _) = GetWeekBounds(year, weekNumber);

            var holidays = await _holidayRepository.GetByYearAsync(year, cancellationToken);

            return holidays
                .Select(h => h.Date)
                .Where(d => d >= weekStart && d <= weekEnd)
                .OrderBy(d => d)
                .ToList();
        }

        private async Task<Dictionary<DateOnly, decimal>> GetAbsenceDaysAsync(
            Guid employeeId,
            int year,
            int weekNumber,
            CancellationToken cancellationToken)
        {
            var (weekStart, weekEnd, _) = GetWeekBounds(year, weekNumber);

            // Récupère les fériés de la semaine pour les exclure
            var holidayDates = (await _holidayRepository.GetByYearAsync(year, cancellationToken))
                .Select(h => h.Date)
                .Where(d => d >= weekStart && d <= weekEnd)
                .ToHashSet();

            var leaveRequests = await _leaveRequestRepository.GetAsync(
                employeeId, LeaveRequestStatus.Approved, cancellationToken);

            var result = new Dictionary<DateOnly, decimal>();

            foreach (var lr in leaveRequests)
            {
                foreach (var d in GetDateRange(lr.StartDate, lr.EndDate))
                {
                    if (d < weekStart || d > weekEnd) continue;

                    // ← Jour férié : on ignore ce jour de congé
                    if (holidayDates.Contains(d)) continue;

                    decimal val = 1m;

                    bool halfStart = lr.StartPeriod == DayPeriod.Afternoon;
                    bool halfEnd = lr.EndPeriod == DayPeriod.Morning;

                    if (d == lr.StartDate && d == lr.EndDate)
                    {
                        if (halfStart || halfEnd) val = 0.5m;
                    }
                    else if (d == lr.StartDate && halfStart)
                    {
                        val = 0.5m;
                    }
                    else if (d == lr.EndDate && halfEnd)
                    {
                        val = 0.5m;
                    }

                    if (result.TryGetValue(d, out var existing))
                        result[d] = Math.Min(1m, existing + val);
                    else
                        result[d] = val;
                }
            }

            return result;
        }
        // NonEditableDays = union fériés + jours de congé 
        private static List<DateOnly> BuildNonEditableDays(
            List<DateOnly> holidayDays,
            Dictionary<DateOnly, decimal> absenceDays)
        {
            // Congés plein jour bloquent la saisie 
            var fullAbsenceDays = absenceDays
                .Where(kvp => kvp.Value == 1m)
                .Select(kvp => kvp.Key);

            return holidayDays
                .Union(fullAbsenceDays)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }
        
        private async Task<Dictionary<Guid, string>> GetProjectNamesAsync(
            Guid employeeId,
            CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.GetByIdAsync(
                new EmployeeId(employeeId), cancellationToken);

            return employee?.Projects
                .ToDictionary(p => (Guid)p.ProjectId, p => p.Name)
                ?? new Dictionary<Guid, string>();
        }

        /// <summary>
        /// Calcule le lundi et le vendredi d'une semaine ISO, et détermine
        /// le mois via le JEUDI (règle ISO 8601).
        /// Le mois sera toujours entre 1 et 12.
        /// </summary>
        private static (DateOnly start, DateOnly end, int month) GetWeekBounds(
            int year, int weekNumber)
        {
            // Semaine 1 ISO = semaine contenant le 4 janvier
            var jan4 = new DateTime(year, 1, 4);
            int daysToMonday = ((int)jan4.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var week1Monday = jan4.AddDays(-daysToMonday);

            var weekStart = DateOnly.FromDateTime(week1Monday.AddDays((weekNumber - 1) * 7));
            var weekEnd = weekStart.AddDays(4); // vendredi

            // Le JEUDI de la semaine détermine le mois (règle ISO)
            // weekStart = lundi → +3 jours = jeudi
            var thursday = weekStart.AddDays(3);
            int month = thursday.Month;

            // Garantie : mois toujours valide (1-12) — en théorie toujours vrai
            // mais on lève une exception claire si ce n'est pas le cas
            if (month < 1 || month > 12)
                throw new DomainException($"Impossible de déterminer un mois valide pour la semaine {weekNumber}/{year}.");

            return (weekStart, weekEnd, month);
        }

        private static IEnumerable<DateOnly> GetDateRange(DateOnly start, DateOnly end)
        {
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                if (d.DayOfWeek != DayOfWeek.Saturday &&
                    d.DayOfWeek != DayOfWeek.Sunday)
                    yield return d;
            }
        }
    }
}