namespace in_ctech_management_backend.Domain.Holidays.Repositories
{
    public interface IHolidayRepository
    {
        Task<Holiday?> GetByIdAsync(HolidayId id, CancellationToken cancellationToken = default);
        Task<Holiday?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
        Task<List<Holiday>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<Holiday>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
        Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default);
        Task UpdateAsync(Holiday holiday, CancellationToken cancellationToken = default);
        Task DeleteAsync(HolidayId id, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
