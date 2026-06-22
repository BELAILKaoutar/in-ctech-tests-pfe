using in_ctech_management_backend.Domain.Common;

namespace in_ctech_management_backend.Domain.Jobs
{
    public class Job : AuditableEntity
    {
        public JobId JobId { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        private Job()
        {
            JobId = new JobId(Guid.NewGuid());
            Title = string.Empty;
        }
        private Job(string title, string description, string? createdBy)
        {
            JobId = new JobId(Guid.NewGuid());
            Title = title;
            Description = description;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy;
        }
        public static Job Create(string title, string description, string? createdBy = null)
        {
            ValidateTitle(title);
            return new Job(title, description, createdBy);
        }
        public void Update(string newTitle, string? newDescription, string? updatedBy = null)
        {
            ValidateTitle(newTitle);

            Title = newTitle;

            if (newDescription is not null)

                Description = newDescription;
                UpdatedAt = DateTime.UtcNow;
                UpdatedBy = updatedBy;
        }
        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Job title is required");
            if (title.Length > 100)
                throw new DomainException("Job title cannot exceed 100 characters");
        }

    }
}
