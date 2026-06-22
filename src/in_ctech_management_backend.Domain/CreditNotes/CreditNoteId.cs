using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Domain.CreditNotes
{
    public class CreditNoteId
    {
        public Guid Value { get; private set; }

        private CreditNoteId(Guid value)
        {
            Value = value;
        }

        public static CreditNoteId CreateUnique()
        {
            return new CreditNoteId(Guid.NewGuid());
        }

        public static CreditNoteId FromGuid(Guid id)
        {
            return new CreditNoteId(id);
        }

        public static CreditNoteId FromString(string value)
        {
            return new CreditNoteId(Guid.Parse(value));
        }
    }
}
