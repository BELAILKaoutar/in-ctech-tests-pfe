using in_ctech_management_backend.Application.Company.DTOs;
using in_ctech_management_backend.Application.PuchaseOrder.DTOs;

namespace in_ctech_management_backend.Application.PurchaseOrder.DTOs
{
    public record PurchaseOrderDto(
        Guid Id,
        string DocType,
        string Reference,
        string EngagementMode,
        string PaymentMode,
        string Status,
        string Description,
        double TotalAmount,
        decimal ConsumedAmount,
        decimal RemainingAmount,
        decimal RemainingPercentage,
        int DaysRemaining,
        DateOnly StartDate,
        DateOnly EndDate,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? CreatedBy,
        string? UpdatedBy,
        double? DailyRate,
        string? Designation,
        double? Quantity,
        double? UnitPrice,
        CompanyResponseDto? Client,
        EmployeeSummaryDto? Employee,
        PurchaseOrderMarginDto? Margin
    );
}
