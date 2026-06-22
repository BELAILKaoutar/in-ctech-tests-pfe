using in_ctech_management_backend.Domain.Common;

namespace in_ctech_management_backend.Domain.Holidays
{
    public class Holiday : AuditableEntity
    {
        public HolidayId HolidayId { get; private set; }
        public string Title { get; private set; }
        public DateOnly Date { get; private set; }

        private Holiday()
        {
            HolidayId = new HolidayId(Guid.NewGuid());
            Title = string.Empty;
        }

        private Holiday(
            string title,
            DateOnly date)
        {
            HolidayId = new HolidayId(Guid.NewGuid());
            Title = title;
            Date = date;
        }

        public static Holiday Create(
            string title,
            DateOnly date)
        {
            ValidateTitle(title);
            ValidateDate(date);
            return new Holiday(title, date);
        }

        public void Update(string newTitle, DateOnly newDate)
        {
            ValidateTitle(newTitle);
            ValidateDate(newDate);
            Title = newTitle;
            Date = newDate;
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Holiday title is required");
            if (title.Length > 100)
                throw new DomainException("Holiday title cannot exceed 100 characters");
        }

        private static void ValidateDate(DateOnly date)
        {
            if (date == default)
                throw new DomainException("Holiday date is required");
        }
    }
}
