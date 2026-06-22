

namespace in_ctech_management_backend.Domain.HrDocumentRequests
{
    public sealed record HrDocumentRequestId(Guid Value)
    {
        public static explicit operator Guid(HrDocumentRequestId hrDocumentRequestId) => hrDocumentRequestId.Value;
    }
}


