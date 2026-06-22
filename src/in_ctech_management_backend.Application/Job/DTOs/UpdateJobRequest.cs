namespace in_ctech_management_backend.Application.Job.DTOs
{
    public record UpdateJobRequest(
        string? Title,
        string? Description,
        string? UpdatedBy = null
    );
}
