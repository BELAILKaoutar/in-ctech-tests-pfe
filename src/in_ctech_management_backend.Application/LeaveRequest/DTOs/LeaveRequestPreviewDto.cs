namespace in_ctech_management_backend.Application.LeaveRequest.DTOs
{
    public class LeaveRequestPreviewDto
    {
        public decimal NumberOfDays { get; set; }
        public List<DateOnly> HolidayDates { get; set; } = new();
        public List<DateOnly> OverlappingApprovedDates { get; set; } = new();
    }
}
