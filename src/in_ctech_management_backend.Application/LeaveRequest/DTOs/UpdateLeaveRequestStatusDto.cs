using in_ctech_management_backend.Domain.LeaveRequests.Enums;

namespace in_ctech_management_backend.Application.LeaveRequest.DTOs
{
    public record UpdateLeaveRequestStatusDto
    {
        public LeaveRequestStatus Status { get; init; }
        public string? RejectionReason { get; init; }
    }
}
