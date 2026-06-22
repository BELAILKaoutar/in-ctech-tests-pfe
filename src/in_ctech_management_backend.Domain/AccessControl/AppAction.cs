namespace in_ctech_management_backend.Domain.AccessControl
{
    /// <summary>
    /// Action possible sur un module : VIEW, CREATE, UPDATE, DELETE, VALIDATE…
    /// </summary>
    public class AppAction
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = default!;
        public string Code { get; private set; } = default!; // ex: "VIEW", "CREATE"

        private AppAction() { }

        public static AppAction Create(string name, string code) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code.ToUpperInvariant()
        };
    }
}
