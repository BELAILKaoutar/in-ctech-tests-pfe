using in_ctech_management_backend.Domain.Invoices.Enums;

namespace in_ctech_management_backend.Application.Invoices.DTOs
{
    public class InvoiceDto
    {
        public string Reference { get; set; }
        public string? EmployeeId { get; set; }
        public string CompanyId { get; set; }
        public string? DeliveryNoteId { get; set; }
        public string? PurchaseOrderReference { get; set; }
        public string? ProjectId { get; set; }
        public decimal TotalExcludingTax { get; set; }
        public decimal VATRate { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        public int Quantity { get; set; }
        public InvoiceType Type { get; set; }
        public string Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? TransferReference { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public ResourceDto? Resource { get; set; }
        public ClientDto? Client { get; set; }
        public List<InvoiceWPItemDto>? WPItems { get; set; }
    }

    public class ResourceDto
    {
        public string FullName { get; set; }
        public string? Trigram { get; set; }
    }

    public class ClientDto
    {
        public string Nom { get; set; }
        public string? Adresse { get; set; }
        public string? Pays { get; set; }
    }
}