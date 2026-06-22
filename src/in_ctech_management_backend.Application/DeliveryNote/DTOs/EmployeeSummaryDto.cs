namespace in_ctech_management_backend.Application.DeliveryNote.DTOs
{
    public record EmployeeSummaryDto(
        Guid Id,
        string FullName,
        string Trigram,
        string? Email,
        double? DailyRate,
        List<string> ProjectNames
    );
}
