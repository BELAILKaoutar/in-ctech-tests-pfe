using in_ctech_management_backend.Application.CreditNotes.DTOs;
using in_ctech_management_backend.Application.Invoices.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.CreditNotes
{
    public interface ICreditNoteService
    {
        Task<CreditNoteDto> CreateCreditNoteAsync(CreateCreditNoteDto dto, string createdBy);
        Task<List<CreditNoteDto>> GetAllCreditNotesAsync();
        Task<CreditNoteDto?> GetCreditNoteByIdAsync(string id);
        Task<bool> DeleteCreditNoteAsync(string id, string deletedBy);
        Task<CreditNoteDocumentDataDto?> GetCreditNoteDocumentDataAsync(string id, string? template, bool signed, CancellationToken cancellationToken = default);
        Task<CreditNoteDto?> UpdateCreditNoteAsync(string id, UpdateCreditNoteDto dto, string updatedBy);
        Task<CreditNoteDto?> UpdateCreditNoteStatusAsync(string id, string status, string updatedBy);
    }
}
