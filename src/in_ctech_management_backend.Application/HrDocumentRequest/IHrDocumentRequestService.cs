using in_ctech_management_backend.Application.HrDocumentRequest.DTOs;
using in_ctech_management_backend.Domain.HrDocumentRequests.Enums;

namespace in_ctech_management_backend.Application.HrDocumentRequest;

public interface IHrDocumentRequestService
{
    Task<IReadOnlyList<HrDocumentRequestDto>> GetMyRequestsAsync(
        Guid employeeId,
        int? year,
        HrDocumentRequestStatus? status,
        CancellationToken ct = default);

    Task<IReadOnlyList<HrDocumentRequestDto>> GetAllRequestsAsync(
        int? year,
        HrDocumentRequestStatus? status,
        CancellationToken ct = default);

    Task<string[]> GetDocumentTypesAsync(
        Guid employeeId,
        CancellationToken ct = default);

    Task<HrDocumentRequestDto> CreateAsync(
        Guid employeeId,
        CreateHrDocumentRequestDto dto,
        CancellationToken ct = default);

    Task<HrDocumentRequestDto> UpdateStatusAsync(
        Guid id,
        string updatedBy,
        UpdateHrDocumentStatusDto dto,
        CancellationToken ct = default);
    Task<HrDocumentRequestDto> UpdateAsync(
        Guid id,
        Guid employeeId,
        UpdateHrDocumentRequestDto dto,
        CancellationToken ct = default);

    Task<HrDocumentRequestDto> CancelAsync(
        Guid id,
        Guid employeeId,
        CancellationToken ct = default);
}