using System.Text.Json.Serialization;

namespace in_ctech_management_backend.Application.TimeSheets.DTOs
{
    public record TimeSheetSubmissionDto(
        [property: JsonPropertyOrder(1)] Guid Id,
        [property: JsonPropertyOrder(2)] Guid EmployeeId,
        [property: JsonPropertyOrder(3)] string? EmployeeName,
        [property: JsonPropertyOrder(4)] int Year,
        [property: JsonPropertyOrder(5)] int Month,
        [property: JsonPropertyOrder(6)] string Status,
        [property: JsonPropertyOrder(8)] DateTime SubmittedAt,
        [property: JsonPropertyOrder(9)] string SubmittedBy,
        [property: JsonPropertyOrder(10)] DateTime? ReviewedAt,
        [property: JsonPropertyOrder(11)] string? ReviewedBy,
        [property: JsonPropertyOrder(12)] string? RejectionReason
    );
}
