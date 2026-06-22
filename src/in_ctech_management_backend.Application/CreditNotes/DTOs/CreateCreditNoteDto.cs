using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.CreditNotes.DTOs
{
    public record CreateCreditNoteDto(
        bool IsFromInvoice,
        string? OriginalInvoiceReference = null,
        string? Designation = null,
        decimal? UnitPrice = null,
        decimal? Quantity = null,
        decimal? VATRate = null,
        DateTime? InvoiceDate = null,
        int? BillingMonth = null,
        string? SocieteId = null,
        string? RessourceId = null,
        string? Currency = null,
        string? Address = null,
        string? CompanyCode = null,
        string? CompanyName = null
    );
}