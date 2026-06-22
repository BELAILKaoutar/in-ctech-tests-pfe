using in_ctech_management_backend.Application.Projects.DTOs;
using in_ctech_management_backend.Domain;
using in_ctech_management_backend.Domain.Projects;
using in_ctech_management_backend.Domain.PurchaseOrders;

namespace in_ctech_management_backend.Application.Projects
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;

        public ProjectService(IProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
        {
            var project = Project.Create(dto.Name.Trim(), dto.Description, dto.CreatedBy);
            await _projectRepository.AddAsync(project, cancellationToken);

            return MapToDto(project);
        }

        public async Task<ProjectDto?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var project = await _projectRepository.GetByIdAsync(new ProjectId(projectId), cancellationToken);

            return project is null ? null : MapToDto(project);
        }

        public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(string? name = null, CancellationToken cancellationToken = default)
        {
            var projects = await _projectRepository.GetAllAsync(name, cancellationToken);

            return projects.Select(MapToDto).ToList();
        }

        public async Task<ProjectDto> UpdateAsync(Guid projectId, UpdateProjectDto dto, CancellationToken cancellationToken = default)
        {
            var project = await _projectRepository.GetByIdAsync(new ProjectId(projectId), cancellationToken);
            if (project is null) throw new DomainException("Project not found.");

            project.Update(dto.Name, dto.Description, dto.UpdatedBy);
            await _projectRepository.UpdateAsync(project, cancellationToken);

            return MapToDto(project);
        }

        public async Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var project = await _projectRepository.GetByIdAsync(new ProjectId(projectId), cancellationToken);

            if (project is null)
                throw new DomainException("Project not found.");

            await _projectRepository.DeleteAsync(project, cancellationToken);
        }

        private static ProjectDto MapToDto(Project project)
        {
            return new ProjectDto(
                project.ProjectId.Value,
                project.Name,
                project.Description,
                project.CreatedAt,
                project.UpdatedAt,
                project.CreatedBy,
                project.UpdatedBy
            );
        }
    }
}