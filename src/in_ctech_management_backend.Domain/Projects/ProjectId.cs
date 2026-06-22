namespace in_ctech_management_backend.Domain.Projects
{
    public sealed record ProjectId(Guid Value)
    {
        public static explicit operator Guid(ProjectId projectId) => projectId.Value;
    }
}
