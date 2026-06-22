namespace in_ctech_management_backend.Application.DeliveryNote.DTOs
{
    public record CreateDeliveryNoteRequest(
        string Type,
        Guid CompanyId,
        DateTime InvoiceDate,
        string CreatedBy,
        // AT
        int? Month,
        int? Year,
        List<CreateDeliveryNoteATItem>? AtItems,
        // WP
        List<CreateDeliveryNoteWPItem>? WpItems
    );
}
