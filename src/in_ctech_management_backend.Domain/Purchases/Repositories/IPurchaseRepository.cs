namespace in_ctech_management_backend.Domain.Purchases.Repositories
{
    public interface IPurchaseRepository
    {
        Task<List<Purchase>> GetAllAsync(string? reference, Guid? resourceId, Guid? clientId, string? type, CancellationToken ct = default);
        Task<Purchase?> GetByIdWithIncludesAsync(PurchaseId id, CancellationToken ct = default);
        Task<Purchase?> GetByIdAsync(PurchaseId id, CancellationToken ct = default);
        Task AddAsync(Purchase purchase, CancellationToken ct = default);
        Task DeleteAsync(Purchase purchase, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
