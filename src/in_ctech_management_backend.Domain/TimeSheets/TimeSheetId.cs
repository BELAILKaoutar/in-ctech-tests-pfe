
namespace in_ctech_management_backend.Domain.TimeSheets
{
    public sealed record TimeSheetId(Guid Value)
    {
        public static explicit operator Guid(TimeSheetId timeSheetId) => timeSheetId.Value;
    }
}
