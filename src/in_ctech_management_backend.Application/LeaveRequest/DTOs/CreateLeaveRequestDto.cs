using in_ctech_management_backend.Domain.LeaveRequests.Enums;
namespace in_ctech_management_backend.Application.LeaveRequest.DTOs
{
    public class CreateLeaveRequestDto
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DayPeriod StartPeriod { get; set; }
        public DayPeriod EndPeriod { get; set; }
        public LeaveType LeaveType { get; set; }
        public string Reason { get; set; } = default!;
    }
}
