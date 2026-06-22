using in_ctech_management_backend.Domain.Employees;

namespace in_ctech_management_backend.Domain.TimeSheets
{
    public interface ITimeSheetRepository
    {
        Task<TimeSheet?> GetByIdAsync(TimeSheetId id,CancellationToken ct = default);
        Task<TimeSheet?> GetByEmployeeAndWeekAsync(EmployeeId employeeId,int year,int weekNumber,CancellationToken ct = default);
        // pour afficher toutes les semaines du mois
        Task<IReadOnlyList<TimeSheet>> GetByEmployeeAndMonthAsync(EmployeeId employeeId,int year,int month,CancellationToken ct = default);
        // batch : toutes les feuilles pour un ensemble d'employés / mois (vue manager)
        Task<IReadOnlyList<TimeSheet>> GetByEmployeesAndMonthsAsync(IReadOnlyCollection<Guid> employeeIds,int year,IReadOnlyCollection<int> months,CancellationToken ct = default);
        // batch par plage de dates : inclut les semaines à cheval sur deux mois (vue manager)
        Task<IReadOnlyList<TimeSheet>> GetByEmployeesAndDateRangeAsync(IReadOnlyCollection<Guid> employeeIds,DateOnly from,DateOnly to,CancellationToken ct = default);
        Task CreateTimeSheetAsync(TimeSheet timeSheet, CancellationToken ct = default);
        Task UpdateTimeSheetAsync(TimeSheet timeSheet, CancellationToken ct = default);
        Task DeleteTimeSheetAsync(TimeSheet timeSheet, CancellationToken ct = default);
        Task UpdateRangeAsync(IEnumerable<TimeSheet> timeSheets, CancellationToken ct = default);
        Task<IReadOnlyList<TimeSheet>> GetByEmployeeAndDateRangeAsync(EmployeeId employeeId,DateOnly from,DateOnly to,CancellationToken ct = default);
        // pour les jours fériés (tous les employés)
        Task<IReadOnlyList<TimeSheet>> GetAllByDateRangeAsync(DateOnly from,DateOnly to,CancellationToken ct = default);
    }
}
