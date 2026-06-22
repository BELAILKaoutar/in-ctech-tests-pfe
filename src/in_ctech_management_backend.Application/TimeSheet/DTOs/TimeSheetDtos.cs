namespace in_ctech_management_backend.Application.TimeSheets.DTOs
{
    public class TimeSheetDto
    {
        public Guid TimeSheetId { get; set; }
        public Guid EmployeeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int WeekNumber { get; set; }
        public DateOnly WeekStartDate { get; set; }
        public DateOnly WeekEndDate { get; set; }
        public decimal TotalWeekDays { get; set; }
        public List<TimeEntryDto> Entries { get; set; } = new();
        public List<DateOnly> HolidayDays { get; set; } = new();
        // Congés approuvés = ligne "Absences" (clé = "2025-01-12", valeur = 0.5 ou 1)
        public Dictionary<string, decimal> AbsenceDays { get; set; } = new();
        public List<DateOnly> NonEditableDays { get; set; } = new();
        public string CreatedBy { get; set; } = default!;
        public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class TimeEntryDto
    {
        public Guid TimeEntryId { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = default!;
        public decimal TotalDays { get; set; }
        // clé = "2025-01-12", valeur = 0 / 0.5 / 1
        public Dictionary<string, decimal> DailyValues { get; set; } = new();
    }
    public class SaveTimeSheetDto
    {
        public int Year { get; set; }
        public int WeekNumber { get; set; }
        public List<SaveTimeEntryDto> Entries { get; set; } = new();
    }

    public class SaveTimeEntryDto
    {
        public Guid ProjectId { get; set; }
        // clé = "2025-01-12", valeur = 0 / 0.5 / 1
        public Dictionary<string, decimal> DailyValues { get; set; } = new();
    }
}