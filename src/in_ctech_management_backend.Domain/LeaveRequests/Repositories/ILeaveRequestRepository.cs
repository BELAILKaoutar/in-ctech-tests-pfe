using in_ctech_management_backend.Domain.LeaveRequests.Enums;

namespace in_ctech_management_backend.Domain.LeaveRequests
{
    public interface ILeaveRequestRepository
    {
        Task<IReadOnlyList<LeaveRequest>> GetAsync(Guid? employeeId,LeaveRequestStatus? status,CancellationToken cancellationToken = default);
        Task<LeaveRequest?> GetByIdAsync(LeaveRequestId id,CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LeaveRequest>> GetByManagerAsync(Guid managerId, Guid? employeeId, LeaveRequestStatus? status, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LeaveRequest>> GetActiveOverlappingDateAsync(DateOnly date, CancellationToken cancellationToken = default);
        Task CreateLeaveRequestAsync(LeaveRequest leaveRequest,CancellationToken cancellationToken = default);
        Task UpdateAsync(LeaveRequest leaveRequest,CancellationToken cancellationToken = default);
        Task UpdateRangeAsync(IEnumerable<LeaveRequest> leaveRequests, CancellationToken cancellationToken = default);
    }
}