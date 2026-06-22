using in_ctech_management_backend.Application.Company.DTOs;
using in_ctech_management_backend.Application.Employee;
using in_ctech_management_backend.Application.PuchaseOrder.DTOs;
using in_ctech_management_backend.Application.PurchaseOrder.DTOs;
using in_ctech_management_backend.Application.Shared;
using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.Companies.Repositories;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Enums;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.Invoices;
using in_ctech_management_backend.Domain.Invoices.Enums;
using in_ctech_management_backend.Domain.Invoices.Repositories;
using in_ctech_management_backend.Domain.Projects;
using in_ctech_management_backend.Domain.PurchaseOrders;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;
using in_ctech_management_backend.Domain.PurchaseOrders.Repositories;


namespace in_ctech_management_backend.Application.PurchaseOrder
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _repository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmployeeService _employeeService;
        private readonly IInvoiceRepository _invoiceRepository;

        public PurchaseOrderService(
            IPurchaseOrderRepository repository,
            IEmployeeRepository employeeRepository,
            IEmployeeService employeeService,
            ICompanyRepository companyRepository,
            IInvoiceRepository invoiceRepository)
        {
            _repository = repository;
            _companyRepository = companyRepository;
            _employeeRepository = employeeRepository;
            _employeeService = employeeService;
            _invoiceRepository = invoiceRepository;
        }


        // ==================== CREATE ====================

        public async Task<Guid> CreateAsync(CreatePurchaseOrderRequest dto, CancellationToken cancellationToken = default)
        {
            if (!Enum.TryParse<EngagementMode>(dto.EngagementMode, true, out var engagementMode))
                throw new Exceptions.ApplicationException($"Invalid EngagementMode: {dto.EngagementMode}");

            var (paymentMode, docType, reference) = await ValidateAndParseCommonFieldsAsync(
                dto.Reference, dto.PaymentMode, dto.DocType, dto.CompanyId, cancellationToken);

            Domain.PurchaseOrders.PurchaseOrder purchaseOrder;

            if (engagementMode == EngagementMode.AT)
            {
                if (!dto.DailyRate.HasValue || (!dto.Quantity.HasValue && !dto.TotalAmount.HasValue))
                    throw new Exceptions.ApplicationException("DailyRate and either Quantity or TotalAmount are required for AT purchase orders.");

                if (dto.EmployeeId.HasValue)
                {
                    var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(dto.EmployeeId.Value), cancellationToken);
                    if (employee == null)
                        throw new Exceptions.ApplicationException($"Employee with id '{dto.EmployeeId}' not found");
                   await _employeeService.UpdateDailyRateAsync(employee.Id.Value, dto.DailyRate, cancellationToken);
                }

                purchaseOrder = Domain.PurchaseOrders.PurchaseOrder.CreateAT(
                    reference,
                    docType,
                    new CompanyId(dto.CompanyId),
                    dto.StartDate,
                    dto.EndDate,
                    paymentMode,
                    dto.Description,
                    dto.EmployeeId.HasValue ? new EmployeeId(dto.EmployeeId.Value) : null,
                    dto.DailyRate.Value,
                    dto.Quantity,
                    dto.TotalAmount,
                    dto.CreatedBy
                );
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Designation) || !dto.UnitPrice.HasValue || (!dto.Quantity.HasValue && !dto.TotalAmount.HasValue))
                    throw new Exceptions.ApplicationException("Designation, UnitPrice and either Quantity or TotalAmount are required for WP purchase orders.");

                purchaseOrder = Domain.PurchaseOrders.PurchaseOrder.CreateWP(
                    reference,
                    docType,
                    new CompanyId(dto.CompanyId),
                    dto.StartDate,
                    dto.EndDate,
                    paymentMode,
                    dto.Description,
                    dto.Designation,
                    dto.Quantity,
                    dto.UnitPrice.Value,
                    dto.TotalAmount,
                    dto.CreatedBy
                );
            }

            await _repository.AddAsync(purchaseOrder, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return purchaseOrder.PurchaseOrderId.Value;
        }


        // ==================== READ ====================

        public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var po = await _repository.GetByIdAsync(new PurchaseOrderId(id), cancellationToken);
            if (po == null) return null;

            return await MapToDtoAsync(po, cancellationToken);
        }

        public async Task<PurchaseOrderDocumentDataDto?> GetDocumentDataAsync(Guid id, string? template = null, CancellationToken cancellationToken = default)
        {
            var po = await _repository.GetByIdAsync(new PurchaseOrderId(id), cancellationToken);
            if (po == null)
                return null;

            var normalizedTemplate = string.Equals(template, "winity", StringComparison.OrdinalIgnoreCase)
                ? "winity"
                : "in-ctech";

            var company = await _companyRepository.GetByIdAsync(po.CompanyId, cancellationToken);

            Domain.Employees.Employee? employee = null;
            if (po.EmployeeId != null)
            {
                employee = await _employeeRepository.GetByIdAsync(po.EmployeeId, cancellationToken);
            }

            return PurchaseOrderMapper.ToDocumentDataDto(po, normalizedTemplate, company, employee);
        }

        public async Task<List<PurchaseOrderDto>> GetAllAsync(
            string? status = null,
            Guid? companyId = null,
            Guid? employeeId = null,
            string? reference = null,
            CancellationToken cancellationToken = default)
        {
            List<Domain.PurchaseOrders.PurchaseOrder> purchaseOrders;

            if (!string.IsNullOrWhiteSpace(reference))
            {
                purchaseOrders = await _repository.SearchByReferenceAsync(reference, cancellationToken);
            }
            else if (companyId.HasValue)
            {
                var company = await _companyRepository.GetByIdAsync(new CompanyId(companyId.Value), cancellationToken);
                if (company == null)
                    throw new Exceptions.ApplicationException($"Client with id '{companyId}' not found");
                purchaseOrders = await _repository.GetByCompanyAsync(new CompanyId(companyId.Value), cancellationToken);
            }
            else if (employeeId.HasValue)
            {
                purchaseOrders = await _repository.GetByEmployeeAsync(new EmployeeId(employeeId.Value), cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<PurchaseOrderStatus>(status, true, out var purchaseOrderStatus))
                    throw new Exceptions.ApplicationException($"Invalid status: {status}");
                purchaseOrders = await _repository.GetByStatusAsync(purchaseOrderStatus, cancellationToken);
            }
            else
            {
                purchaseOrders = await _repository.GetAllAsync(cancellationToken);
            }

            var result = new List<PurchaseOrderDto>();
            foreach (var po in purchaseOrders)
            {
                result.Add(await MapToDtoAsync(po, cancellationToken));
            }
            return result;
        }

        // ==================== UPDATE ====================

        public async Task UpdateAsync(Guid id, UpdatePurchaseOrderRequest dto, CancellationToken cancellationToken = default)
        {
            var po = await _repository.GetByIdAsync(new PurchaseOrderId(id), cancellationToken);
            if (po == null)
                throw new Exceptions.ApplicationException($"Purchase Order with id '{id}' not found");

            if (!Enum.TryParse<PaymentMethod>(dto.PaymentMode, true, out var paymentMode))
                throw new Exceptions.ApplicationException($"Invalid PaymentMode: {dto.PaymentMode}");

            if (!Enum.TryParse<EngagementMode>(dto.EngagementMode, true, out var engagementMode))
                throw new Exceptions.ApplicationException($"Invalid EngagementMode: {dto.EngagementMode}");

            var company = await _companyRepository.GetByIdAsync(new CompanyId(dto.CompanyId), cancellationToken);
            if (company == null)
                throw new Exceptions.ApplicationException($"Company with id '{dto.CompanyId}' not found");

            if (!company.SocietyType.Equals("CLIENT", StringComparison.OrdinalIgnoreCase))
                throw new Exceptions.ApplicationException("Vous devez sélectionner un client.");

            if (po.EngagementMode == EngagementMode.AT)
            {
                if (!dto.DailyRate.HasValue || (!dto.Quantity.HasValue && !dto.TotalAmount.HasValue))
                    throw new Exceptions.ApplicationException("DailyRate and either Quantity or TotalAmount are required for AT purchase orders.");

                if (dto.EmployeeId.HasValue)
                {
                    var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(dto.EmployeeId.Value), cancellationToken);
                    if (employee == null)
                        throw new Exceptions.ApplicationException($"Employee with id '{dto.EmployeeId}' not found");
                    await _employeeService.UpdateDailyRateAsync(employee.Id.Value, dto.DailyRate, cancellationToken);
                }

                po.UpdateAT(
                    dto.EmployeeId.HasValue ? new EmployeeId(dto.EmployeeId.Value) : null,
                    new CompanyId(dto.CompanyId),
                    dto.StartDate,
                    dto.EndDate,
                    engagementMode,
                    paymentMode,
                    dto.DailyRate.Value,
                    dto.Quantity,
                    dto.TotalAmount,
                    dto.UpdatedBy
                );
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Designation) || !dto.UnitPrice.HasValue || (!dto.Quantity.HasValue && !dto.TotalAmount.HasValue))
                    throw new Exceptions.ApplicationException("Designation, UnitPrice and either Quantity or TotalAmount are required for WP purchase orders.");

                if (!string.IsNullOrWhiteSpace(dto.Reference) && dto.Reference != po.Reference)
                {
                    var existing = await _repository.GetByReferenceAsync(dto.Reference, cancellationToken);
                    if (existing != null)
                        throw new Exceptions.ApplicationException($"Purchase Order with reference '{dto.Reference}' already exists");
                }

                po.UpdateWP(
                    dto.Reference ?? po.Reference,
                    dto.Designation,
                    new CompanyId(dto.CompanyId),
                    dto.StartDate,
                    dto.EndDate,
                    engagementMode,
                    paymentMode,
                    dto.Quantity,
                    dto.UnitPrice.Value,
                    dto.TotalAmount,
                    dto.UpdatedBy
                );
            }

            await _repository.SaveChangesAsync(cancellationToken);
        }



        // ==================== STATUS ====================

        public async Task ChangeStatusAsync(Guid id, string newStatus, string updatedBy, CancellationToken cancellationToken = default)
        {
            var po = await _repository.GetByIdAsync(new PurchaseOrderId(id), cancellationToken);
            if (po == null)
                throw new Exceptions.ApplicationException($"Purchase Order with id '{id}' not found");

            if (!Enum.TryParse<PurchaseOrderStatus>(newStatus, true, out var status))
                throw new Exceptions.ApplicationException($"Invalid status: {newStatus}");

            po.ChangeStatus(status, updatedBy);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        // ==================== DELETE ====================

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var po = await _repository.GetByIdAsync(new PurchaseOrderId(id), cancellationToken);
            if (po == null)
                throw new Exceptions.ApplicationException($"Purchase Order with id '{id}' not found");

            await _repository.DeleteAsync(po.PurchaseOrderId, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        // ==================== PRIVATE HELPERS ====================

        private async Task<(PaymentMethod paymentMode, DocType docType, string reference)> ValidateAndParseCommonFieldsAsync(
            string? reference,
            string paymentModeStr,
            string docTypeStr,
            Guid companyId,
            CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<PaymentMethod>(paymentModeStr, true, out var paymentMode))
                throw new Exceptions.ApplicationException($"Invalid PaymentMode: {paymentModeStr}");

            if (!Enum.TryParse<DocType>(docTypeStr, true, out var docType))
                throw new Exceptions.ApplicationException($"Invalid DocType: {docTypeStr}");

            // Référence : auto pour achat, manuelle pour vente
            string finalReference;
            if (docType == DocType.PURCHASE)
            {
                finalReference = await GeneratePurchaseReferenceAsync(cancellationToken);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(reference))
                    throw new Exceptions.ApplicationException("La référence est obligatoire pour les ventes.");
                finalReference = reference;
            }

            // Vérifier unicité
            var existing = await _repository.GetByReferenceAsync(finalReference, cancellationToken);
            if (existing is not null)
                throw new Exceptions.ApplicationException($"Purchase Order with reference '{finalReference}' already exists");

            // Valider company
            var company = await _companyRepository.GetByIdAsync(new CompanyId(companyId), cancellationToken);
            if (company is null)
                throw new Exceptions.ApplicationException($"Company with id '{companyId}' not found");

            if (docType == DocType.SALE &&
                !company.SocietyType.Equals("CLIENT", StringComparison.OrdinalIgnoreCase))
                throw new Exceptions.ApplicationException("Pour un bon de commande de type Vente, vous devez sélectionner un client.");

            if (docType == DocType.PURCHASE &&
                !company.SocietyType.Equals("FOURNISSEUR", StringComparison.OrdinalIgnoreCase))
                throw new Exceptions.ApplicationException("Pour un bon de commande de type Achat, vous devez sélectionner un fournisseur.");

            return (paymentMode, docType, finalReference);
        }

        private async Task<string> GeneratePurchaseReferenceAsync(CancellationToken cancellationToken)
        {
            var currentYear = DateTime.UtcNow.Year.ToString()[2..];
            var latestReference = await _repository.GetLatestPurchaseReferenceAsync(cancellationToken);

            if (latestReference == null)
            {
                return $"BC-INC-{currentYear}-0001";
            }

            var parts = latestReference.Split('-');
            var lastNum = int.Parse(parts[3]);
            var nextNum = (lastNum + 1).ToString("D4");
            return $"BC-INC-{currentYear}-{nextNum}";
        }

        private async Task<PurchaseOrderDto> MapToDtoAsync(
            Domain.PurchaseOrders.PurchaseOrder po,
            CancellationToken cancellationToken)
        {
            // Mapper Company
            CompanyResponseDto? companyDto = null;
            var company = await _companyRepository.GetByIdAsync(po.CompanyId, cancellationToken);
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
            EmployeeSummaryDto? employeeDto = null;
            Domain.Employees.Employee? employee = null;
            if (po.EmployeeId != null)
            {
                employee = await _employeeRepository.GetByIdAsync(po.EmployeeId, cancellationToken);
                if (employee != null)
                {
                    employeeDto = new EmployeeSummaryDto(
                        employee.Id.Value,
                        employee.FullName,
                        employee.Trigram,
                        employee.Email
                    );
                }
            }

            var margin = CalculateMargin(po, company, employee);

            var consumedAmount = await CalculateConsumedAmountAsync(po, cancellationToken);
            var totalAmount = Convert.ToDecimal(po.TotalAmount);
            var remainingAmount = totalAmount - consumedAmount;
            var remainingPercentage = totalAmount > 0
                ? Math.Round((remainingAmount / totalAmount) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;
            var daysRemaining = po.EndDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;

            return PurchaseOrderMapper.ToDto(
                po,
                companyDto,
                employeeDto,
                Math.Round(consumedAmount, 2, MidpointRounding.AwayFromZero),
                Math.Round(remainingAmount, 2, MidpointRounding.AwayFromZero),
                remainingPercentage,
                daysRemaining,
                margin);
        }

        // Marge AT = tarif journalier facturé au client - prix d'achat de la ressource.
        // Le tarif est dans la devise du client (Maroc => MAD, sinon EUR), le prix d'achat
        // dans sa propre devise ; les deux sont normalisés en MAD avant calcul (CurrencyConverter).
        private static PurchaseOrderMarginDto? CalculateMargin(
            Domain.PurchaseOrders.PurchaseOrder po,
            Domain.Companies.Company? company,
            Domain.Employees.Employee? employee)
        {
            if (employee is null || !employee.PurchasePrice.HasValue || !po.DailyRate.HasValue)
                return null;

            var dailyRateCurrency = CurrencyConverter.GetBcCurrency(company);
            var purchaseCurrency = employee.PurchasePriceCurrency ?? Currency.MAD;

            var dailyRateMad = CurrencyConverter.ToMad((decimal)po.DailyRate.Value, dailyRateCurrency);
            var purchasePriceMad = CurrencyConverter.ToMad(employee.PurchasePrice.Value, purchaseCurrency);

            var marginValue = dailyRateMad - purchasePriceMad;

            return new PurchaseOrderMarginDto(
                Math.Round(dailyRateMad, 2, MidpointRounding.AwayFromZero),
                dailyRateCurrency.ToString(),
                Math.Round(purchasePriceMad, 2, MidpointRounding.AwayFromZero),
                purchaseCurrency.ToString(),
                Math.Round(marginValue, 2, MidpointRounding.AwayFromZero));
        }

        private async Task<decimal> CalculateConsumedAmountAsync(
            Domain.PurchaseOrders.PurchaseOrder purchaseOrder,
            CancellationToken cancellationToken)
        {
            var invoices = await _invoiceRepository.GetAllAsync();
            return invoices
                .Where(invoice => !invoice.IsDeleted)
                .Sum(invoice => GetConsumedAmountForPurchaseOrder(invoice, purchaseOrder));
        }

        private static decimal GetConsumedAmountForPurchaseOrder(
            Invoice invoice,
            Domain.PurchaseOrders.PurchaseOrder purchaseOrder)
        {
            if (!IsLinkedToPurchaseOrder(invoice, purchaseOrder))
                return 0m;

            if (invoice.Type == InvoiceType.InvoiceWP && invoice.WPItems != null)
            {
                return invoice.WPItems
                    .Where(item => string.Equals(item.ReferenceBC, purchaseOrder.Reference, StringComparison.OrdinalIgnoreCase))
                    .Sum(item => item.PrixUnitaire * item.Quantite);
            }

            return invoice.TotalHT;
        }

        private static bool IsLinkedToPurchaseOrder(
            Invoice invoice,
            Domain.PurchaseOrders.PurchaseOrder purchaseOrder)
        {
            if (string.Equals(invoice.PurchaseOrderReference, purchaseOrder.Reference, StringComparison.OrdinalIgnoreCase))
                return true;

            if (invoice.Type == InvoiceType.InvoiceWP)
            {
                return invoice.WPItems?.Any(item => string.Equals(item.ReferenceBC, purchaseOrder.Reference, StringComparison.OrdinalIgnoreCase)) == true;
            }

            if (purchaseOrder.EngagementMode != EngagementMode.AT ||
                string.IsNullOrEmpty(invoice.RessourceId) ||
                string.IsNullOrEmpty(invoice.SocieteId) ||
                purchaseOrder.EmployeeId == null)
            {
                return false;
            }

            return ((Guid)purchaseOrder.EmployeeId).ToString() == invoice.RessourceId &&
                   ((Guid)purchaseOrder.CompanyId).ToString() == invoice.SocieteId &&
                   DateOnly.FromDateTime(invoice.DateFacturation) >= purchaseOrder.StartDate &&
                   DateOnly.FromDateTime(invoice.DateFacturation) <= purchaseOrder.EndDate;
        }
    }
}
