using in_ctech_management_backend.Application.LeaveRequest.DTOs;
using in_ctech_management_backend.Application.Shared.DTOs;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.LeaveRequests.Enums;
using DomainLeaveRequest = in_ctech_management_backend.Domain.LeaveRequests.LeaveRequest;

namespace in_ctech_management_backend.Application.LeaveRequest
{
    public static class LeaveRequestMapper
    {
        public static DomainLeaveRequest ToEntity(
            Guid employeeId,
            CreateLeaveRequestDto dto,
            IReadOnlySet<DateOnly>? excludedDates = null)
        {
            return DomainLeaveRequest.Create(
                new EmployeeId(employeeId),
                dto.LeaveType,
                dto.StartDate,
                dto.EndDate,
                dto.StartPeriod,
                dto.EndPeriod,
                reason: dto.Reason,
                createdBy: employeeId.ToString(),
                excludedDates: excludedDates
            );
        }
        public static LeaveRequestDto ToDto(DomainLeaveRequest lr, EmployeeSummaryDto? employee = null) => new()
        {
            Id = (Guid)lr.LeaveRequestId,
            Employee = employee,
            LeaveType = GetLeaveTypeLabel(lr.LeaveType),
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            StartPeriod = lr.StartPeriod,
            EndPeriod = lr.EndPeriod,
            NumberOfDays = lr.NumberOfDays,
            Reason = lr.Reason,
            Status = GetStatusLabel(lr.Status),
            RejectionReason = lr.RejectionReason,
            CreatedBy = lr.CreatedBy,
            UpdatedBy = lr.UpdatedBy,
            CreatedAt = lr.CreatedAt,
            UpdatedAt = lr.UpdatedAt
        };

        public static string GetLeaveTypeLabel(LeaveType type) => type switch
        {
            LeaveType.PaidLeave => "Congé payé",
            LeaveType.UnpaidLeave => "Congé sans solde",
            LeaveType.MaternityLeave => "Congé maternité",
            LeaveType.BirthLeave => "Congé naissance",
            LeaveType.BereavementLeave => "Congé décès",
            LeaveType.Sickness => "Maladie",
            LeaveType.WorkAccident => "Accident de travail",
            LeaveType.MarriageLeave => "Congé mariage",
            LeaveType.AdvancedPaidLeave => "Congé payé anticipé",
            _ => type.ToString()
        };
        public static string GetStatusLabel(LeaveRequestStatus status) => status switch
        {
            LeaveRequestStatus.Pending => "En attente",
            LeaveRequestStatus.Approved => "Validée",
            LeaveRequestStatus.Rejected => "Refusée",
            LeaveRequestStatus.Cancelled => "Annulée",
            _ => status.ToString()
        };
    }
}