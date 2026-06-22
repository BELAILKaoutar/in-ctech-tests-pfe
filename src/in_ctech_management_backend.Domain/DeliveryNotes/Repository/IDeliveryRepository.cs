using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.DeliveryNotes.Enums;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Projects;


namespace in_ctech_management_backend.Domain.DeliveryNotes.Repository
{
    public interface IDeliveryNoteRepository
    {
        Task<DeliveryNote?> GetByIdAsync(DeliveryNoteId id, CancellationToken cancellationToken = default);
        Task<List<DeliveryNote>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<DeliveryNote>> GetByCompanyAsync(CompanyId societeId, CancellationToken cancellationToken = default);
        Task<List<DeliveryNote>> GetByEmployeeAsync(EmployeeId employeeId, CancellationToken cancellationToken = default);
        Task<List<DeliveryNote>> GetByTypeAsync(DeliveryNoteType type, CancellationToken cancellationToken = default);
        Task<List<DeliveryNote>> SearchByReferenceAsync(string reference, CancellationToken cancellationToken = default);
        Task<List<DeliveryNote>> GetByProjectAsync(ProjectId projectId, CancellationToken cancellationToken = default);
        Task<string?> GetLatestReferenceAsync(CancellationToken cancellationToken = default);
        Task AddAsync(DeliveryNote deliveryNote, CancellationToken cancellationToken = default);
        Task DeleteAsync(DeliveryNoteId id, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
