using in_ctech_management_backend.Application.Company.DTOs;
using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.Companies.Repositories;

namespace in_ctech_management_backend.Application.Company
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _repository;

        public CompanyService(ICompanyRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CompanyResponseDto>> GetAllAsync(string? nom, string? pays, CancellationToken cancellationToken = default)
        {
            var companies = await _repository.GetAllAsync(nom, pays, cancellationToken);
            return companies.Select(MapToResponse).ToList();
        }

        public async Task<CompanyResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var company = await _repository.GetByIdAsync(new CompanyId(id), cancellationToken);
            return company == null ? null : MapToResponse(company);
        }
        public async Task<CompanyResponseDto> CreateAsync(CreateCompanyDto dto, CancellationToken cancellationToken = default)
        {
            var existing = await _repository.GetByCodeAsync(dto.Code, cancellationToken);
            if (existing != null)
                throw new Exceptions.ApplicationException("Une société avec ce code existe déjà.");

            var company = Domain.Companies.Company.Create(
                dto.Nom,
                dto.Adresse,
                dto.Contact,
                dto.Code,
                dto.Pays,
                dto.SocietyType,
                dto.CreatedBy ?? "System"
            );

            await _repository.AddAsync(company, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return MapToResponse(company);
        }
        public async Task<CompanyResponseDto> UpdateAsync(Guid id, UpdateCompanyDto dto, CancellationToken cancellationToken = default)
        {
            var company = await _repository.GetByIdAsync(new CompanyId(id), cancellationToken)
                ?? throw new KeyNotFoundException($"Société avec l'id '{id}' introuvable.");

            if (dto.Code is not null && dto.Code != company.Code)
            {
                var existing = await _repository.GetByCodeAsync(dto.Code, cancellationToken);
                if (existing != null)
                    throw new Exceptions.ApplicationException("Une société avec ce code existe déjà.");
            }

            company.Update(dto.Nom, dto.Adresse, dto.Contact, dto.Code, dto.Pays, dto.SocietyType, dto.UpdatedBy ?? "System");

            await _repository.UpdateAsync(company, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return MapToResponse(company);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var company = await _repository.GetByIdAsync(new CompanyId(id), cancellationToken)
                ?? throw new KeyNotFoundException($"Société avec l'id '{id}' introuvable.");

            await _repository.DeleteAsync(company.CompanyId, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        private static CompanyResponseDto MapToResponse(Domain.Companies.Company s)
        {
            return new CompanyResponseDto
            {
                Id = s.CompanyId.Value.ToString(),
                Nom = s.Nom,
                Adresse = s.Adresse,
                Contact = s.Contact,
                Code = s.Code,
                Pays = s.Pays,
                SocietyType = s.SocietyType,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                CreatedBy = s.CreatedBy,
                UpdatedBy = s.UpdatedBy,
            };
        }
    }
}
