using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.HrDocumentRequests;
using in_ctech_management_backend.Domain.HrDocumentRequests.Enums;

namespace in_ctech_management_backend.Tests.Domain.HrDocumentRequests;

public class HrDocumentRequestTests
{
    private static readonly EmployeeId _employeeId = new EmployeeId(Guid.NewGuid());
    private const string _createdBy = "collab@winity.com";
    private const string _updatedBy = "rh@winity.com";

    // ═══════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════

    private static HrDocumentRequest CreatePending() =>
        HrDocumentRequest.Create(_employeeId, "Attestation de travail", _createdBy);

    // ═══════════════════════════════════════════════════
    // Create
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Create_ValidRequest_ShouldReturnPendingHrDocumentRequest()
    {
        var request = CreatePending();

        Assert.Equal(HrDocumentRequestStatus.Pending, request.Status);
        Assert.Equal("Attestation de travail", request.DocumentType);
        Assert.Equal(_createdBy, request.CreatedBy);
        Assert.Equal(_employeeId.Value, request.EmployeeId.Value);
        Assert.Equal(string.Empty, request.RejectionReason);
        Assert.Null(request.UpdatedBy);
        Assert.Null(request.UpdatedAt);
    }

    // ═══════════════════════════════════════════════════
    // Approve
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Approve_PendingRequest_ShouldSetStatusApproved()
    {
        var request = CreatePending();
        request.Approve(_updatedBy);

        Assert.Equal(HrDocumentRequestStatus.Approved, request.Status);
        Assert.Equal(_updatedBy, request.UpdatedBy);
        Assert.NotNull(request.UpdatedAt);
    }

    [Fact]
    public void Approve_AlreadyApproved_ShouldThrowInvalidOperationException()
    {
        var request = CreatePending();
        request.Approve(_updatedBy);

        Assert.Throws<InvalidOperationException>(() => request.Approve(_updatedBy));
    }

    [Fact]
    public void Approve_RejectedRequest_ShouldThrowInvalidOperationException()
    {
        var request = CreatePending();
        request.Reject(_updatedBy, "Document non disponible");

        Assert.Throws<InvalidOperationException>(() => request.Approve(_updatedBy));
    }

    // ═══════════════════════════════════════════════════
    // Reject
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Reject_PendingRequest_ShouldSetStatusRejected()
    {
        var request = CreatePending();
        request.Reject(_updatedBy, "Document non disponible");

        Assert.Equal(HrDocumentRequestStatus.Rejected, request.Status);
        Assert.Equal("Document non disponible", request.RejectionReason);
        Assert.Equal(_updatedBy, request.UpdatedBy);
    }

    [Fact]
    public void Reject_EmptyReason_ShouldThrowArgumentException()
    {
        var request = CreatePending();

        Assert.Throws<ArgumentException>(() => request.Reject(_updatedBy, "   "));
    }

    [Fact]
    public void Reject_AlreadyRejected_ShouldThrowInvalidOperationException()
    {
        var request = CreatePending();
        request.Reject(_updatedBy, "Motif");

        Assert.Throws<InvalidOperationException>(() =>
            request.Reject(_updatedBy, "Autre motif"));
    }

    // ═══════════════════════════════════════════════════
    // Cancel
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Cancel_PendingRequest_ShouldSetStatusCancelled()
    {
        var request = CreatePending();
        request.Cancel(_createdBy);

        Assert.Equal(HrDocumentRequestStatus.Cancelled, request.Status);
        Assert.Equal(_createdBy, request.UpdatedBy);
        Assert.NotNull(request.UpdatedAt);
    }

    [Fact]
    public void Cancel_ApprovedRequest_ShouldThrowInvalidOperationException()
    {
        var request = CreatePending();
        request.Approve(_updatedBy);

        Assert.Throws<InvalidOperationException>(() => request.Cancel(_createdBy));
    }

    [Fact]
    public void Cancel_CancelledRequest_ShouldThrowInvalidOperationException()
    {
        var request = CreatePending();
        request.Cancel(_createdBy);

        Assert.Throws<InvalidOperationException>(() => request.Cancel(_createdBy));
    }

    // ═══════════════════════════════════════════════════
    // Update
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Update_PendingRequest_ShouldChangeDocumentType()
    {
        var request = CreatePending();
        request.Update("Bulletin de paie", _createdBy);

        Assert.Equal("Bulletin de paie", request.DocumentType);
        Assert.Equal(_createdBy, request.UpdatedBy);
        Assert.NotNull(request.UpdatedAt);
    }

    [Fact]
    public void Update_EmptyDocumentType_ShouldThrowArgumentException()
    {
        var request = CreatePending();

        Assert.Throws<ArgumentException>(() => request.Update("   ", _createdBy));
    }

    [Fact]
    public void Update_ApprovedRequest_ShouldThrowInvalidOperationException()
    {
        var request = CreatePending();
        request.Approve(_updatedBy);

        Assert.Throws<InvalidOperationException>(() =>
            request.Update("Bulletin de paie", _createdBy));
    }
}
