using in_ctech_management_backend.Domain.Invoices;

namespace in_ctech_management_backend.Domain.Invoices.Repositories
{
    public interface IInvoiceRepository
    {
        Task<Invoice> GetByIdAsync(InvoiceId id);
        Task<List<Invoice>> GetAllAsync();
        Task AddAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task DeleteAsync(InvoiceId id);
        Task<Invoice?> GetByReferenceAsync(string reference);
        Task<Invoice?> GetByDeliveryNoteIdAsync(Guid deliveryNoteId);
        Task<List<Invoice>> GetByReferencesAsync(List<string> references);
    }
}