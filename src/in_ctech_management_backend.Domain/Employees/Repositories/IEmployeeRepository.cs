using in_ctech_management_backend.Domain.Employees.Enums;
using in_ctech_management_backend.Domain.Projects;

namespace in_ctech_management_backend.Domain.Employees.Repositories
{
    public interface IEmployeeRepository
    {
        Task<bool> ExistsByFirstNameAndLastNameAsync(string firstName, string lastName, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<string?> GetLastRegistrationNumberAsync(bool isPermanent, CancellationToken cancellationToken = default);
        Task<List<Employee>> GetManagersAsync(CancellationToken cancellationToken = default);

        Task AddAsync(Employee employee, CancellationToken cancellationToken = default);

        Task<Employee?> GetByIdAsync(EmployeeId employeeId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Employee>> GetAllAsync(
            string? fullName = null,
            ContractType? contractType = null,
            ProjectId? projectId = null,
            bool isActive = true,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Employee>> GetLeaveEligibleEmployeesAsync(CancellationToken cancellationToken = default);

        Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);

        Task DeleteAsync(Employee employee, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Employee?> GetManagerByEmployeeIdAsync(EmployeeId employeeId, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, string>> GetNamesByIdsAsync(List<Guid> employeeIds, CancellationToken cancellationToken = default);

        Task<Dictionary<Guid, EmployeeSummaryProjection>> GetSummariesByIdsAsync(List<Guid> employeeIds, CancellationToken cancellationToken = default);

        // batch : projets affectés à un ensemble d'employés (employeeId -> { projectId -> name })
        Task<Dictionary<Guid, Dictionary<Guid, string>>> GetProjectsByEmployeeIdsAsync(List<Guid> employeeIds, CancellationToken cancellationToken = default);

        Task SetActiveStatusAsync(EmployeeId employeeId, bool newStatus, EmployeeId? changedByEmployeeId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Employee>> GetAllWithoutStatusFilterAsync(CancellationToken cancellationToken = default);
        Task<List<EmployeeStatusHistory>> GetStatusHistoryByEmployeeIdAsync(EmployeeId employeeId, CancellationToken cancellationToken = default);
    }

    public record EmployeeSummaryProjection(string FullName, decimal AnnualConsumedLeaves);
}