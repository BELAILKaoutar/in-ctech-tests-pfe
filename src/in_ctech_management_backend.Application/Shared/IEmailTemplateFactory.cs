using in_ctech_management_backend.Domain.Enums;

namespace in_ctech_management_backend.Application.Shared
{
    public interface IEmailTemplateFactory
    {
        Task<string> GetTemplateAsync(EmailTemplateType templateType);
    }
}
