namespace in_ctech_management_backend.Application.Role.DTOs
{
    /// <summary>
    /// Résumé d'un utilisateur avec ses rôles actuels.
    /// </summary>
    public record UserWithRolesDto(
        string UserId,
        string Email,
        string UserName,
        Guid? EmployeeId,
        List<string> Roles
    );

    /// <summary>
    /// Body pour assigner un ou plusieurs rôles à un utilisateur.
    /// </summary>
    public record AssignRolesDto(
        List<string> Roles  // ex: ["RH", "Manager"]
    );

    /// <summary>
    /// Résultat après modification des rôles.
    /// </summary>
    public record UpdateRolesResultDto(
        string UserId,
        string Email,
        List<string> Roles,        // rôles actuels après modification
        List<string> AddedRoles,   // rôles ajoutés
        List<string> RevokedRoles  // rôles révoqués
    );
}