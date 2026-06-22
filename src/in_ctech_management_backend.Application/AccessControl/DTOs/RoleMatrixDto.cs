namespace in_ctech_management_backend.Application.AccessControl.DTOs;

public record RoleMatrixDto(
    string RoleName,
    List<PermissionCellDto> Permissions);