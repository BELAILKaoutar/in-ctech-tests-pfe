using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.HrDocumentRequests.Enums;

namespace in_ctech_management_backend.Domain.HrDocumentRequests.Repositories;

public interface IHrDocumentRequestRepository
{
    Task<HrDocumentRequest?> GetByIdAsync(HrDocumentRequestId id,CancellationToken ct = default);

    Task<IEnumerable<HrDocumentRequest>> GetByEmployeeAsync(EmployeeId employeeId,int? year,HrDocumentRequestStatus? status,CancellationToken ct = default);

    Task<IEnumerable<HrDocumentRequest>> GetAllAsync(int? year,HrDocumentRequestStatus? status,CancellationToken ct = default);

    Task AddAsync(HrDocumentRequest request, CancellationToken ct = default);
    Task UpdateAsync(HrDocumentRequest request, CancellationToken ct = default);
}