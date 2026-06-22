
namespace in_ctech_management_backend.Domain.LeaveRequests
{
    public sealed record LeaveRequestId(Guid Value)
    {
        public static explicit operator Guid(LeaveRequestId leaveRequestId) => leaveRequestId.Value;
    }
}
