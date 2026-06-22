using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.CreditNotes.DTOs
{
    public record UpdateCreditNoteDto(
        decimal Quantity,
        string? Designation = null,
        decimal? UnitPrice = null,
        decimal? VATRate = null,
        DateTime? InvoiceDate = null,
        int? BillingMonth = null,
        string? Currency = null,
        string? Status = null,
        string? CompanyName = null,
        string? CompanyCode = null,
        string? Address = null
    );
}