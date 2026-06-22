using System.Text.Json.Serialization;

namespace in_ctech_management_backend.Application.PurchaseOrder.DTOs
{
    public record PurchaseOrderDocumentPartyDto(
        [property: JsonPropertyName("_id")] string Id,
        string Nom,
        string? Adresse,
        string Code,
        string Pays
    );

    public record PurchaseOrderDocumentEmployeeDto(
        Guid Id,
        string FullName
    );

    public record PurchaseOrderDocumentDataDto(
        Guid Id,
        string Reference,
        string FileName,
        string Template,
        DateTime CreatedAt,
        string DocType,
        string EngagementMode,
        string PaymentMode,
        DateOnly StartDate,
        DateOnly EndDate,
        string Description,
        double TotalAmount,
        double? DailyRate,
        string? Designation,
        double? Quantity,
        double? UnitPrice,
        PurchaseOrderDocumentPartyDto? Client,
        PurchaseOrderDocumentPartyDto? Supplier,
        PurchaseOrderDocumentEmployeeDto? Employee
    );
}