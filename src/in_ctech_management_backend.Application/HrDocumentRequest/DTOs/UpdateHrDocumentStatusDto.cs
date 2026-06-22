
using in_ctech_management_backend.Domain.HrDocumentRequests.Enums;

namespace in_ctech_management_backend.Application.HrDocumentRequest.DTOs
{
    public class UpdateHrDocumentStatusDto
    {
        public HrDocumentRequestStatus Status { get; set; } 
        public string? RejectionReason { get; set; }
    }
}
