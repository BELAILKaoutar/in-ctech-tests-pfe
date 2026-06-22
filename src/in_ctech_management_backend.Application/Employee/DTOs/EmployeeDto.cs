using in_ctech_management_backend.Domain.Employees.Enums;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;

namespace in_ctech_management_backend.Application.Employee.DTOs
{
    public record EmployeeDto(
        Guid Id,
        string FirstName,
        string LastName,
        string FullName,
        string Trigram,
        string Cnss,
        string NationalId,
        double? DailyRate,
        string PhoneNumber,
        string? Email,
        string? BankAccountNumber,
        string RegistrationNumber,
        PaymentMethod PaymentMethod,
        ContractType ContractType,
        FreelancerType? FreelancerType,
        DateTime? StartDate,
        DateTime? ContractEndDate,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? CreatedBy,
        string? UpdatedBy,
        Guid JobId,
        List<ProjectSummaryDto>? Projects,
        ManagerSummaryDto? Manager,
        // Leave tracking
        decimal? MonthlyLeaveAllowance,
        decimal? LeaveBalance,
        decimal AnnualConsumedLeaves,
        DateTime? LastBalanceUpdateAt,
        // Purchase price
        decimal? PurchasePrice,
        Currency? PurchasePriceCurrency,
        bool IsActive
    );
}