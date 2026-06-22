namespace in_ctech_management_backend.Application.AccessControl.DTOs;

public record PermissionCellDto(
    Guid ModuleId,
    Guid ActionId,
    bool IsAllowed);