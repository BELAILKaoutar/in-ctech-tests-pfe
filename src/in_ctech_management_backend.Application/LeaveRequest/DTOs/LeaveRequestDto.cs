using in_ctech_management_backend.Application.Shared.DTOs;
using in_ctech_management_backend.Domain.LeaveRequests.Enums;

namespace in_ctech_management_backend.Application.LeaveRequest.DTOs
{
    public record LeaveRequestDto
    {
        public Guid Id { get; init; }
        public EmployeeSummaryDto? Employee { get; init; }
        public string LeaveType { get; init; } = default!;
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public DayPeriod StartPeriod { get; init; }
        public DayPeriod EndPeriod { get; init; }
        public decimal NumberOfDays { get; init; }
        public string Reason { get; init; } = default!;
        public string Status { get; init; } = default!;
        public string? RejectionReason { get; init; }
        public string CreatedBy { get; init; } = default!;
        public string? UpdatedBy { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
