using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.HrDocumentRequests.Enums;

namespace in_ctech_management_backend.Domain.HrDocumentRequests;

public class HrDocumentRequest
{
    public HrDocumentRequestId Id { get; private set; } = null!;
    public EmployeeId EmployeeId { get; private set; } = null!;
    public string DocumentType { get; private set; } = string.Empty;
    public HrDocumentRequestStatus Status { get; private set; }
    public string RejectionReason { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public string? UpdatedBy { get; private set; }

    private HrDocumentRequest() { }

    public static HrDocumentRequest Create(
        EmployeeId employeeId,
        string documentType,
        string createdBy)
    {
        return new HrDocumentRequest
        {
            Id = new HrDocumentRequestId(Guid.NewGuid()),
            EmployeeId = employeeId,
            DocumentType = documentType,
            Status = HrDocumentRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
    }

    public void Approve(string updatedBy)
    {
        if (Status != HrDocumentRequestStatus.Pending)
            throw new InvalidOperationException(
                "Seules les demandes en attente peuvent être validées.");

        Status = HrDocumentRequestStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Reject(string updatedBy, string rejectionReason)
    {
        if (Status != HrDocumentRequestStatus.Pending)
            throw new InvalidOperationException(
                "Seules les demandes en attente peuvent être refusées.");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Le motif de refus est obligatoire.");

        Status = HrDocumentRequestStatus.Rejected;
        RejectionReason = rejectionReason;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Cancel(string updatedBy)
    {
        if (Status != HrDocumentRequestStatus.Pending)
            throw new InvalidOperationException(
                "Seules les demandes en attente peuvent être annulées.");

        Status = HrDocumentRequestStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Update(string documentType, string updatedBy)
    {
        if (Status != HrDocumentRequestStatus.Pending)
            throw new InvalidOperationException(
                "Seules les demandes en attente peuvent être modifiées.");

        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("Le type de document est obligatoire.");

        DocumentType = documentType;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
