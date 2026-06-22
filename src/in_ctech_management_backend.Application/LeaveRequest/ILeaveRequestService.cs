using in_ctech_management_backend.Application.LeaveRequest.DTOs;
using in_ctech_management_backend.Domain.LeaveRequests.Enums;

namespace in_ctech_management_backend.Application.LeaveRequest
{
    public interface ILeaveRequestService
    {
        Task<IReadOnlyList<LeaveRequestDto>> GetLeaveRequestsAsync(Guid? employeeId,LeaveRequestStatus? status,CancellationToken cancellationToken = default);
        /// <summary>
        /// managerId = null  → RH/Admin voit toutes les demandes
        /// managerId = valeur → Manager voit uniquement les demandes de son équipe
        /// </summary>
        Task<IReadOnlyList<LeaveRequestDto>> GetLeaveRequestsByManagerAsync(Guid? managerId, Guid? employeeId, LeaveRequestStatus? status, CancellationToken cancellationToken = default);
        Task<LeaveRequestDto> CreateAsync(Guid employeeId,CreateLeaveRequestDto dto,CancellationToken cancellationToken = default);
        Task<LeaveRequestPreviewDto> PreviewAsync(Guid employeeId, PreviewLeaveRequestDto dto, CancellationToken cancellationToken = default);
        Task<LeaveRequestDto> UpdateByCollaboratorAsync(Guid leaveRequestId, Guid employeeId, CreateLeaveRequestDto dto, CancellationToken cancellationToken = default);
        Task<LeaveRequestDto> CancelAsync(Guid leaveRequestId, Guid employeeId, CancellationToken cancellationToken = default);
        Task<LeaveRequestDto> UpdateStatusAsync(Guid leaveRequestId,string updatedBy,UpdateLeaveRequestStatusDto dto,CancellationToken cancellationToken = default);
    }
}