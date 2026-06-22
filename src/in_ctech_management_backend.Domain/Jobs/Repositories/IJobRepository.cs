namespace in_ctech_management_backend.Domain.Jobs.Repositories
{
    public interface IJobRepository
    {
        Task<Job?> GetByIdAsync(JobId id, CancellationToken cancellationToken = default);
        Task<Job?> GetByTitle(string title, CancellationToken cancellationToken = default);
        Task<List<Job>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Job job, CancellationToken cancellationToken = default);
        Task UpdateAsync(Job job, CancellationToken cancellationToken = default);
        Task DeleteAsync(JobId id, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
