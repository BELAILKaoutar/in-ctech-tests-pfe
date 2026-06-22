using in_ctech_management_backend.Domain.TimeSheets.Submissions.Enums;

namespace in_ctech_management_backend.Application.TimeSheets.DTOs
{
    public record UpdateSubmissionStatusDto(
        SubmissionStatus Status,
        string? Reason
    );
}
