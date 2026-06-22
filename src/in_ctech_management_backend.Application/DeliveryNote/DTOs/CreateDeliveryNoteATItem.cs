namespace in_ctech_management_backend.Application.DeliveryNote.DTOs
{
    public record CreateDeliveryNoteATItem(
        Guid EmployeeId,
        double Quantity
    );
}
