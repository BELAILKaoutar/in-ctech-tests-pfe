namespace in_ctech_management_backend.Application.TimeSheets.DTOs
{
    public record SubmissionProjectTotalDto(
        Guid ProjectId,
        string ProjectName,
        decimal TotalDays
    );
}
