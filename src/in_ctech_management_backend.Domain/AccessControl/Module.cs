namespace in_ctech_management_backend.Domain.AccessControl
{
    public class Module
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = default!;
        public string Code { get; private set; } = default!; // ex: "RESSOURCES", "PROJETS"

        private Module() { }

        public static Module Create(string name, string code) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code.ToUpperInvariant()
        };
    }
}
