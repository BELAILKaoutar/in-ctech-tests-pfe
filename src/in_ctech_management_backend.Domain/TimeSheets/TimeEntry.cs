using in_ctech_management_backend.Domain.Projects;

namespace in_ctech_management_backend.Domain.TimeSheets
{
    public class TimeEntry
    {
        public TimeEntryId TimeEntryId { get; private set; }
        public TimeSheetId TimeSheetId { get; private set; }
        public ProjectId ProjectId { get; private set; }
        public IReadOnlyDictionary<DateOnly, decimal> DailyValues => _dailyValues; // lun→ven : { DateOnly → 0 / 0.5 / 1 }
        public readonly Dictionary<DateOnly, decimal> _dailyValues = new();
        public decimal TotalDays => _dailyValues.Values.Sum(); //total de la ligne (colonne "Total")

        private TimeEntry() { }
        public static TimeEntry Create(
            TimeSheetId timeSheetId,
            ProjectId projectId,
            Dictionary<DateOnly, decimal> dailyValues)
        {
            var entry = new TimeEntry
            {
                TimeEntryId = new TimeEntryId(Guid.NewGuid()),
                TimeSheetId = timeSheetId,
                ProjectId = projectId
            };

            foreach (var (date, value) in dailyValues)
            {
                ValidateValue(value, date);
                entry._dailyValues[date] = value;
            }

            return entry;
        }

        public void UpdateDay(DateOnly date, decimal value)
        {
            ValidateValue(value, date);
            _dailyValues[date] = value;
        }

        private static void ValidateValue(decimal value, DateOnly date)
        {
            if (value != 0m && value != 0.5m && value != 1m)
                throw new DomainException(
                    $"Valeur invalide ({value}) pour le {date}. Valeurs autorisées : 0, 0.5, 1.");
        }
    }
}