namespace in_ctech_management_backend.Application.Projects.DTOs
{
    public record CreateProjectDto(
        string Name,
        string Description,
        string? CreatedBy = null
    );
}