namespace in_ctech_management_backend.Application.Shared.DTOs
{
    public class EmployeeResponseDto
    {
        public string Id { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Trigram { get; set; } = default!;
        public double DailyRate { get; set; }
        public string ContractType { get; set; } = default!;
        public string? FreelancerType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
    }
}
