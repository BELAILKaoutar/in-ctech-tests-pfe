using System.Text.Json.Serialization;
using in_ctech_management_backend.Application.Company.DTOs;
using in_ctech_management_backend.Application.Shared.DTOs;

namespace in_ctech_management_backend.Application.Purchase.DTOs
{
    public class PurchaseResponseDto
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = default!;
        public string Reference { get; set; } = default!;
        public EmployeeResponseDto? Resource { get; set; }
        public CompanyResponseDto? Client { get; set; }
        public DateTime PresMonth { get; set; }
        public decimal Ttc { get; set; }
        public decimal Tva { get; set; }
        public decimal Ht { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Type { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime? PaymentDate { get; set; }
        public string? PaiementMode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class PurchaseRequestDto
    {
        public string? Reference { get; set; }
        public string? Resource { get; set; }
        public string? Client { get; set; }
        public DateTime? PresMonth { get; set; }
        public decimal? Ttc { get; set; }
        public decimal? Tva { get; set; }
        public decimal? Ht { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? PaiementMode { get; set; }
    }

    public class PurchaseConfirmPaymentDto
    {
        public DateTime PaymentDate { get; set; }
    }
}
