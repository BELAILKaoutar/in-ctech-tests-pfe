namespace in_ctech_management_backend.Domain.Companies.Repositories
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken = default);
        Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<List<Company>> GetAllAsync(string? nom, string? pays, CancellationToken cancellationToken = default);
        Task AddAsync(Company company, CancellationToken cancellationToken = default);
        Task UpdateAsync(Company company, CancellationToken cancellationToken = default);
        Task DeleteAsync(CompanyId id, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
