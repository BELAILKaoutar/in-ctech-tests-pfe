using in_ctech_management_backend.Application.Invoices.DTOs;

namespace in_ctech_management_backend.Application.Invoices
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, string createdBy);
        Task<CreateInvoicesFromDeliveryNotesDto> CreateFromDeliveryNotesAsync(
            CreateInvoicesFromDeliveryNotesDto dto,
            string createdBy,
            CancellationToken cancellationToken = default);
        Task<List<InvoiceDto>> GetInvoicesAsync(
            string? reference = null,
            string? ressourceId = null,
            string? societeId = null,
            string? projectId = null,
            int? moisFacturation = null,
            string? type = null,
            string? dateFacturation = null);
        Task<InvoiceDto> GetInvoiceByIdAsync(string reference);
        Task<InvoiceDocumentDataDto?> GetInvoiceDocumentDataAsync(string reference, string? template = null, bool signed = false, CancellationToken cancellationToken = default);
        Task<InvoiceDto?> UpdateInvoiceAsync(string reference, UpdateInvoiceDto dto, string updatedBy = "System");
        Task<bool> DeleteInvoiceAsync(string reference, string deletedBy = "System");
        Task<bool> UpdateStatusAsync(string reference, string status, string updatedBy, DateTime? paymentDate, string? transferReference);
        Task<bool> UpdateBulkStatusAsync(List<string> references, string status, string updatedBy, DateTime? paymentDate, string? transferReference);
    }
}