
namespace in_ctech_management_backend.Domain.TimeSheets
{
    public sealed record TimeEntryId(Guid Value)
    {
        public static explicit operator Guid(TimeEntryId timeEntryId) => timeEntryId.Value;
    }
}
