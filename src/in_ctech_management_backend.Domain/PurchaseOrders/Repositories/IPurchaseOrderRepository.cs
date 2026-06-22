using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;

namespace in_ctech_management_backend.Domain.PurchaseOrders.Repositories
{
    public interface IPurchaseOrderRepository
    {
        Task<PurchaseOrder?> GetByIdAsync(PurchaseOrderId id, CancellationToken cancellationToken = default);
        Task<PurchaseOrder?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
        Task<List<PurchaseOrder>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status, CancellationToken cancellationToken = default);
        Task<List<PurchaseOrder>> GetByEmployeeAsync(EmployeeId employeeId, CancellationToken cancellationToken = default);
        Task<List<PurchaseOrder>> GetByCompanyAsync(CompanyId societeId, CancellationToken cancellationToken = default);
        Task<List<PurchaseOrder>> SearchByReferenceAsync(string reference, CancellationToken cancellationToken = default);
        Task<string?> GetLatestPurchaseReferenceAsync(CancellationToken cancellationToken = default);
        Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);
        Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);
        Task DeleteAsync(PurchaseOrderId id, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<string?> GetMatchingSaleOrderForEmployeeAsync(CompanyId companyId, EmployeeId employeeId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken);
    }
}