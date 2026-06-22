namespace in_ctech_management_backend.Domain.TimeSheets.Submissions
{
    public sealed record SubmissionId(Guid Value)
    {
        public static explicit operator Guid(SubmissionId submissionId) => submissionId.Value;
    }
}
