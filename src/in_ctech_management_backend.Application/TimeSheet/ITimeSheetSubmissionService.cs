using in_ctech_management_backend.Application.TimeSheets.DTOs;
using in_ctech_management_backend.Domain.TimeSheets.Submissions.Enums;

namespace in_ctech_management_backend.Application.TimeSheets
{
    public interface ITimeSheetSubmissionService
    {
        Task<TimeSheetSubmissionDetailDto> SubmitMonthAsync(Guid employeeId, int year, int month, CancellationToken ct = default);
        Task<IReadOnlyList<TimeSheetSubmissionDetailDto>> GetByManagerAsync(Guid? managerId, int? month, Guid? employeeId, SubmissionStatus? status, CancellationToken ct = default);
        Task<IReadOnlyList<TimeSheetSubmissionDetailDto>> GetByEmployeeAndMonthAsync(Guid employeeId, int month, CancellationToken ct = default);
        Task<TimeSheetSubmissionDetailDto> GetByIdAsync(Guid submissionId, CancellationToken ct = default);
        Task<TimeSheetSubmissionDto> UpdateStatusAsync(Guid submissionId, string reviewedBy, UpdateSubmissionStatusDto dto, CancellationToken ct = default);
    }
}
