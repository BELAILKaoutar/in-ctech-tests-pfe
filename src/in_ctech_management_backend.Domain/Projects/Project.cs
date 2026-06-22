using in_ctech_management_backend.Domain.Common;
using in_ctech_management_backend.Domain.PurchaseOrders;

namespace in_ctech_management_backend.Domain.Projects
{
    public class Project : AuditableEntity
    {
        public ProjectId ProjectId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        private Project()
        {
            ProjectId = new ProjectId(Guid.NewGuid());
            Name = string.Empty;
            Description = string.Empty;
        }

        private Project(string name, string description, string? createdBy)
        {
            ProjectId = new ProjectId(Guid.NewGuid());
            Name = name;
            Description = description;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy;
        }

        public static Project Create(string name, string description, string? createdBy = null)
        {
            ValidateName(name);

            return new Project(
                name.Trim(),
                description?.Trim() ?? string.Empty,
                createdBy
            );
        }

        public void Update(string newName, string newDescription, string? updatedBy = null)
        {
            ValidateName(newName);

            Name = newName.Trim();
            Description = newDescription?.Trim() ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Project name is required.");

            if (name.Trim().Length > 50)
                throw new DomainException("Project name cannot exceed 50 characters.");
        }
    }
}