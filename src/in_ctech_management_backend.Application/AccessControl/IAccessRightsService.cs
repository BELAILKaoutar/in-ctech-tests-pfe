using in_ctech_management_backend.Application.AccessControl.DTOs;
namespace in_ctech_management_backend.Application.AccessControl
{
    public interface IAccessRightsService
    {
        Task<AccessRightsMatrixDto> GetMatrixAsync(CancellationToken ct = default);
        Task UpdateAsync(List<UpdatePermissionDto> updates, CancellationToken ct = default);
    }
}
