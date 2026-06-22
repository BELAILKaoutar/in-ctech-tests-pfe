namespace in_ctech_management_backend.Application.PuchaseOrder.DTOs
{
    public record CreatePurchaseOrderRequest(
        string DocType,
        string EngagementMode,
        Guid CompanyId,
        DateOnly StartDate,
        DateOnly EndDate,
        string PaymentMode,
        string Description,
        string? Reference,
        string CreatedBy,
        // Champs AT
        Guid? EmployeeId,
        double? DailyRate,
        double? TotalAmount,
        // Champs WP
        string? Designation,
        double? Quantity,
        double? UnitPrice
    );

}
