using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.Invoices.DTOs
{
    public record InvoicePdfDataDto(
        string Reference,
        string FileName,
        string Template,
        string ClientName,
        string ClientAddress,
        string ClientCode,
        string ClientCountry,
        string InvoiceDate,
        string? BcNumber,
        decimal Ht,
        decimal Tva,
        decimal Ttc,
        string Currency,
        string AmountInWords,
        bool Signed,
        List<InvoicePdfDataItemDto> Items,
        string? OriginalInvoiceReference = null
    );

    public record InvoicePdfDataItemDto(
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        decimal Total,
        string? ResourceName = null,
        string? BillingMonthLabel = null,
        string? PurchaseOrderReference = null
    );
}
