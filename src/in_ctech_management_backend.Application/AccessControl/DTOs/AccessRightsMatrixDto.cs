namespace in_ctech_management_backend.Application.AccessControl.DTOs;

public record AccessRightsMatrixDto(
    List<ModuleDto> Modules,
    List<ActionDto> Actions,
    List<RoleMatrixDto> Roles);