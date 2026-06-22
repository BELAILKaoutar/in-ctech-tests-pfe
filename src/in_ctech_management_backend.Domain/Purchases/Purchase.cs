using in_ctech_management_backend.Domain.Common;
using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.Employees;
using System.Globalization;
using System.Text;

namespace in_ctech_management_backend.Domain.Purchases
{
    public class Purchase : AuditableEntity
    {
        public PurchaseId Id { get; private set; }
        public string Reference { get; private set; }
        public EmployeeId? ResourceId { get; private set; }
        public Employee? Resource { get; private set; }
        public CompanyId? ClientId { get; private set; }
        public Company? Client { get; private set; }
        public DateTime PresMonth { get; private set; }
        public decimal Ttc { get; private set; }
        public decimal Tva { get; private set; }
        public decimal Ht { get; private set; }
        public DateTime InvoiceDate { get; private set; }
        public string Type { get; private set; }
        public string Status { get; private set; }
        public DateTime? PaymentDate { get; private set; }
        public string? PaiementMode { get; private set; }

        private Purchase() { }

        public static Purchase Create(
            string reference,
            EmployeeId? resourceId,
            CompanyId? clientId,
            DateTime presMonth,
            decimal ttc,
            decimal tva,
            decimal ht,
            DateTime invoiceDate,
            string type,
            string? status,
            string? paiementMode)
        {
            return new Purchase
            {
                Id = new PurchaseId(Guid.NewGuid()),
                Reference = reference,
                ResourceId = resourceId,
                ClientId = clientId,
                PresMonth = presMonth,
                Ttc = ttc,
                Tva = tva,
                Ht = ht,
                InvoiceDate = invoiceDate,
                Type = type,
                Status = string.IsNullOrWhiteSpace(status) ? "En attente" : NormalizeStatus(status),
                PaiementMode = paiementMode,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Update(
            string? reference,
            EmployeeId? resourceId,
            CompanyId? clientId,
            DateTime? presMonth,
            decimal? ttc,
            decimal? tva,
            decimal? ht,
            DateTime? invoiceDate,
            string? type,
            string? status,
            string? paiementMode)
        {
            if (reference is not null) Reference = reference;
            if (resourceId is not null) ResourceId = resourceId;
            if (clientId is not null) ClientId = clientId;
            if (presMonth.HasValue) PresMonth = presMonth.Value;
            if (ttc.HasValue) Ttc = ttc.Value;
            if (tva.HasValue) Tva = tva.Value;
            if (ht.HasValue) Ht = ht.Value;
            if (invoiceDate.HasValue) InvoiceDate = invoiceDate.Value;
            if (type is not null) Type = type;
            if (!string.IsNullOrWhiteSpace(status))
            {
                Status = NormalizeStatus(status);
                if (Status != "Payée")
                {
                    PaymentDate = null;
                }
            }
            if (paiementMode is not null) PaiementMode = paiementMode;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ConfirmPayment(DateTime paymentDate)
        {
            PaymentDate = paymentDate;
            Status = "Payée";
            UpdatedAt = DateTime.UtcNow;
        }

        private static string NormalizeStatus(string status)
        {
            var normalized = string.Join(
                " ",
                RemoveDiacritics(status)
                    .Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();

            return normalized switch
            {
                "en attente" => "En attente",
                "non paye" or "non payee" => "Non Payée",
                "paye" => "Payée",
                _ => throw new DomainException($"Invalid purchase status: {status}")
            };
        }

        private static string RemoveDiacritics(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
