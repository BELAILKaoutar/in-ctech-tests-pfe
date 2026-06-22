using in_ctech_management_backend.Application.Company.DTOs;
using in_ctech_management_backend.Application.Purchase.DTOs;
using in_ctech_management_backend.Application.Shared.DTOs;
using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.Companies.Repositories;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Purchases;
using in_ctech_management_backend.Domain.Purchases.Repositories;

namespace in_ctech_management_backend.Application.Purchase
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly ICompanyRepository _companyRepo;

        public PurchaseService(IPurchaseRepository repo, ICompanyRepository companyRepo)
        {
            _purchaseRepo = repo;
            _companyRepo = companyRepo;
        }

        public async Task<List<PurchaseResponseDto>> GetAllAsync(string? reference, Guid? resource, Guid? client, string? type, CancellationToken ct = default)
        {
            var purchases = await _purchaseRepo.GetAllAsync(reference, resource, client, type, ct);
            return purchases.Select(MapToResponse).ToList();
        }

        public async Task<PurchaseResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var purchase = await _purchaseRepo.GetByIdWithIncludesAsync(new PurchaseId(id), ct);
            return purchase is null ? null : MapToResponse(purchase);
        }

        public async Task<PurchaseResponseDto> CreateAsync(PurchaseRequestDto dto, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(dto.Reference, nameof(dto.Reference));
            ArgumentNullException.ThrowIfNull(dto.PresMonth, nameof(dto.PresMonth));
            ArgumentNullException.ThrowIfNull(dto.Ttc, nameof(dto.Ttc));
            ArgumentNullException.ThrowIfNull(dto.Tva, nameof(dto.Tva));
            ArgumentNullException.ThrowIfNull(dto.Ht, nameof(dto.Ht));
            ArgumentNullException.ThrowIfNull(dto.InvoiceDate, nameof(dto.InvoiceDate));
            ArgumentException.ThrowIfNullOrEmpty(dto.Type, nameof(dto.Type));

            EmployeeId? EmployeeId = string.IsNullOrEmpty(dto.Resource) ? null : new EmployeeId(Guid.Parse(dto.Resource));

            CompanyId? clientId = null;
            if (!string.IsNullOrEmpty(dto.Client))
            {
                var cliGuid = Guid.Parse(dto.Client);
                _ = await _companyRepo.GetByIdAsync(new CompanyId(cliGuid), ct)
                    ?? throw new KeyNotFoundException("Client introuvable.");
                clientId = new CompanyId(cliGuid);
            }

            var purchase = Domain.Purchases.Purchase.Create(
                dto.Reference,
                EmployeeId,
                clientId,
                DateTime.SpecifyKind(dto.PresMonth.Value, DateTimeKind.Utc),
                dto.Ttc.Value,
                dto.Tva.Value,
                dto.Ht.Value,
                DateTime.SpecifyKind(dto.InvoiceDate.Value, DateTimeKind.Utc),
                dto.Type,
                dto.Status,
                dto.PaiementMode
            );

            await _purchaseRepo.AddAsync(purchase, ct);
            await _purchaseRepo.SaveChangesAsync(ct);

            var saved = await _purchaseRepo.GetByIdWithIncludesAsync(purchase.Id, ct);
            return MapToResponse(saved!);
        }

        public async Task<PurchaseResponseDto> UpdateAsync(Guid id, PurchaseRequestDto dto, CancellationToken ct = default)
        {
            var purchase = await _purchaseRepo.GetByIdAsync(new PurchaseId(id), ct)
                ?? throw new KeyNotFoundException($"Purchase with id '{id}' not found.");

            EmployeeId? EmployeeId = string.IsNullOrEmpty(dto.Resource) ? null : new EmployeeId(Guid.Parse(dto.Resource));

            CompanyId? clientId = null;
            if (!string.IsNullOrEmpty(dto.Client))
            {
                var cliGuid = Guid.Parse(dto.Client);
                _ = await _companyRepo.GetByIdAsync(new CompanyId(cliGuid), ct)
                    ?? throw new KeyNotFoundException("Client introuvable.");
                clientId = new CompanyId(cliGuid);
            }

            purchase.Update(
                dto.Reference,
                EmployeeId,
                clientId,
                dto.PresMonth.HasValue ? DateTime.SpecifyKind(dto.PresMonth.Value, DateTimeKind.Utc) : null,
                dto.Ttc,
                dto.Tva,
                dto.Ht,
                dto.InvoiceDate.HasValue ? DateTime.SpecifyKind(dto.InvoiceDate.Value, DateTimeKind.Utc) : null,
                dto.Type,
                dto.Status,
                dto.PaiementMode
            );

            await _purchaseRepo.SaveChangesAsync(ct);

            var updated = await _purchaseRepo.GetByIdWithIncludesAsync(purchase.Id, ct);
            return MapToResponse(updated!);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var purchase = await _purchaseRepo.GetByIdAsync(new PurchaseId(id), ct)
                ?? throw new KeyNotFoundException($"Purchase with id '{id}' not found.");

            await _purchaseRepo.DeleteAsync(purchase, ct);
            await _purchaseRepo.SaveChangesAsync(ct);
        }

        public async Task<PurchaseResponseDto> ConfirmPaymentAsync(Guid id, PurchaseConfirmPaymentDto dto, CancellationToken ct = default)
        {
            var purchase = await _purchaseRepo.GetByIdAsync(new PurchaseId(id), ct)
                ?? throw new KeyNotFoundException($"Purchase with id '{id}' not found.");

            purchase.ConfirmPayment(DateTime.SpecifyKind(dto.PaymentDate, DateTimeKind.Utc));
            await _purchaseRepo.SaveChangesAsync(ct);

            var updated = await _purchaseRepo.GetByIdWithIncludesAsync(purchase.Id, ct);
            return MapToResponse(updated!);
        }

        private static PurchaseResponseDto MapToResponse(Domain.Purchases.Purchase p)
        {
            return new PurchaseResponseDto
            {
                Id = ((Guid)p.Id).ToString(),
                Reference = p.Reference,
                Resource = p.Resource is not null ? MapEmployeeToResponse(p.Resource) : null,
                Client = p.Client is not null ? MapCompanyToResponse(p.Client) : null,
                PresMonth = p.PresMonth,
                Ttc = p.Ttc,
                Tva = p.Tva,
                Ht = p.Ht,
                InvoiceDate = p.InvoiceDate,
                Type = p.Type,
                Status = p.Status,
                PaymentDate = p.PaymentDate,
                PaiementMode = p.PaiementMode,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                CreatedBy = p.CreatedBy,
                UpdatedBy = p.UpdatedBy,
            };
        }

        private static EmployeeResponseDto MapEmployeeToResponse(Domain.Employees.Employee e)
        {
            return new EmployeeResponseDto
            {
                Id = ((Guid)e.Id).ToString(),
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FullName,
                Trigram = e.Trigram,
                DailyRate = e.DailyRate ?? 0,
                ContractType = e.ContractType.ToString(),
                FreelancerType = e.FreelancerType?.ToString(),
                StartDate = e.StartDate ?? DateTime.MinValue,
                ContractEndDate = e.ContractEndDate,
            };
        }

        private static CompanyResponseDto MapCompanyToResponse(Domain.Companies.Company c)
        {
            return new CompanyResponseDto
            {
                Id = ((Guid)c.CompanyId).ToString(),
                Nom = c.Nom,
                Adresse = c.Adresse,
                Contact = c.Contact,
                Code = c.Code,
                Pays = c.Pays,
                SocietyType = c.SocietyType,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                CreatedBy = c.CreatedBy,
                UpdatedBy = c.UpdatedBy,
            };
        }
    }
}
