using System;

namespace in_ctech_management_backend.Domain.Invoices
{
    public class InvoiceId
    {
        public Guid Value { get; private set; }

        private InvoiceId(Guid value)
        {
            Value = value;
        }

        public static InvoiceId CreateUnique()
        {
            return new InvoiceId(Guid.NewGuid());
        }

        public static InvoiceId FromGuid(Guid id)
        {
            return new InvoiceId(id);
        }

        public static InvoiceId FromString(string value)
        {
            return new InvoiceId(Guid.Parse(value));
        }
    }
}