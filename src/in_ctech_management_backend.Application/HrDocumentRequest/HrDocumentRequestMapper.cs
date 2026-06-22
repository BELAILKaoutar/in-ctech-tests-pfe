using in_ctech_management_backend.Application.HrDocumentRequest.DTOs;
using in_ctech_management_backend.Domain.HrDocumentRequests;
using in_ctech_management_backend.Domain.HrDocumentRequests.Enums;

namespace in_ctech_management_backend.Application.HrDocumentRequest;

public static class HrDocumentRequestMapper
{
    public static HrDocumentRequestDto ToDto(
        Domain.HrDocumentRequests.HrDocumentRequest r,
        string? employeeName = null)
    {
        return new HrDocumentRequestDto
        {
            Id = r.Id.Value,
            EmployeeId = (Guid)r.EmployeeId,
            EmployeeName = employeeName ?? string.Empty,
            DocumentType = r.DocumentType,
            Status = GetStatusLabel(r.Status),
            RejectionReason = r.Status == HrDocumentRequestStatus.Rejected
                ? r.RejectionReason
                : null,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            CreatedBy = r.CreatedBy,   
            UpdatedBy = r.UpdatedBy,
        };
    }

    public static string GetStatusLabel(HrDocumentRequestStatus status) => status switch
    {
        HrDocumentRequestStatus.Pending => "En attente",
        HrDocumentRequestStatus.Approved => "Validée",
        HrDocumentRequestStatus.Rejected => "Refusée",
        _ => status.ToString(),
    };
}