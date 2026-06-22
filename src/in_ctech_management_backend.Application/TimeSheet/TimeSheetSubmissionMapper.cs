using in_ctech_management_backend.Application.TimeSheets.DTOs;
using in_ctech_management_backend.Domain.TimeSheets;
using in_ctech_management_backend.Domain.TimeSheets.Submissions;

namespace in_ctech_management_backend.Application.TimeSheets
{
    public static class TimeSheetSubmissionMapper
    {
        public static TimeSheetSubmissionDto ToDto(
            TimeSheetSubmission submission,
            string? employeeName = null)
        {
            return new TimeSheetSubmissionDto(
                Id: (Guid)submission.Id,
                EmployeeId: (Guid)submission.EmployeeId,
                EmployeeName: employeeName,
                Year: submission.Year,
                Month: submission.Month,
                Status: submission.Status.ToString(),
                SubmittedAt: submission.SubmittedAt,
                SubmittedBy: submission.SubmittedBy,
                ReviewedAt: submission.ReviewedAt,
                ReviewedBy: submission.ReviewedBy,
                RejectionReason: submission.RejectionReason
            );
        }

        public static TimeSheetSubmissionDetailDto ToDetailDto(
            TimeSheetSubmission submission,
            IEnumerable<TimeSheet> sheets,
            Dictionary<Guid, string> projectNames,
            decimal imputedDays,
            decimal leaveDays,
            decimal workingDays,
            string? employeeName = null)
        {
            return new TimeSheetSubmissionDetailDto(
                Id: (Guid)submission.Id,
                EmployeeId: (Guid)submission.EmployeeId,
                EmployeeName: employeeName,
                Year: submission.Year,
                Month: submission.Month,
                Status: submission.Status.ToString(),
                Weeks: sheets
                    .OrderBy(s => s.WeekNumber)
                    .Select(s => ToWeekDto(s, projectNames))
                    .ToList(),
                SubmittedAt: submission.SubmittedAt,
                SubmittedBy: submission.SubmittedBy,
                ReviewedAt: submission.ReviewedAt,
                ReviewedBy: submission.ReviewedBy,
                RejectionReason: submission.RejectionReason,
                ImputedDays: imputedDays,
                LeaveDays: leaveDays,
                WorkingDays: workingDays
            );
        }

        private static SubmissionWeekDto ToWeekDto(
            TimeSheet sheet,
            Dictionary<Guid, string> projectNames)
        {
            return new SubmissionWeekDto(
                TimeSheetId: (Guid)sheet.TimeSheetId,
                WeekNumber: sheet.WeekNumber,
                WeekStartDate: sheet.WeekStartDate,
                WeekEndDate: sheet.WeekEndDate,
                ProjectTotals: sheet.Entries
                    .Select(e => new SubmissionProjectTotalDto(
                        ProjectId: (Guid)e.ProjectId,
                        ProjectName: projectNames.TryGetValue((Guid)e.ProjectId, out var name)
                            ? name
                            : "Projet inconnu",
                        TotalDays: e.TotalDays
                    ))
                    .OrderBy(p => p.ProjectName)
                    .ToList()
            );
        }
    }
}
