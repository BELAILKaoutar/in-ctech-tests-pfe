namespace in_ctech_management_backend.Application.Projects.DTOs
{
    public record ProjectDto(
        Guid ProjectId,
        string Name,
        string Description,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? CreatedBy,
        string? UpdatedBy
    );
}