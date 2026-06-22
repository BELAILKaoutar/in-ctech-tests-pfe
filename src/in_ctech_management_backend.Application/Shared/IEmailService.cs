using in_ctech_management_backend.Domain.LeaveRequests.Enums;

namespace in_ctech_management_backend.Application.Shared
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendWelcomeEmail(string to, Dictionary<string, string> replacements);
        Task SendEmployeeWelcomeEmailAsync(string to, string fullName, string password);
        Task SendLeaveRequestCreatedEmailAsync(string managerEmail,string collaboratorName,string collaboratorEmail,string leaveType,DateOnly startDate,DateOnly endDate,decimal numberOfDays,string status);
        Task SendLeaveRequestApprovedEmailAsync(
            string collaboratorEmail,
            string collaboratorName,
            LeaveType leaveType,
            DateOnly startDate,
            DateOnly endDate,
            decimal numberOfDays);

        Task SendLeaveRequestRejectedEmailAsync(
            string collaboratorEmail,
            string collaboratorName,
            LeaveType leaveType,
            DateOnly startDate,
            DateOnly endDate,
            decimal numberOfDays,
            string rejectionReason);

        Task SendLeaveRequestUpdatedEmailAsync(
            string managerEmail,
            string collaboratorName,
            string collaboratorEmail,
            string leaveType,
            DateOnly startDate,
            DateOnly endDate,
            decimal numberOfDays,
            string status);

        Task SendLeaveRequestCancelledEmailAsync(
            string managerEmail,
            string collaboratorName,
            string collaboratorEmail,
            string leaveType,
            DateOnly startDate,
            DateOnly endDate,
            decimal numberOfDays);

        Task SendTimeSheetSubmittedEmailAsync(
            string managerEmail,
            string collaboratorName,
            string collaboratorEmail,
            int year,
            int month,
            DateTime submittedAt);

        Task SendTimeSheetToCorrectEmailAsync(
            string collaboratorEmail,
            string collaboratorName,
            int year,
            int month,
            DateTime reviewedAt,
            string reason);

        Task SendPasswordResetEmailAsync(
            string to,
            string fullName,
            string resetLink);

        Task SendHrDocumentRequestCreatedEmailAsync(
            string managerEmail,
            string collaboratorName,
            string collaboratorEmail,
            string documentType,
            DateTime requestDate);

        Task SendHrDocumentRequestUpdatedEmailAsync(
            string managerEmail,
            string collaboratorName,
            string collaboratorEmail,
            string documentType,
            DateTime updatedAt);

        Task SendHrDocumentRequestCancelledEmailAsync(
            string managerEmail,
            string collaboratorName,
            string collaboratorEmail,
            string documentType,
            DateTime cancelledAt);

    }
}

