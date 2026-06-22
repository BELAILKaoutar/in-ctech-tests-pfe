namespace in_ctech_management_backend.Application.PuchaseOrder.DTOs
{
    public record UpdatePurchaseOrderRequest(
        Guid CompanyId,
        DateOnly StartDate,
        DateOnly EndDate,
        string EngagementMode,
        string PaymentMode,
        string Description,
        string? Reference,
        string UpdatedBy,
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
