namespace in_ctech_management_backend.Domain.DeliveryNotes
{
    public sealed record DeliveryNoteId(Guid Value)
    {
        public static explicit operator Guid(DeliveryNoteId deliveryNoteId) => deliveryNoteId.Value;
    }
}