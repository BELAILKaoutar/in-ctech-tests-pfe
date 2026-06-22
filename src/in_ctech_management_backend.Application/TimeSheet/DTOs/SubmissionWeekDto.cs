namespace in_ctech_management_backend.Application.TimeSheets.DTOs
{
    public record SubmissionWeekDto(
        Guid TimeSheetId,
        int WeekNumber,
        DateOnly WeekStartDate,
        DateOnly WeekEndDate,
        List<SubmissionProjectTotalDto> ProjectTotals
    );
}
