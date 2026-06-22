using in_ctech_management_backend.Application.Company.DTOs;
using in_ctech_management_backend.Application.PuchaseOrder.DTOs;

namespace in_ctech_management_backend.Application.DeliveryNote.DTOs
{
    public record DeliveryNoteDto(
        Guid Id,
        string Reference,
        string Type,
        double Quantity,
        double Amount,
        DateTime InvoiceDate,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? CreatedBy,
        string? UpdatedBy,
        int? Month,
        int? Year,
        string? Designation,
        double? UnitPrice,
        string? PurchaseOrderId,
        CompanyResponseDto? Client,
        EmployeeSummaryDto? Employee
    );
}
