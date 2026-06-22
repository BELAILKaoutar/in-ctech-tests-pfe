using in_ctech_management_backend.Application.Job.DTOs;

namespace in_ctech_management_backend.Application.Job
{
    public interface IJobService
    {
        Task<Guid> CreateAsync(CreateJobRequest dto, CancellationToken cancellationToken = default);
        Task<JobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<JobDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync(Guid id, UpdateJobRequest dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
