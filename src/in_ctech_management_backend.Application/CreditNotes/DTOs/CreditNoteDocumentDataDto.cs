using in_ctech_management_backend.Application.Invoices.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.CreditNotes.DTOs
{
    public record CreditNoteDocumentDataDto(
        string Reference,
        string? OriginalInvoiceReference,
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
        List<InvoicePdfDataItemDto> Items
    );
}
