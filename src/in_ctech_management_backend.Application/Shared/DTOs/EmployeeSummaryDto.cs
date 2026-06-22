namespace in_ctech_management_backend.Application.Shared.DTOs
{
    public record EmployeeSummaryDto
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = default!;
        public decimal AnnualConsumedLeaves { get; init; }
    }
}
