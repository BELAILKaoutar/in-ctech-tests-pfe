using in_ctech_management_backend.Domain.Invoices.Enums;

namespace in_ctech_management_backend.Application.Invoices.DTOs
{
    public class UpdateInvoiceDto
    {
        public string? EmployeeId { get; set; }
        public string? CompanyId { get; set; }
        public string? ProjectId { get; set; }
        public string? PurchaseOrderReference { get; set; }
        public decimal? TotalExcludingTax { get; set; }
        public decimal? VATRate { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public int? BillingMonth { get; set; }
        public int? BillingYear { get; set; }
        public int? Quantity { get; set; }
        public InvoiceType? Type { get; set; }
        public List<InvoiceWPItemDto>? WPItems { get; set; }
        public string? UpdatedBy { get; set; }
    }
}