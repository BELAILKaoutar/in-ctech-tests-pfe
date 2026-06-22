using in_ctech_management_backend.Application.Purchase.DTOs;

namespace in_ctech_management_backend.Application.Purchase
{
    public interface IPurchaseService
    {
        Task<List<PurchaseResponseDto>> GetAllAsync(string? reference, Guid? resource, Guid? client, string? type, CancellationToken ct = default);
        Task<PurchaseResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<PurchaseResponseDto> CreateAsync(PurchaseRequestDto dto, CancellationToken ct = default);
        Task<PurchaseResponseDto> UpdateAsync(Guid id, PurchaseRequestDto dto, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task<PurchaseResponseDto> ConfirmPaymentAsync(Guid id, PurchaseConfirmPaymentDto dto, CancellationToken ct = default);
    }
}
