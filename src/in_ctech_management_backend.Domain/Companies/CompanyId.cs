namespace in_ctech_management_backend.Domain.Companies
{
    public sealed record CompanyId(Guid Value)
    {
        public static explicit operator Guid(CompanyId companyId) => companyId.Value;
    }
}
