using in_ctech_management_backend.Domain.CreditNotes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Domain.CreditNotes.Repositories
{
    public interface ICreditNoteRepository
    {
        Task<CreditNote> GetByIdAsync(CreditNoteId id);
        Task<List<CreditNote>> GetAllAsync();
        Task AddAsync(CreditNote creditNote);
        Task UpdateAsync(CreditNote creditNote);
        Task DeleteAsync(CreditNoteId id);
        Task<string?> GetLastReferenceAsync(int year);
    }
}
