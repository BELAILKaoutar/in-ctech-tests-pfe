namespace in_ctech_management_backend.Application.LeaveRequest
{
    public interface ILeaveBalanceJobService
    {
        Task<int> AccrueMonthlyLeaveForAllEmployeesAsync(CancellationToken cancellationToken = default);
        Task<int> ResetAnnualConsumedLeavesForAllEmployeesAsync(CancellationToken cancellationToken = default);
    }
}