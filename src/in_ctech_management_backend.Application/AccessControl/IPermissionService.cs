namespace in_ctech_management_backend.Application.AccessControl
{
    public interface IPermissionService
    {
        /// <summary>
        /// Retourne toutes les permissions d'un utilisateur
        /// sous forme "MODULE_ACTION" (ex: "RESSOURCES_VIEW").
        /// Union de tous ses rôles.
        /// </summary>
        Task<IReadOnlyList<string>> GetUserPermissionsAsync(
            string userId,
            CancellationToken ct = default);

        /// <summary>
        /// Vérifie si un utilisateur possède une permission spécifique.
        /// </summary>
        Task<bool> HasPermissionAsync(
            string userId,
            string permissionCode,
            CancellationToken ct = default);
    }
}
