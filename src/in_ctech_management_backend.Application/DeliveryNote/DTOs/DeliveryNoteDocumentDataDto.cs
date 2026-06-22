namespace in_ctech_management_backend.Application.DeliveryNote.DTOs
{
    public record DeliveryNoteDocumentClientDto(
        string Id,
        string Nom,
        string? Adresse,
        string Code,
        string Pays
    );

    public record DeliveryNoteDocumentItemDto(
        string Description,
        double Quantity,
        double UnitPrice,
        double Total
    );

    public record DeliveryNoteDocumentDataDto(
        Guid Id,
        string Reference,
        string FileName,
        string Template,
        string Type,
        DateTime IssueDate,
        string? PurchaseOrderReference,
        DeliveryNoteDocumentClientDto? Client,
        string? ResourceName,
        int? Month,
        int? Year,
        string PeriodLabel,
        string? Designation,
        List<DeliveryNoteDocumentItemDto> Items,
        double Amount,
        double Tva,
        double TotalTtc,
        string Currency,
        bool Signed
    );
}