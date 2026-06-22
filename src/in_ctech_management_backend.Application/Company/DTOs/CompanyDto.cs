using System.Text.Json.Serialization;

namespace in_ctech_management_backend.Application.Company.DTOs
{
    public class CompanyResponseDto
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = default!;
        public string Nom { get; set; } = default!;
        public string? Adresse { get; set; }
        public string? Contact { get; set; }
        public string Code { get; set; } = default!;
        public string Pays { get; set; } = default!;
        public string SocietyType { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class CreateCompanyDto
    {
        public string Nom { get; set; } = default!;
        public string? Adresse { get; set; }
        public string? Contact { get; set; }
        public string Code { get; set; } = default!;
        public string Pays { get; set; } = default!;
        public string SocietyType { get; set; } = default!;
        public string? CreatedBy { get; set; }
    }

    public class UpdateCompanyDto
    {
        public string? Nom { get; set; }
        public string? Adresse { get; set; }
        public string? Contact { get; set; }
        public string? Code { get; set; }
        public string? Pays { get; set; }
        public string? SocietyType { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
