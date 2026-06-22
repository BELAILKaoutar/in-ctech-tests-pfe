namespace in_ctech_management_backend.Application.Employee.DTOs
{
    public record ManagerSummaryDto(
        Guid Id,
        string FullName,
        string Email
    );
}
