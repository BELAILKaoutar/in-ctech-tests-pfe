using in_ctech_management_backend.Application.Company.DTOs;
using in_ctech_management_backend.Application.DeliveryNote.DTOs;
using in_ctech_management_backend.Application.Employee;
using in_ctech_management_backend.Application.PuchaseOrder.DTOs;
using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.Companies.Repositories;
using in_ctech_management_backend.Domain.DeliveryNotes;
using in_ctech_management_backend.Domain.DeliveryNotes.Enums;
using in_ctech_management_backend.Domain.DeliveryNotes.Repository;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.Projects;
using in_ctech_management_backend.Domain.PurchaseOrders;
using in_ctech_management_backend.Domain.PurchaseOrders.Repositories;


namespace in_ctech_management_backend.Application.DeliveryNote
{
    public class DeliveryNoteService : IDeliveryNoteService
    {
        private readonly IDeliveryNoteRepository _repository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IEmployeeService _employeeService;

        public DeliveryNoteService(
            IDeliveryNoteRepository repository,
            ICompanyRepository companyRepository,
            IEmployeeRepository employeeRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IProjectRepository projectRepository,
            IEmployeeService employeeService)
        {
            _repository = repository;
            _companyRepository = companyRepository;
            _employeeRepository = employeeRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _projectRepository = projectRepository;
            _employeeService = employeeService;
        }

        // ==================== CREATE ====================

        public async Task<List<Guid>> CreateAsync(CreateDeliveryNoteRequest dto, CancellationToken cancellationToken = default)
        {
            if (!Enum.TryParse<DeliveryNoteType>(dto.Type, true, out var type))
                throw new Exceptions.ApplicationException($"Invalid Type: {dto.Type}");

            var company = await _companyRepository.GetByIdAsync(new CompanyId(dto.CompanyId), cancellationToken);
            if (company == null)
                throw new Exceptions.ApplicationException($"Company with id '{dto.CompanyId}' not found");

            if (!company.SocietyType.Equals("CLIENT", StringComparison.OrdinalIgnoreCase))
                throw new Exceptions.ApplicationException("Vous devez sélectionner un client pour un bon de livraison.");

            var createdIds = new List<Guid>();

            if (type == DeliveryNoteType.AT)
            {
                if (dto.AtItems == null || dto.AtItems.Count == 0)
                    throw new Exceptions.ApplicationException("At least one AT item is required.");

                if (!dto.Month.HasValue || !dto.Year.HasValue)
                    throw new Exceptions.ApplicationException("Month and Year are required for AT delivery notes.");
                foreach (var item in dto.AtItems)
                {
                    var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(item.EmployeeId), cancellationToken);
                    if (employee == null)
                        throw new Exceptions.ApplicationException($"Employee with id '{item.EmployeeId}' not found");

                    var reference = await GenerateReferenceAsync(cancellationToken);
                    var amount = item.Quantity * (employee.DailyRate ?? 0);

                    var deliveryNote = Domain.DeliveryNotes.DeliveryNote.CreateAT(
                        reference,
                        new CompanyId(dto.CompanyId),
                        new EmployeeId(item.EmployeeId),
                        dto.Month.Value,
                        dto.Year.Value,
                        item.Quantity,
                        amount,
                        dto.InvoiceDate,
                        dto.CreatedBy
                    );

                    await _repository.AddAsync(deliveryNote, cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);
                    createdIds.Add(deliveryNote.DeliveryNoteId.Value);
                }
            }
            else
            {
                if (dto.WpItems == null || dto.WpItems.Count == 0)
                    throw new Exceptions.ApplicationException("At least one WP item is required.");

                foreach (var item in dto.WpItems)
                {
                    if (string.IsNullOrWhiteSpace(item.Designation))
                        throw new Exceptions.ApplicationException("Designation is required for WP delivery notes.");

                    var reference = await GenerateReferenceAsync(cancellationToken);

                    var deliveryNote = Domain.DeliveryNotes.DeliveryNote.CreateWP(
                        reference,
                        new CompanyId(dto.CompanyId),
                        item.Designation,
                        item.Quantity,
                        item.UnitPrice,
                        item.PurchaseOrderId.HasValue ? new PurchaseOrderId(item.PurchaseOrderId.Value) : null,
                        dto.InvoiceDate,
                        dto.CreatedBy
                    );

                    await _repository.AddAsync(deliveryNote, cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);
                    createdIds.Add(deliveryNote.DeliveryNoteId.Value);
                }
            }

            return createdIds;
        }

        // ==================== READ ====================

        public async Task<DeliveryNoteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var dn = await _repository.GetByIdAsync(new DeliveryNoteId(id), cancellationToken);
            if (dn == null) return null;

            return await MapToDtoAsync(dn, cancellationToken);
        }

        public async Task<DeliveryNoteDocumentDataDto?> GetDocumentDataAsync(Guid id, string? template = null, bool signed = false, CancellationToken cancellationToken = default)
        {
            var dn = await _repository.GetByIdAsync(new DeliveryNoteId(id), cancellationToken);
            if (dn == null)
                return null;

            var company = await _companyRepository.GetByIdAsync(dn.CompanyId, cancellationToken);

            Domain.Employees.Employee? employee = null;
            if (dn.EmployeeId != null)
                employee = await _employeeRepository.GetByIdAsync(dn.EmployeeId, cancellationToken);

            string? purchaseOrderReference = null;
            if (dn.PurchaseOrderId != null)
            {
                var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dn.PurchaseOrderId, cancellationToken);
                if (purchaseOrder != null)
                    purchaseOrderReference = purchaseOrder.Reference;
            }

            var isWinity = string.Equals(template, "winity", StringComparison.OrdinalIgnoreCase);
            var quantity = dn.Quantity;
            var unitPrice = dn.Type == DeliveryNoteType.AT
                ? employee?.DailyRate ?? 0
                : dn.UnitPrice ?? 0;
            var amount = dn.Amount;
            var tva = isWinity ? 0 : Math.Round(amount * 0.20, 2);
            var totalTtc = Math.Round(amount + tva, 2);
            var periodLabel = dn.Month.HasValue && dn.Year.HasValue
                ? $"{dn.Month.Value:D2}/{dn.Year.Value}"
                : dn.InvoiceDate.ToString("MM/yyyy");

            var baseDescription = dn.Type == DeliveryNoteType.WP
                ? dn.Designation ?? string.Empty
                : "Prestation de développement informatique";
            var itemDescription = isWinity
                ? $"{baseDescription}\n\nMois: {periodLabel}"
                : baseDescription;

            var items = new List<DeliveryNoteDocumentItemDto>
            {
                new(
                    itemDescription,
                    quantity,
                    unitPrice,
                    amount)
            };

            DeliveryNoteDocumentClientDto? client = null;
            if (company != null)
            {
                client = new DeliveryNoteDocumentClientDto(
                    company.CompanyId.Value.ToString(),
                    company.Nom,
                    company.Adresse,
                    company.Code,
                    company.Pays);
            }

            return new DeliveryNoteDocumentDataDto(
                dn.DeliveryNoteId.Value,
                dn.Reference,
                BuildFileName(dn.Reference, isWinity),
                isWinity ? "winity" : "default",
                dn.Type.ToString(),
                dn.InvoiceDate,
                purchaseOrderReference,
                client,
                employee?.FullName,
                dn.Month,
                dn.Year,
                periodLabel,
                dn.Designation,
                items,
                amount,
                tva,
                totalTtc,
                isWinity ? "EUR" : "MAD",
                signed);
        }

        public async Task<List<DeliveryNoteDto>> GetAllAsync(
            string? reference = null,
            Guid? companyId = null,
            Guid? employeeId = null,
            Guid? projectId = null,
            string? type = null,
            CancellationToken cancellationToken = default)
        {
            List<Domain.DeliveryNotes.DeliveryNote> deliveryNotes;

            if (!string.IsNullOrWhiteSpace(reference))
            {
                deliveryNotes = await _repository.SearchByReferenceAsync(reference, cancellationToken);
            }
            else if (companyId.HasValue)
            {
                deliveryNotes = await _repository.GetByCompanyAsync(new CompanyId(companyId.Value), cancellationToken);
            }
            else if (employeeId.HasValue)
            {
                deliveryNotes = await _repository.GetByEmployeeAsync(new EmployeeId(employeeId.Value), cancellationToken);
            }
            else if (projectId.HasValue)
            {
                deliveryNotes = await _repository.GetByProjectAsync(new ProjectId(projectId.Value), cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(type))
            {
                if (!Enum.TryParse<DeliveryNoteType>(type, true, out var deliveryNoteType))
                    throw new Exceptions.ApplicationException($"Invalid type: {type}");
                deliveryNotes = await _repository.GetByTypeAsync(deliveryNoteType, cancellationToken);
            }
            else
            {
                deliveryNotes = await _repository.GetAllAsync(cancellationToken);
            }

            var result = new List<DeliveryNoteDto>();
            foreach (var dn in deliveryNotes)
            {
                result.Add(await MapToDtoAsync(dn, cancellationToken));
            }
            return result;
        }

        // ==================== UPDATE ====================

        public async Task UpdateAsync(Guid id, UpdateDeliveryNoteRequest dto, CancellationToken cancellationToken = default)
        {
            var dn = await _repository.GetByIdAsync(new DeliveryNoteId(id), cancellationToken);
            if (dn == null)
                throw new Exceptions.ApplicationException($"Delivery Note with id '{id}' not found");

            var company = await _companyRepository.GetByIdAsync(new CompanyId(dto.CompanyId), cancellationToken);
            if (company == null)
                throw new Exceptions.ApplicationException($"Company with id '{dto.CompanyId}' not found");

            if (!company.SocietyType.Equals("CLIENT", StringComparison.OrdinalIgnoreCase))
                throw new Exceptions.ApplicationException("Vous devez sélectionner un client.");

            if (dn.Type == DeliveryNoteType.AT)
            {
                if (!dto.EmployeeId.HasValue || !dto.Month.HasValue || !dto.Year.HasValue)
                    throw new Exceptions.ApplicationException("EmployeeId, Month and Year are required for AT delivery notes.");

                var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(dto.EmployeeId.Value), cancellationToken);
                if (employee == null)
                    throw new Exceptions.ApplicationException($"Employee with id '{dto.EmployeeId}' not found");

                var amount = dto.Quantity * (employee.DailyRate ?? 0);

                dn.UpdateAT(
                    new CompanyId(dto.CompanyId),
                    new EmployeeId(dto.EmployeeId.Value),
                    dto.Month.Value,
                    dto.Year.Value,
                    dto.Quantity,
                    amount,
                    dto.InvoiceDate,
                    dto.UpdatedBy
                );
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Designation) || !dto.UnitPrice.HasValue)
                    throw new Exceptions.ApplicationException("Designation and UnitPrice are required for WP delivery notes.");

                dn.UpdateWP(
                    new CompanyId(dto.CompanyId),
                    dto.Designation,
                    dto.Quantity,
                    dto.UnitPrice.Value,
                    dto.InvoiceDate,
                    dto.UpdatedBy
                );
            }

            await _repository.SaveChangesAsync(cancellationToken);
        }

        // ==================== DELETE ====================

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var dn = await _repository.GetByIdAsync(new DeliveryNoteId(id), cancellationToken);
            if (dn == null)
                throw new Exceptions.ApplicationException($"Delivery Note with id '{id}' not found");

            await _repository.DeleteAsync(dn.DeliveryNoteId, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        // ==================== PRIVATE HELPERS ====================

        private async Task<string> GenerateReferenceAsync(CancellationToken cancellationToken)
        {
            var currentYear = DateTime.UtcNow.Year.ToString();
            var latestReference = await _repository.GetLatestReferenceAsync(cancellationToken);

            if (latestReference == null)
            {
                return $"BL-IN-C-{currentYear}-0001";
            }

            var parts = latestReference.Split('-');
            var lastNum = int.Parse(parts[4]);
            var nextNum = (lastNum + 1).ToString("D4");
            return $"BL-IN-C-{currentYear}-{nextNum}";
        }

        private async Task<DeliveryNoteDto> MapToDtoAsync(
            Domain.DeliveryNotes.DeliveryNote dn,
            CancellationToken cancellationToken)
        {
            // Mapper Company
            CompanyResponseDto? companyDto = null;
            var company = await _companyRepository.GetByIdAsync(dn.CompanyId, cancellationToken);
            if (company != null)
            {
                companyDto = new CompanyResponseDto
                {
                    Id = company.CompanyId.Value.ToString(),
                    Nom = company.Nom,
                    Adresse = company.Adresse,
                    Contact = company.Contact,
                    Code = company.Code,
                    Pays = company.Pays,
                    SocietyType = company.SocietyType,
                    CreatedAt = company.CreatedAt,
                    UpdatedAt = company.UpdatedAt,
                };
            }

            // Mapper Employee
            DTOs.EmployeeSummaryDto? employeeDto = null;
            if (dn.EmployeeId != null)
            {
                var employee = await _employeeRepository.GetByIdAsync(dn.EmployeeId, cancellationToken);
                if (employee != null)
                {
                    var projectNames = employee.Projects
                        .Select(p => p.Name)
                        .ToList();

                    employeeDto = new DTOs.EmployeeSummaryDto(
                        employee.Id.Value,
                        employee.FullName,
                        employee.Trigram,
                        employee.Email,
                        employee.DailyRate,
                        projectNames
                    );
                }
            }

            // Mapper PurchaseOrder Reference
            string? purchaseOrderReference = null;
            if (dn.PurchaseOrderId != null)
            {
                var po = await _purchaseOrderRepository.GetByIdAsync(dn.PurchaseOrderId, cancellationToken);
                if (po != null)
                {
                    purchaseOrderReference = po.Reference;
                }
            }

            return new DeliveryNoteDto(
                dn.DeliveryNoteId.Value,
                dn.Reference,
                dn.Type.ToString(),
                dn.Quantity,
                dn.Amount,
                dn.InvoiceDate,
                dn.CreatedAt,
                dn.UpdatedAt,
                dn.CreatedBy,
                dn.UpdatedBy,
                dn.Month,
                dn.Year,
                dn.Designation,
                dn.UnitPrice,
                purchaseOrderReference,
                companyDto,
                employeeDto
            );
        }

        private static string BuildFileName(string reference, bool isWinity)
        {
            if (!isWinity)
                return reference;

            var parts = reference.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"DN-{parts[^2]}-{parts[^1]}";
            }

            return reference;
        }
    }
}
