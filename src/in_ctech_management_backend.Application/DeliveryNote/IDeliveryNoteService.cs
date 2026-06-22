using in_ctech_management_backend.Application.DeliveryNote.DTOs;

namespace in_ctech_management_backend.Application.DeliveryNote
{
    public interface IDeliveryNoteService
    {
        Task<List<Guid>> CreateAsync(CreateDeliveryNoteRequest dto, CancellationToken cancellationToken = default);
        Task<DeliveryNoteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<DeliveryNoteDocumentDataDto?> GetDocumentDataAsync(Guid id, string? template = null, bool signed = false, CancellationToken cancellationToken = default);
        Task<List<DeliveryNoteDto>> GetAllAsync(
            string? reference = null,
            Guid? companyId = null,
            Guid? employeeId = null,
            Guid? projectId = null,
            string? type = null,
            CancellationToken cancellationToken = default);
        Task UpdateAsync(Guid id, UpdateDeliveryNoteRequest dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
