using in_ctech_management_backend.Application.Employee.DTOs;

namespace in_ctech_management_backend.Application.Employee
{
    public interface IEmployeeService
    {
        Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default);
        Task<EmployeeDto?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmployeeDto>> GetAllAsync(
            string? fullName = null,
            string? contractType = null,
            Guid? projectId = null,
            Guid? managerId = null,
            bool isActive = true,
            CancellationToken cancellationToken = default);
        Task<List<ManagerSummaryDto>> GetManagersAsync(CancellationToken cancellationToken = default);
        Task<EmployeeDto> UpdateAsync(Guid employeeId, UpdateEmployeeDto dto, CancellationToken cancellationToken = default);
        Task<EmployeeDto> UpdateDailyRateAsync(Guid employeeId, double? dailyRate, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid employeeId, CancellationToken cancellationToken = default);
        Task<FinancialIndicatorsDto?> GetFinancialIndicatorsAsync(Guid employeeId, CancellationToken cancellationToken = default);
        Task<EmployeeDto> ActivateAsync(Guid employeeId, Guid? changedByEmployeeId, CancellationToken cancellationToken = default);
        Task<EmployeeDto> DeactivateAsync(Guid employeeId, Guid? changedByEmployeeId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmployeeDto>> GetAllForFilterAsync(CancellationToken cancellationToken = default);
        Task<List<EmployeeStatusHistoryDto>> GetStatusHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);
    }
}