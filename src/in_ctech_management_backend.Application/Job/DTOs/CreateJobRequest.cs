namespace in_ctech_management_backend.Application.Job.DTOs
{
    public record CreateJobRequest(
        string Title,
        string Description,
        string? CreatedBy = null
    );
}
