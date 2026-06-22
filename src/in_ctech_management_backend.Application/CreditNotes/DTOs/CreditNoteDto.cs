using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.CreditNotes.DTOs
{
    public record CreditNoteDto(
        string Id,
        string Reference,
        bool IsFromInvoice,
        string? OriginalInvoiceReference,
        string? OriginalInvoiceType,
        string Designation,
        decimal UnitPrice,
        decimal Quantity,
        decimal VATRate,
        DateTime InvoiceDate,
        int BillingMonth,
        string? PurchaseOrderReference,
        string? EmployeeFullName,
        string? EmployeeTrigram,
        string? CompanyName,
        string? CompanyAddress,
        string? CompanyCountry,
        string Currency,
        string? Address,
        string? CompanyCode,
        string Status,
        string CreatedBy,
        DateTime CreatedAt,
        string? UpdatedBy,
        DateTime? UpdatedAt,
        string? DeletedBy,
        DateTime? DeletedAt
    );
}