namespace in_ctech_management_backend.Domain.AccessControl
{
    public class RolePermission
    {
        public Guid Id { get; private set; }
        public string RoleName { get; private set; } = default!; // ex: "RH", "Manager"
        public Guid ModuleId { get; private set; }
        public Guid ActionId { get; private set; }
        public bool IsAllowed { get; private set; }
        public Module? Module { get; private set; }
        public AppAction? Action { get; private set; }

        private RolePermission() { }

        public static RolePermission Create(
            string roleName,
            Guid moduleId,
            Guid actionId,
            bool isAllowed = false) => new()
            {
                Id = Guid.NewGuid(),
                RoleName = roleName,
                ModuleId = moduleId,
                ActionId = actionId,
                IsAllowed = isAllowed
            };

        /// <summary>Appelé quand l'admin coche ou décoche une case.</summary>
        public void SetAllowed(bool isAllowed) => IsAllowed = isAllowed;
    }
}
