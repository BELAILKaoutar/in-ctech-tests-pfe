namespace in_ctech_management_backend.Application.PuchaseOrder.DTOs
{
    public record EmployeeSummaryDto(
        Guid Id,
        string FullName,
        string Trigram,
        string? Email
    );

}