namespace in_ctech_management_backend.Application.DeliveryNote.DTOs
{
    public record CreateDeliveryNoteWPItem(
        string Designation,
        double Quantity,
        double UnitPrice,
        Guid? PurchaseOrderId
    );
}
