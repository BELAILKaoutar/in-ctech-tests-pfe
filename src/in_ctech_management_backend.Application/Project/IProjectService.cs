using in_ctech_management_backend.Application.Projects.DTOs;

namespace in_ctech_management_backend.Application.Projects
{
    public interface IProjectService
    {
        Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default);
        Task<ProjectDto?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProjectDto>> GetAllAsync(string? name = null, CancellationToken cancellationToken = default);
        Task<ProjectDto> UpdateAsync(Guid projectId, UpdateProjectDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default);
    }
}