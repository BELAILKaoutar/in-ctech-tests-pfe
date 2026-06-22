namespace in_ctech_management_backend.Application.Projects.DTOs
{
    public record UpdateProjectDto(
        string Name,
        string Description,
        string? UpdatedBy = null
    );
}