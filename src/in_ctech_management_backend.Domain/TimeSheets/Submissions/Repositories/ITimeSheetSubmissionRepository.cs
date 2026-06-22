using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.TimeSheets.Submissions.Enums;

namespace in_ctech_management_backend.Domain.TimeSheets.Submissions.Repositories
{
    public interface ITimeSheetSubmissionRepository
    {
        Task<TimeSheetSubmission?> GetByIdAsync(
            SubmissionId id,
            CancellationToken ct = default);

        Task<TimeSheetSubmission?> GetByEmployeeAndMonthAsync(
            EmployeeId employeeId,
            int year,
            int month,
            CancellationToken ct = default);

        Task<IReadOnlyList<TimeSheetSubmission>> GetByManagerAsync(
            EmployeeId managerId,
            int year,
            int? month,
            EmployeeId? employeeId,
            SubmissionStatus? status,
            CancellationToken ct = default);
        Task<IReadOnlyList<TimeSheetSubmission>> GetAllAsync(
            int year,
            int? month,
            EmployeeId? employeeId,
            SubmissionStatus? status,
            CancellationToken ct = default);

        Task<IReadOnlyList<TimeSheetSubmission>> GetByEmployeeAsync(
            EmployeeId employeeId,
            int year,
            int month,
            CancellationToken ct = default);

        Task CreateAsync(TimeSheetSubmission submission, CancellationToken ct = default);
        Task UpdateAsync(TimeSheetSubmission submission, CancellationToken ct = default);
        Task DeleteAsync(TimeSheetSubmission submission, CancellationToken ct = default);
    }
}
