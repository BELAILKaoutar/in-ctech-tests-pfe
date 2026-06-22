namespace in_ctech_management_backend.Domain.Holidays
{
    public sealed record HolidayId(Guid Value)
    {
        public static explicit operator Guid(HolidayId holidayId) => holidayId.Value;
    }
}
