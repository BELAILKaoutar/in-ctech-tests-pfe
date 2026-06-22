using in_ctech_management_backend.Domain.Projects;

namespace in_ctech_management_backend.Domain.Projects
{
    public interface IProjectRepository
    {
        Task<Project?> GetByIdAsync(ProjectId projectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Project>> GetAllAsync(string? name = null, CancellationToken cancellationToken = default);
        Task AddAsync(Project project, CancellationToken cancellationToken = default);
        Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
        Task DeleteAsync(Project project, CancellationToken cancellationToken = default);
        Task<bool> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}