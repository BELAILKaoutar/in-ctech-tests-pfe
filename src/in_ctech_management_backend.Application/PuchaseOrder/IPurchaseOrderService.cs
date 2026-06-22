using in_ctech_management_backend.Application.PuchaseOrder.DTOs;
using in_ctech_management_backend.Application.PurchaseOrder.DTOs;

namespace in_ctech_management_backend.Application.PurchaseOrder
{
    public interface IPurchaseOrderService
    {
        Task<Guid> CreateAsync(CreatePurchaseOrderRequest dto, CancellationToken cancellationToken = default);
        Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PurchaseOrderDocumentDataDto?> GetDocumentDataAsync(Guid id, string? template = null, CancellationToken cancellationToken = default);
        Task<List<PurchaseOrderDto>> GetAllAsync(
            string? status = null,
            Guid? societeId = null,
            Guid? employeeId = null,
            string? reference = null,
            CancellationToken cancellationToken = default);
        Task UpdateAsync(Guid id, UpdatePurchaseOrderRequest dto, CancellationToken cancellationToken = default);
        Task ChangeStatusAsync(Guid id, string newStatus, string updatedBy, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}