using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.TimeSheets.Submissions;

namespace in_ctech_management_backend.Domain.TimeSheets
{
    public class TimeSheet
    {
        public TimeSheetId TimeSheetId { get; private set; }
        public EmployeeId EmployeeId { get; private set; }
        public int Year { get; private set; }
        public int Month { get; private set; }
        public int WeekNumber { get; private set; }  // semaine dans le mois
        public DateOnly WeekStartDate { get; private set; }  // toujours un lundi
        public DateOnly WeekEndDate { get; private set; }  // toujours un vendredi
        public IReadOnlyList<TimeEntry> Entries => _entries;
        private readonly List<TimeEntry> _entries = new();
        public decimal TotalWeekDays => _entries.Sum(e => e.TotalDays); // somme verticale = première ligne du tableau

        // Lien vers la soumission mensuelle (null = jamais soumise).
        // Conservé même après Unlock() pour garder la traçabilité.
        public SubmissionId? SubmissionId { get; private set; }
        // Verrou effectif : true quand la soumission est En attente ou Validé.
        // Repassé à false quand la soumission est À corriger.
        public bool IsLocked { get; private set; }

        public string CreatedBy { get; private set; }
        public string? UpdatedBy { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private TimeSheet() { }

        public static TimeSheet Create(
            EmployeeId employeeId,
            int year,
            int month,
            int weekNumber,
            DateOnly weekStartDate,
            DateOnly weekEndDate,
            SubmissionId submissionId,
            string createdBy)
        {
            if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
                throw new DomainException("La semaine doit commencer un lundi.");

            if (weekEndDate.DayOfWeek != DayOfWeek.Friday)
                throw new DomainException("La semaine doit finir un vendredi.");

            return new TimeSheet
            {
                TimeSheetId = new TimeSheetId(Guid.NewGuid()),
                EmployeeId = employeeId,
                Year = year,
                Month = month,
                WeekNumber = weekNumber,
                WeekStartDate = weekStartDate,
                WeekEndDate = weekEndDate,
                SubmissionId = submissionId,
                IsLocked = false,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void AddEntry(TimeEntry entry)
        {
            if (_entries.Any(e => e.ProjectId == entry.ProjectId))
                throw new DomainException($"Le projet '{entry.ProjectId}' est déjà ajouté.");

            _entries.Add(entry);
        }

        // appelé quand le collab modifie une cellule
        public void UpdateEntry(
            TimeEntryId entryId,
            DateOnly date,
            decimal value,
            IReadOnlyList<DateOnly> nonEditableDays)
        {
            if (IsLocked)
                throw new DomainException(
                    "Cette feuille est verrouillée (soumise pour validation) et ne peut pas être modifiée.");

            if (nonEditableDays.Contains(date))
                throw new DomainException("Ce jour est non éditable (férié ou congé validé).");

            var entry = _entries.FirstOrDefault(e => e.TimeEntryId == entryId)
                ?? throw new DomainException("Entrée introuvable.");

            var totalForDay = _entries.Sum(e =>
            {
                if (e.TimeEntryId == entryId)
                    return value;

                return e.DailyValues.TryGetValue(date, out var existingValue)
                    ? existingValue
                    : 0m;
            });

            if (totalForDay > 1m)
                throw new DomainException(
                    $"Le total du {date} ne doit pas dépasser 1 jour.");

            entry.UpdateDay(date, value);
            UpdatedAt = DateTime.UtcNow;
        }
        public void Save(string updatedBy)
        {
            if (IsLocked)
                throw new DomainException(
                    "Cette feuille est verrouillée (soumise pour validation) et ne peut pas être modifiée.");

            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        // Verrouille la feuille (soumission ou re-soumission).
        // La SubmissionId est déjà assignée à la création de la feuille.
        public void Lock()
        {
            IsLocked = true;
            UpdatedAt = DateTime.UtcNow;
        }

        // Déverrouille la feuille (déclenché quand la soumission passe À corriger).
        // SubmissionId est conservé pour la traçabilité.
        public void Unlock()
        {
            IsLocked = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Écrase à zéro toutes les imputations sur les jours couverts par un congé validé.
        /// Opération système — contourne les gardes de saisie collaborateur.
        /// </summary>
        public void ClearDaysForLeave(IEnumerable<DateOnly> leaveDays)
        {
            foreach (var entry in _entries)
                foreach (var day in leaveDays)
                    if (entry._dailyValues.ContainsKey(day))
                        entry._dailyValues[day] = 0m;

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
