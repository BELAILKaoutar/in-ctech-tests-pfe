namespace in_ctech_management_backend.Domain.Equipment.Repositories
{
    public interface IEquipmentRepository
    {
        Task<Equipment?> GetByIdAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);
        Task<Equipment?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default);
        Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default);
        Task DeleteAsync(Equipment equipment, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}