namespace in_ctech_management_backend.Domain.Employees
{
    public sealed record EmployeeId(Guid Value)
    {
        public static explicit operator Guid(EmployeeId employeeId) => employeeId.Value;
    }
}
