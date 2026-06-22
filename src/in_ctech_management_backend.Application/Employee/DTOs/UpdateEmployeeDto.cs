using in_ctech_management_backend.Domain.Employees.Enums;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;

namespace in_ctech_management_backend.Application.Employee.DTOs
{
    public record UpdateEmployeeDto(
        string FirstName,
        string LastName,
        string Trigram,
        string Cnss,
        string NationalId,
        string PhoneNumber,
        string? Email,
        string? BankAccountNumber,
        PaymentMethod PaymentMethod,
        ContractType ContractType,
        FreelancerType? FreelancerType,
        DateTime? StartDate,
        DateTime? ContractEndDate,
        Guid JobId,
        List<Guid>? ProjectIds,
        Guid? ManagerId,
        decimal? MonthlyLeaveAllowance,
        decimal? LeaveBalance,
        decimal? AnnualConsumedLeaves,
        decimal? PurchasePrice,
        Currency? PurchasePriceCurrency
    );
}