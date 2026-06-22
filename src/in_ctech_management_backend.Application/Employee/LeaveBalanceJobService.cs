using in_ctech_management_backend.Application.LeaveRequest;
using in_ctech_management_backend.Domain.Employees.Repositories;
using Microsoft.Extensions.Logging;

namespace in_ctech_management_backend.Application.Employee
{
    public class LeaveBalanceJobService : ILeaveBalanceJobService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<LeaveBalanceJobService> _logger;

        public LeaveBalanceJobService(
            IEmployeeRepository employeeRepository,
            ILogger<LeaveBalanceJobService> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        // Appelée par le cron job mensuel (1er du mois).
        public async Task<int> AccrueMonthlyLeaveForAllEmployeesAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow;
            _logger.LogInformation("Starting monthly leave accrual job for {Date:yyyy-MM-dd}", today);

            var employees = await _employeeRepository.GetLeaveEligibleEmployeesAsync(cancellationToken);

            var creditedCount = 0;
            foreach (var employee in employees)
            {
                var credited = employee.AccrueMonthlyLeave(today);
                if (credited)
                {
                    await _employeeRepository.UpdateAsync(employee, cancellationToken);
                    creditedCount++;
                }
            }

            await _employeeRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Monthly leave accrual job done : {Count} employee(s) credited", creditedCount);

            return creditedCount;
        }

        // Appelée par le cron job annuel (1er janvier).
        public async Task<int> ResetAnnualConsumedLeavesForAllEmployeesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting annual consumed leaves reset job");

            var employees = await _employeeRepository.GetAllAsync(null, null, null, isActive: true, cancellationToken);

            var resetCount = 0;
            foreach (var employee in employees)
            {
                if (employee.AnnualConsumedLeaves > 0m)
                {
                    employee.ResetAnnualConsumedLeaves();
                    await _employeeRepository.UpdateAsync(employee, cancellationToken);
                    resetCount++;
                }
            }

            await _employeeRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Annual consumed leaves reset job done : {Count} employee(s) reset", resetCount);

            return resetCount;
        }
    }
}
