namespace in_ctech_management_backend.Application.Invoices.DTOs
{
    public record InvoiceDocumentItemDto(
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        decimal Total,
        string? ResourceName,
        string? BillingMonthLabel,
        string? PurchaseOrderReference
    );

    public record InvoiceDocumentDataDto(
        string Reference,
        string FileName,
        string Template,
        string ClientName,
        string ClientAddress,
        string ClientCode,
        string ClientCountry,
        DateTime InvoiceDate,
        string? BCNumber,
        decimal Ht,
        decimal Tva,
        decimal Ttc,
        string Currency,
        string AmountInWords,
        bool Signed,
        List<InvoiceDocumentItemDto> Items
    );
}