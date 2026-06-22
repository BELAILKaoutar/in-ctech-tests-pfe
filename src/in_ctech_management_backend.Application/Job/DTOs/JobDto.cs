namespace in_ctech_management_backend.Application.Job.DTOs
{
    public record JobDto(
        Guid Id,
        string Title,
        string Description,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? CreatedBy,
        string? UpdatedBy
    );
}
