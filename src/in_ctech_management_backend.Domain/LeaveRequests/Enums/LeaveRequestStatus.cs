
namespace in_ctech_management_backend.Domain.LeaveRequests.Enums
{
    public enum LeaveRequestStatus
    {
        Pending, // en attejnte de validation
        Approved, // validée
        Rejected, // refusée
        Cancelled // annulée par le collaborateur
    }
}
