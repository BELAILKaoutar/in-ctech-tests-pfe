using in_ctech_management_backend.Domain.LeaveRequests.Enums;

namespace in_ctech_management_backend.Application.LeaveRequest.DTOs
{
    public class PreviewLeaveRequestDto
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DayPeriod StartPeriod { get; set; }
        public DayPeriod EndPeriod { get; set; }
    }
}
