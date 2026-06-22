using in_ctech_management_backend.Domain.Invoices;
using in_ctech_management_backend.Domain.Common;

namespace in_ctech_management_backend.Domain.CreditNotes
{
    public class CreditNote : AuditableEntity
    {
        public CreditNoteId Id { get; private set; }
        public string Reference { get; private set; }
        public InvoiceId? OriginalInvoiceId { get; private set; }
        public Invoice? OriginalInvoice { get; private set; }
        public bool IsFromInvoice { get; private set; }
        public string Designation { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal Quantity { get; private set; }
        public decimal VATRate { get; private set; }
        public DateTime InvoiceDate { get; private set; }
        public int BillingMonth { get; private set; }
        public string? SocieteId { get; private set; }
        public string? RessourceId { get; private set; }
        public string Currency { get; private set; } = "DH";
        public CreditNoteStatus Status { get; private set; } = CreditNoteStatus.Envoye;
        public string? Address { get; private set; }
        public string? CompanyCode { get; private set; }
        public string? CompanyName { get; private set; }
        private CreditNote() { }
        public static CreditNote CreateFromInvoice(
            string reference,
            InvoiceId originalInvoiceId,
            string designation,
            decimal unitPrice,
            decimal quantity,
            decimal vatRate,
            DateTime invoiceDate,
            int billingMonth,
            string? societeId,
            string? ressourceId,
            string currency = "€")
        {
            return new CreditNote
            {
                Id = CreditNoteId.CreateUnique(),
                Reference = reference,
                IsFromInvoice = true,
                OriginalInvoiceId = originalInvoiceId,
                Designation = designation,
                UnitPrice = unitPrice,
                Quantity = quantity < 0 ? quantity : -quantity,
                VATRate = vatRate,
                InvoiceDate = invoiceDate,
                BillingMonth = billingMonth,
                SocieteId = societeId,
                RessourceId = ressourceId,
                Currency = currency,
                Status = CreditNoteStatus.Envoye,
            };
        }

        public static CreditNote CreateManual(
            string reference,
            string designation,
            decimal unitPrice,
            decimal quantity,
            decimal vatRate,
            DateTime invoiceDate,
            int billingMonth,
            string? societeId,
            string? ressourceId,
            string currency = "€",
            string? address = null,
            string? companyCode = null,
            string? companyName = null)
        {
            return new CreditNote
            {
                Id = CreditNoteId.CreateUnique(),
                Reference = reference,
                IsFromInvoice = false,
                OriginalInvoiceId = null,
                Designation = designation,
                UnitPrice = unitPrice,
                Quantity = quantity < 0 ? quantity : -quantity,
                VATRate = vatRate,
                InvoiceDate = invoiceDate,
                BillingMonth = billingMonth,
                SocieteId = societeId,
                RessourceId = ressourceId,
                Currency = currency,
                Status = CreditNoteStatus.Envoye,
                Address = address,
                CompanyCode = companyCode,
                CompanyName = companyName,
            };
        }

        public void Update(
            string designation,
            decimal unitPrice,
            decimal quantity,
            decimal vatRate,
            DateTime invoiceDate,
            int billingMonth,
            string? currency = null,
            string? address = null,
            string? companyCode = null,
            string? companyName = null)
        {
            Designation = designation;
            UnitPrice = unitPrice;
            Quantity = quantity < 0 ? quantity : -quantity;
            VATRate = vatRate;
            InvoiceDate = invoiceDate;
            BillingMonth = billingMonth;
            if (currency != null) Currency = currency;
            if (address != null) Address = address;
            if (companyCode != null) CompanyCode = companyCode;
            if (companyName != null) CompanyName = companyName;
        }

        public void UpdateStatus(CreditNoteStatus status)
        {
            Status = status;
        }
    }
}