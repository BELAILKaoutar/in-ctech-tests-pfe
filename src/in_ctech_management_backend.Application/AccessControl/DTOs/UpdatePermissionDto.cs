namespace in_ctech_management_backend.Application.AccessControl.DTOs;

public record UpdatePermissionDto(
    string RoleName,
    Guid ModuleId,
    Guid ActionId,
    bool IsAllowed);