using in_ctech_management_backend.Application.Shared.DTOs;

namespace in_ctech_management_backend.Application.Shared
{
    public interface IEmployeeInfosService
    {
        /// <summary>
        /// Retourne le nom, l'email du collaborateur et l'email de son manager.
        /// </summary>
        Task<CollaboratorWithManagerInfo> GetCollaboratorWithManagerEmailAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);
        Task<EmployeeInfosDto> GetCollaboratorEmailAsync(Guid employeeId, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, string>> GetEmployeeNamesByIdsAsync(List<Guid> employeeIds, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, EmployeeSummaryDto>> GetEmployeeSummariesByIdsAsync(List<Guid> employeeIds, CancellationToken cancellationToken = default);
    }

    public record CollaboratorWithManagerInfo(
        string? FullName,
        string? Email,
        string? ManagerEmail);

    public record EmployeeInfosDto
    {
        public string? FullName { get; init; }
        public string? Email { get; init; }
        public string? ManagerEmail { get; init; }
    }
}