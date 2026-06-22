using in_ctech_management_backend.Application.Role.DTOs;

namespace in_ctech_management_backend.Application.Role
{
    public interface IRoleService
    {
        Task<List<UserWithRolesDto>> GetAllUsersWithRolesAsync(CancellationToken ct = default);
        Task<UserWithRolesDto> GetUserRolesAsync(string userId,CancellationToken ct = default);
        Task<UpdateRolesResultDto> UpdateUserRolesAsync(string userId,AssignRolesDto dto,CancellationToken ct = default);
        Task RevokeRoleAsync(string userId,string roleName,CancellationToken ct = default);
        Task<List<string>> GetAvailableRolesAsync(CancellationToken ct = default);
    }
}