using in_ctech_management_backend.Application.Job.DTOs;
using in_ctech_management_backend.Domain.Jobs;
using in_ctech_management_backend.Domain.Jobs.Repositories;


namespace in_ctech_management_backend.Application.Job
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _repository;

        public JobService(IJobRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> CreateAsync(CreateJobRequest dto, CancellationToken cancellationToken = default)
        {
            var existing = await _repository.GetByTitle(dto.Title, cancellationToken);
            if (existing is not null)
                throw new ApplicationException("job avec ce titre est déjà existant");
            var job = Domain.Jobs.Job.Create(dto.Title, dto.Description, dto.CreatedBy);
            await _repository.AddAsync(job);
            await _repository.SaveChangesAsync(cancellationToken);
            return job.JobId.Value;
        }

        public async Task<JobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var job = await _repository.GetByIdAsync(new JobId(id), cancellationToken);
            return job is not null ? MapToDto(job) : null;
        }

        public async Task<List<JobDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var jobs = await _repository.GetAllAsync(cancellationToken);
            return jobs.Select(MapToDto).ToList();
        }

        public async Task UpdateAsync(Guid id, UpdateJobRequest dto, CancellationToken cancellationToken = default)
        {
            var existing = await _repository.GetByIdAsync(new JobId(id), cancellationToken);
            if (existing is null)
                throw new ApplicationException($"job with id : {id} is not found");
            existing.Update(
                dto.Title ?? existing.Title,
                dto.Description ?? existing.Description,
                dto.UpdatedBy
            );
            await _repository.UpdateAsync(existing, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _repository.GetByIdAsync(new JobId(id), cancellationToken);
            if (existing is null)
                throw new ApplicationException($"job with id : {id} is not foud");
            await _repository.DeleteAsync(new JobId(id), cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }


        private static JobDto MapToDto(Domain.Jobs.Job j)
        {
            return new JobDto(
                j.JobId.Value,
                j.Title,
                j.Description,
                j.CreatedAt,
                j.UpdatedAt,
                j.CreatedBy,
                j.UpdatedBy
            );
        }
    }
}
