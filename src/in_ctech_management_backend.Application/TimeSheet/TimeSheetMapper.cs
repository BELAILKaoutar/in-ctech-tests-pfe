using in_ctech_management_backend.Application.TimeSheets.DTOs;
using in_ctech_management_backend.Domain.Projects;
using in_ctech_management_backend.Domain.TimeSheets;
using DomainTimeSheet = in_ctech_management_backend.Domain.TimeSheets.TimeSheet;
using DomainTimeEntry = in_ctech_management_backend.Domain.TimeSheets.TimeEntry;

namespace in_ctech_management_backend.Application.TimeSheets
{
    public static class TimeSheetMapper
    {
        public static DomainTimeEntry ToEntryEntity(
            TimeSheetId timeSheetId,
            ProjectId projectId,
            Dictionary<DateOnly, decimal>? dailyValues = null)
        {
            return DomainTimeEntry.Create(
                timeSheetId,
                projectId,
                dailyValues ?? new Dictionary<DateOnly, decimal>()
            );
        }
        public static TimeSheetDto ToEmptyDto(
            Guid employeeId,
            int year,
            int month,
            int weekNumber,
            DateOnly weekStart,
            DateOnly weekEnd,
            IEnumerable<Project> projects,
            List<DateOnly> holidayDays,
            Dictionary<DateOnly, decimal> absenceDays,
            List<DateOnly> nonEditableDays)
        {
            return new TimeSheetDto
            {
                TimeSheetId = Guid.Empty,
                EmployeeId = employeeId,
                Year = year,
                Month = month,
                WeekNumber = weekNumber,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd,
                TotalWeekDays = 0,
                HolidayDays = holidayDays,
                AbsenceDays = absenceDays.ToDictionary(
                             kvp => kvp.Key.ToString("yyyy-MM-dd"),
                             kvp => kvp.Value),
                NonEditableDays = nonEditableDays,
                Entries = projects.Select(p => new TimeEntryDto
                {
                    TimeEntryId = Guid.Empty,
                    ProjectId = (Guid)p.ProjectId,
                    ProjectName = p.Name,
                    TotalDays = 0,
                    DailyValues = new Dictionary<string, decimal>()
                }).ToList()
            };
        }

        public static TimeSheetDto ToDto(
            DomainTimeSheet ts,
            Dictionary<Guid, string> projectNames,
            List<DateOnly> holidayDays,
            Dictionary<DateOnly, decimal> absenceDays,
            List<DateOnly> nonEditableDays)
        {
            return new TimeSheetDto
            {
                TimeSheetId = (Guid)ts.TimeSheetId,
                EmployeeId = (Guid)ts.EmployeeId,
                Year = ts.Year,
                Month = ts.Month,
                WeekNumber = ts.WeekNumber,
                WeekStartDate = ts.WeekStartDate,
                WeekEndDate = ts.WeekEndDate,
                TotalWeekDays = ts.TotalWeekDays,
                HolidayDays = holidayDays,
                AbsenceDays = absenceDays.ToDictionary(
                              kvp => kvp.Key.ToString("yyyy-MM-dd"),
                              kvp => kvp.Value),
                NonEditableDays = nonEditableDays,
                Entries = ts.Entries.Select(e => ToEntryDto(e, projectNames)).ToList(),
                CreatedBy = ts.CreatedBy,
                UpdatedBy = ts.UpdatedBy,
                CreatedAt = ts.CreatedAt,
                UpdatedAt = ts.UpdatedAt
            };
        }
        private static TimeEntryDto ToEntryDto(
            DomainTimeEntry entry,
            Dictionary<Guid, string> projectNames)
        {
            var projectId = (Guid)entry.ProjectId;

            return new TimeEntryDto
            {
                TimeEntryId = (Guid)entry.TimeEntryId,
                ProjectId = projectId,
                ProjectName = projectNames.TryGetValue(projectId, out var name)
                                  ? name
                                  : "Projet inconnu",
                TotalDays = entry.TotalDays,
                DailyValues = entry.DailyValues.ToDictionary(
                    kvp => kvp.Key.ToString("yyyy-MM-dd"),
                    kvp => kvp.Value
                )
            };
        }


    }
}