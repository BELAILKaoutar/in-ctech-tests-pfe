namespace in_ctech_management_backend.Domain.Jobs
{
    public sealed record JobId(Guid Value)
    {
        public static explicit operator Guid(JobId projectId) => projectId.Value;
    }
}
