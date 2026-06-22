namespace in_ctech_management_backend.Application.DeliveryNote.DTOs
{
    public record UpdateDeliveryNoteRequest(
        Guid CompanyId,
        double Quantity,
        DateTime InvoiceDate,
        string UpdatedBy,
        // AT
        Guid? EmployeeId,
        int? Month,
        int? Year,
        // WP
        string? Designation,
        double? UnitPrice
    );
}
