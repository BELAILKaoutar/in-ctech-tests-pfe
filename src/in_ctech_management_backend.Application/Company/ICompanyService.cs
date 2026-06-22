using in_ctech_management_backend.Application.Company.DTOs;

namespace in_ctech_management_backend.Application.Company
{
    public interface ICompanyService
    {
        Task<List<CompanyResponseDto>> GetAllAsync(string? nom, string? pays, CancellationToken cancellationToken = default);
        Task<CompanyResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CompanyResponseDto> CreateAsync(CreateCompanyDto dto, CancellationToken cancellationToken = default);
        Task<CompanyResponseDto> UpdateAsync(Guid id, UpdateCompanyDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
