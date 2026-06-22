using in_ctech_management_backend.Application.Invoices.DTOs;
using in_ctech_management_backend.Application.Shared;
using in_ctech_management_backend.Domain;
using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.Companies.Repositories;
using in_ctech_management_backend.Domain.DeliveryNotes;
using in_ctech_management_backend.Domain.DeliveryNotes.Enums;
using in_ctech_management_backend.Domain.DeliveryNotes.Repository;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.Enums;
using in_ctech_management_backend.Domain.Invoices;
using in_ctech_management_backend.Domain.Invoices.Enums;
using in_ctech_management_backend.Domain.Invoices.Repositories;
using in_ctech_management_backend.Domain.Projects;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;
using in_ctech_management_backend.Domain.PurchaseOrders.Repositories;
using CompanyEntity = in_ctech_management_backend.Domain.Companies.Company;
using EmployeeEntity = in_ctech_management_backend.Domain.Employees.Employee;
using System.Globalization;
using System.Net.Mail;

namespace in_ctech_management_backend.Application.Invoices
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ICompanyRepository _societeRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateFactory _emailTemplateFactory;
        private readonly IDeliveryNoteRepository _deliveryNoteRepository;
        private const decimal ATVatRate = 18m;
        private const decimal WPVatRate = 0m;

        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            IEmployeeRepository employeeRepository,
            ICompanyRepository societeRepository,
            IProjectRepository projectRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IEmailService emailService,
            IEmailTemplateFactory emailTemplateFactory,
            IDeliveryNoteRepository deliveryNoteRepository)
        {
            _invoiceRepository = invoiceRepository;
            _employeeRepository = employeeRepository;
            _societeRepository = societeRepository;
            _projectRepository = projectRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _emailService = emailService;
            _emailTemplateFactory = emailTemplateFactory;
            _deliveryNoteRepository = deliveryNoteRepository;
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, string createdById)
        {
            var reference = await GenerateReferenceAsync();

            var societeId = new CompanyId(Guid.Parse(dto.CompanyId));
            var societe = await _societeRepository.GetByIdAsync(societeId);
            if (societe == null)
                throw new KeyNotFoundException("Societe not found");

            string? ressourceIdStr = null;
            if (dto.Type == InvoiceType.Invoice)
            {
                if (string.IsNullOrEmpty(dto.EmployeeId))
                    throw new ArgumentException("EmployeeId is required for a standard Invoice.");

                var employeeId = new EmployeeId(Guid.Parse(dto.EmployeeId));
                var employee = await _employeeRepository.GetByIdAsync(employeeId);
                if (employee == null)
                    throw new KeyNotFoundException("Employee not found");

                ressourceIdStr = ((Guid)employeeId).ToString();
            }

            string? projectIdStr = null;
            if (!string.IsNullOrEmpty(dto.ProjectId))
            {
                var projectId = new ProjectId(Guid.Parse(dto.ProjectId));
                var project = await _projectRepository.GetByIdAsync(projectId);
                if (project == null)
                    throw new KeyNotFoundException("Project not found");

                projectIdStr = ((Guid)projectId).ToString();
            }

            List<InvoiceWPItem>? wpItems = null;
            if (dto.Type == InvoiceType.InvoiceWP)
            {
                if (dto.WPItems == null || !dto.WPItems.Any())
                    throw new ArgumentException("WP items sont requis pour une Invoice WP.");

                wpItems = MapWPItems(dto.WPItems, dto.PurchaseOrderReference);
            }

            if (dto.DeliveryNoteId.HasValue)
            {
                var existingInvoice = await _invoiceRepository.GetByDeliveryNoteIdAsync(dto.DeliveryNoteId.Value);
                if (existingInvoice != null)
                    throw new ArgumentException(BuildDuplicateInvoiceReason(existingInvoice));
            }

            var invoice = new Invoice(
                reference,
                ressourceIdStr,
                ((Guid)societeId).ToString(),
                projectIdStr,
                dto.TotalExcludingTax,
                dto.VATRate,
                NormalizeDateTimeForPostgres(dto.InvoiceDate),
                dto.BillingMonth ?? 0,
                dto.BillingYear ?? DateTime.UtcNow.Year,
                dto.Type,
                createdById,
                dto.Quantity,
                wpItems,
                dto.PurchaseOrderReference,
                dto.DeliveryNoteId
            );

            await _invoiceRepository.AddAsync(invoice);
            await SendPurchaseOrderAlertsAsync(invoice);

            return MapToDto(invoice, wpItemsOverride: dto.WPItems);
        }

        public async Task<CreateInvoicesFromDeliveryNotesDto> CreateFromDeliveryNotesAsync(
            CreateInvoicesFromDeliveryNotesDto dto,
            string createdBy,
            CancellationToken cancellationToken = default)
        {
            if (dto.DeliveryNoteIds == null || !dto.DeliveryNoteIds.Any())
                throw new ArgumentException("Veuillez sélectionner au moins un bon de livraison.");

            if (!dto.InvoiceDate.HasValue)
                throw new ArgumentException("La date de facture est obligatoire.");

            var created = new List<InvoiceDto>();
            var skipped = new List<CreateInvoicesFromDeliveryNotesDto>();

            foreach (var rawDeliveryNoteId in dto.DeliveryNoteIds)
            {
                if (!Guid.TryParse(rawDeliveryNoteId, out var deliveryNoteGuid))
                {
                    skipped.Add(CreateSkipped(rawDeliveryNoteId, null, "Identifiant du bon de livraison invalide."));
                    continue;
                }

                var deliveryNote = await _deliveryNoteRepository.GetByIdAsync(new DeliveryNoteId(deliveryNoteGuid), cancellationToken);
                if (deliveryNote == null || deliveryNote.IsDeleted)
                {
                    skipped.Add(CreateSkipped(deliveryNoteGuid.ToString(), null, "Bon de livraison introuvable."));
                    continue;
                }

                var existingInvoice = await _invoiceRepository.GetByDeliveryNoteIdAsync(deliveryNoteGuid);
                if (existingInvoice != null)
                {
                    skipped.Add(CreateSkipped(
                        deliveryNoteGuid.ToString(),
                        deliveryNote.Reference,
                        BuildDuplicateInvoiceReason(existingInvoice)));
                    continue;
                }

                try
                {
                    var createInvoiceDto = await BuildInvoiceFromDeliveryNoteAsync(deliveryNote, dto.InvoiceDate.Value, cancellationToken);
                    var invoice = await CreateInvoiceAsync(createInvoiceDto, createdBy);
                    created.Add(invoice);
                }
                catch (Exception ex)
                {
                    skipped.Add(CreateSkipped(deliveryNoteGuid.ToString(), deliveryNote.Reference, GetInnermostExceptionMessage(ex)));
                }
            }

            return new CreateInvoicesFromDeliveryNotesDto
            {
                Created = created,
                Skipped = skipped
            };
        }

        public async Task<List<InvoiceDto>> GetInvoicesAsync(
            string? reference = null,
            string? resourceId = null,
            string? companyId = null,
            string? projectId = null,
            int? billingMonth = null,
            string? type = null,
            string? invoiceDate = null)
        {
            var invoices = await _invoiceRepository.GetAllAsync();
            invoices = invoices.Where(f => !f.IsDeleted).ToList();

            if (!string.IsNullOrEmpty(reference))
                invoices = invoices.Where(f => f.Reference.Contains(reference, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(resourceId))
                invoices = invoices.Where(f => f.RessourceId == resourceId).ToList();

            if (!string.IsNullOrEmpty(companyId))
                invoices = invoices.Where(f => f.SocieteId == companyId).ToList();

            if (!string.IsNullOrEmpty(projectId))
                invoices = invoices.Where(f => f.ProjectId == projectId).ToList();

            if (billingMonth.HasValue)
                invoices = invoices.Where(f => f.MoisFacturation == billingMonth.Value).ToList();

            if (!string.IsNullOrEmpty(type))
                invoices = invoices.Where(f => f.Type.ToString() == type).ToList();

            if (!string.IsNullOrEmpty(invoiceDate) && DateTime.TryParse(invoiceDate, out var date))
                invoices = invoices.Where(f => f.DateFacturation.Date == date.Date).ToList();

            var result = new List<InvoiceDto>();
            foreach (var invoice in invoices)
            {
                EmployeeEntity? employee = null;
                CompanyEntity? company = null;

                if (invoice.Type == InvoiceType.InvoiceWP)
                    employee = await GetWPEmployeeAsync(invoice);
                else if (!string.IsNullOrEmpty(invoice.RessourceId))
                    employee = await _employeeRepository.GetByIdAsync(new EmployeeId(Guid.Parse(invoice.RessourceId)));

                if (!string.IsNullOrEmpty(invoice.SocieteId))
                    company = await _societeRepository.GetByIdAsync(new CompanyId(Guid.Parse(invoice.SocieteId)));

                string? purchaseOrderReference = null;
                if (invoice.Type == InvoiceType.Invoice)
                    purchaseOrderReference = await GetATBonCmdReferenceAsync(invoice);

                result.Add(MapToDto(invoice, employee, company, purchaseOrderReference));
            }
            return result;
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(string reference)
        {
            var invoice = await GetInvoiceByIdOrReferenceAsync(reference);
            if (invoice == null) return null;

            EmployeeEntity? employee = null;
            CompanyEntity? company = null;

            if (invoice.Type == InvoiceType.InvoiceWP)
                employee = await GetWPEmployeeAsync(invoice);
            else if (!string.IsNullOrEmpty(invoice.RessourceId))
                employee = await _employeeRepository.GetByIdAsync(new EmployeeId(Guid.Parse(invoice.RessourceId)));

            if (!string.IsNullOrEmpty(invoice.SocieteId))
                company = await _societeRepository.GetByIdAsync(new CompanyId(Guid.Parse(invoice.SocieteId)));

            string? purchaseOrderReference = null;
            if (invoice.Type == InvoiceType.Invoice)
                purchaseOrderReference = await GetATBonCmdReferenceAsync(invoice);

            return MapToDto(invoice, employee, company, purchaseOrderReference);
        }

        public async Task<InvoiceDocumentDataDto?> GetInvoiceDocumentDataAsync(string reference, string? template = null, bool signed = false, CancellationToken cancellationToken = default)
        {
            var invoice = await _invoiceRepository.GetByReferenceAsync(reference);
            if (invoice == null || invoice.IsDeleted)
                return null;

            var company = await _societeRepository.GetByIdAsync(new CompanyId(Guid.Parse(invoice.SocieteId)));
            if (company == null)
                return null;

            Domain.Employees.Employee? employee = null;
            if (!string.IsNullOrWhiteSpace(invoice.RessourceId))
                employee = await _employeeRepository.GetByIdAsync(new EmployeeId(Guid.Parse(invoice.RessourceId)));

            var isWinity = string.Equals(template, "winity", StringComparison.OrdinalIgnoreCase);
            var normalizedTemplate = isWinity ? "winity" : "in-ctech";
            var tvaRate = isWinity ? 0m : invoice.TauxTVA;
            var tva = Math.Round(invoice.TotalHT * (tvaRate / 100m), 2, MidpointRounding.AwayFromZero);
            var ttc = Math.Round(invoice.TotalHT + tva, 2, MidpointRounding.AwayFromZero);
            var billingMonthLabel = invoice.MoisFacturation > 0
                ? $"{invoice.MoisFacturation:D2}/{invoice.DateFacturation.Year}"
                : invoice.DateFacturation.ToString("MM/yyyy", CultureInfo.InvariantCulture);

            var items = BuildDocumentItems(invoice, employee, billingMonthLabel);
            var bcNumber = invoice.Type == InvoiceType.InvoiceWP
                ? GetWPPurchaseOrderReference(invoice)
                : invoice.PurchaseOrderReference;

            return new InvoiceDocumentDataDto(
                invoice.Reference,
                BuildFileName(invoice.Reference, isWinity),
                normalizedTemplate,
                company.Nom,
                string.IsNullOrWhiteSpace(company.Adresse) ? company.Pays : company.Adresse,
                company.Code,
                company.Pays,
                invoice.DateFacturation,
                bcNumber,
                Math.Round(invoice.TotalHT, 2, MidpointRounding.AwayFromZero),
                tva,
                ttc,
                isWinity ? "EUR" : "MAD",
                BuildAmountInWords(ttc, isWinity ? "euro" : "dirham"),
                signed,
                items);
        }

        public async Task<InvoiceDto?> UpdateInvoiceAsync(string reference, UpdateInvoiceDto dto, string updatedBy = "System")
        {
            var invoice = await GetInvoiceByIdOrReferenceAsync(reference);
            if (invoice == null) return null;

            if (dto.TotalExcludingTax.HasValue) invoice.TotalHT = dto.TotalExcludingTax.Value;
            if (dto.VATRate.HasValue) invoice.TauxTVA = dto.VATRate.Value;
            if (dto.InvoiceDate.HasValue) invoice.DateFacturation = NormalizeDateTimeForPostgres(dto.InvoiceDate.Value);
            if (dto.BillingMonth.HasValue) invoice.MoisFacturation = dto.BillingMonth.Value;
            if (dto.BillingYear.HasValue) invoice.BillingYear = dto.BillingYear.Value;
            if (dto.Quantity.HasValue) invoice.Quantity = dto.Quantity.Value;
            if (!string.IsNullOrEmpty(dto.EmployeeId)) invoice.RessourceId = dto.EmployeeId;
            if (!string.IsNullOrEmpty(dto.CompanyId)) invoice.SocieteId = dto.CompanyId;
            if (dto.ProjectId != null) invoice.ProjectId = dto.ProjectId;
            var updatedPurchaseOrderReference = dto.PurchaseOrderReference;
            if (updatedPurchaseOrderReference != null) invoice.PurchaseOrderReference = updatedPurchaseOrderReference;

            if (dto.Type.HasValue)
            {
                invoice.Type = dto.Type.Value;

                if (dto.Type == InvoiceType.InvoiceWP)
                {
                    if (dto.WPItems == null || !dto.WPItems.Any())
                        throw new ArgumentException("WP items sont requis pour une Invoice WP.");

                    invoice.WPItems = MapWPItems(dto.WPItems, dto.PurchaseOrderReference);
                }
                else
                {
                    invoice.WPItems = null;
                }
            }
            else if (dto.WPItems != null)
            {
                if (invoice.Type != InvoiceType.InvoiceWP)
                    throw new ArgumentException("WP items can only be updated for an Invoice WP.");

                invoice.WPItems = MapWPItems(dto.WPItems, dto.PurchaseOrderReference);
            }
            else if (updatedPurchaseOrderReference != null && invoice.Type == InvoiceType.InvoiceWP)
            {
                if (invoice.WPItems == null || !invoice.WPItems.Any())
                    throw new ArgumentException("WP items sont requis pour une Invoice WP.");

                invoice.WPItems[0].ReferenceBC = updatedPurchaseOrderReference;
            }

            invoice.UpdatedBy = updatedBy;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _invoiceRepository.UpdateAsync(invoice);
            EmployeeEntity? employee = null;
            CompanyEntity? company = null;

            if (invoice.Type == InvoiceType.InvoiceWP)
                employee = await GetWPEmployeeAsync(invoice);
            else if (!string.IsNullOrEmpty(invoice.RessourceId))
                employee = await _employeeRepository.GetByIdAsync(new EmployeeId(Guid.Parse(invoice.RessourceId)));

            if (!string.IsNullOrEmpty(invoice.SocieteId))
                company = await _societeRepository.GetByIdAsync(new CompanyId(Guid.Parse(invoice.SocieteId)));

            string? purchaseOrderReference = null;
            if (invoice.Type == InvoiceType.Invoice)
                purchaseOrderReference = await GetATBonCmdReferenceAsync(invoice);

            return MapToDto(invoice, employee, company, purchaseOrderReference);
        }

        public async Task<bool> DeleteInvoiceAsync(string reference, string deletedBy = "System")
        {
            var invoice = await _invoiceRepository.GetByReferenceAsync(reference);
            if (invoice == null) return false;

            invoice.IsDeleted = true;
            invoice.DeletedBy = deletedBy;
            invoice.DeletedAt = DateTime.UtcNow;

            await _invoiceRepository.UpdateAsync(invoice);
            return true;
        }

        public async Task<bool> UpdateStatusAsync(string reference, string status, string updatedBy, DateTime? paymentDate, string? transferReference)
        {
            var invoice = await _invoiceRepository.GetByReferenceAsync(reference);
            if (invoice == null) return false;

            if (!Enum.TryParse<InvoiceStatus>(status, out var parsedStatus))
                throw new ArgumentException($"Statut invalide : {status}");

            invoice.Status = parsedStatus;
            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.UpdatedBy = updatedBy;

            if (parsedStatus == InvoiceStatus.Payee)
            {
                invoice.PaymentDate = paymentDate;
                invoice.TransferReference = transferReference;
            }

            await _invoiceRepository.UpdateAsync(invoice);
            return true;
        }

        public async Task<bool> UpdateBulkStatusAsync(List<string> references, string status, string updatedBy, DateTime? paymentDate, string? transferReference)
        {
            if (!Enum.TryParse<InvoiceStatus>(status, out var parsedStatus))
                throw new ArgumentException($"Statut invalide : {status}");

            var invoices = await _invoiceRepository.GetByReferencesAsync(references);
            if (invoices == null || !invoices.Any()) return false;

            foreach (var invoice in invoices)
            {
                invoice.Status = parsedStatus;
                invoice.UpdatedAt = DateTime.UtcNow;
                invoice.UpdatedBy = updatedBy;

                if (parsedStatus == InvoiceStatus.Payee)
                {
                    invoice.PaymentDate = paymentDate;
                    invoice.TransferReference = transferReference;
                }

                await _invoiceRepository.UpdateAsync(invoice);
            }

            return true;
        }
        private InvoiceDto MapToDto(Invoice invoice, EmployeeEntity? employee = null, CompanyEntity? company = null, string? purchaseOrderReference = null, List<InvoiceWPItemDto>? wpItemsOverride = null)
        {
            return new InvoiceDto
            {
                Reference = invoice.Reference,
                EmployeeId = invoice.RessourceId,
                CompanyId = invoice.SocieteId,
                DeliveryNoteId = invoice.DeliveryNoteId?.ToString(),
                ProjectId = invoice.ProjectId,
                PurchaseOrderReference = invoice.Type == InvoiceType.InvoiceWP
                    ? GetWPPurchaseOrderReference(invoice)
                    : invoice.PurchaseOrderReference ?? purchaseOrderReference,
                TotalExcludingTax = invoice.TotalHT,
                VATRate = invoice.TauxTVA,
                InvoiceDate = invoice.DateFacturation,
                BillingMonth = invoice.MoisFacturation,
                BillingYear = invoice.BillingYear,
                Quantity = invoice.Quantity,
                Type = invoice.Type,
                Status = invoice.Status.ToString(),
                PaymentDate = invoice.PaymentDate,
                TransferReference = invoice.TransferReference,
                CreatedBy = invoice.CreatedBy,
                CreatedAt = invoice.CreatedAt,
                UpdatedBy = invoice.UpdatedBy,
                UpdatedAt = invoice.UpdatedAt,
                Resource = employee != null ? new ResourceDto
                {
                    FullName = employee.FullName,
                    Trigram = employee.Trigram
                } : null,
                Client = company != null ? new ClientDto
                {
                    Nom = company.Nom,
                    Adresse = company.Adresse,
                    Pays = company.Pays
                } : null,
                WPItems = wpItemsOverride ?? invoice.WPItems?.Select(i => new InvoiceWPItemDto
                {
                    PurchaseOrderReference = i.ReferenceBC,
                    Description = i.Designation,
                    UnitPrice = i.PrixUnitaire,
                    Quantity = i.Quantite
                }).ToList()
            };
        }

        private async Task SendPurchaseOrderAlertsAsync(Invoice invoice)
        {
            var purchaseOrders = await GetInvoicePurchaseOrdersAsync(invoice);

            foreach (var purchaseOrder in purchaseOrders)
            {
                var totalAmount = Convert.ToDecimal(purchaseOrder.TotalAmount);
                if (totalAmount <= 0)
                    continue;

                var consumedAmount = await CalculateConsumedAmountAsync(purchaseOrder);
                var remainingAmount = totalAmount - consumedAmount;
                var remainingPercentage = Math.Round((remainingAmount / totalAmount) * 100m, 2, MidpointRounding.AwayFromZero);
                var daysRemaining = purchaseOrder.EndDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;

                if (remainingPercentage > 10m && daysRemaining > 30)
                    continue;

                var recipients = await GetPurchaseOrderAlertRecipientsAsync(purchaseOrder);
                if (recipients.Count == 0)
                    continue;

                var subject = $"Alerte BC {purchaseOrder.Reference}";
                var body = await BuildPurchaseOrderAlertBodyAsync(
                    purchaseOrder.Reference,
                    totalAmount,
                    consumedAmount,
                    remainingAmount,
                    remainingPercentage,
                    purchaseOrder.EndDate,
                    daysRemaining);

                await TrySendPurchaseOrderAlertEmailsAsync(recipients, subject, body);
            }
        }

        private async Task<List<Domain.PurchaseOrders.PurchaseOrder>> GetInvoicePurchaseOrdersAsync(Invoice invoice)
        {
            var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(invoice.PurchaseOrderReference))
                references.Add(invoice.PurchaseOrderReference);

            if (invoice.WPItems != null)
            {
                foreach (var item in invoice.WPItems.Where(item => !string.IsNullOrWhiteSpace(item.ReferenceBC)))
                    references.Add(item.ReferenceBC);
            }

            if (references.Count > 0)
            {
                var purchaseOrders = new List<Domain.PurchaseOrders.PurchaseOrder>();
                foreach (var reference in references)
                {
                    var purchaseOrder = await _purchaseOrderRepository.GetByReferenceAsync(reference);
                    if (purchaseOrder != null)
                        purchaseOrders.Add(purchaseOrder);
                }

                return purchaseOrders
                    .GroupBy(po => po.PurchaseOrderId.Value)
                    .Select(group => group.First())
                    .ToList();
            }

            if (invoice.Type != InvoiceType.Invoice ||
                string.IsNullOrEmpty(invoice.RessourceId) ||
                string.IsNullOrEmpty(invoice.SocieteId))
            {
                return new List<Domain.PurchaseOrders.PurchaseOrder>();
            }

            var allPurchaseOrders = await _purchaseOrderRepository.GetAllAsync();
            return allPurchaseOrders
                .Where(po => IsInvoiceLinkedToPurchaseOrder(invoice, po))
                .ToList();
        }

        private async Task<decimal> CalculateConsumedAmountAsync(Domain.PurchaseOrders.PurchaseOrder purchaseOrder)
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
            if (!IsInvoiceLinkedToPurchaseOrder(invoice, purchaseOrder))
                return 0m;

            if (invoice.Type == InvoiceType.InvoiceWP && invoice.WPItems != null)
            {
                return invoice.WPItems
                    .Where(item => string.Equals(item.ReferenceBC, purchaseOrder.Reference, StringComparison.OrdinalIgnoreCase))
                    .Sum(item => item.PrixUnitaire * item.Quantite);
            }

            return invoice.TotalHT;
        }

        private static bool IsInvoiceLinkedToPurchaseOrder(
            Invoice invoice,
            Domain.PurchaseOrders.PurchaseOrder purchaseOrder)
        {
            if (string.Equals(invoice.PurchaseOrderReference, purchaseOrder.Reference, StringComparison.OrdinalIgnoreCase))
                return true;

            if (invoice.Type == InvoiceType.InvoiceWP)
                return invoice.WPItems?.Any(item => string.Equals(item.ReferenceBC, purchaseOrder.Reference, StringComparison.OrdinalIgnoreCase)) == true;

            if (purchaseOrder.EngagementMode != EngagementMode.AT ||
                purchaseOrder.EmployeeId == null ||
                string.IsNullOrEmpty(invoice.RessourceId) ||
                string.IsNullOrEmpty(invoice.SocieteId))
            {
                return false;
            }

            return ((Guid)purchaseOrder.EmployeeId).ToString() == invoice.RessourceId &&
                   ((Guid)purchaseOrder.CompanyId).ToString() == invoice.SocieteId &&
                   DateOnly.FromDateTime(invoice.DateFacturation) >= purchaseOrder.StartDate &&
                   DateOnly.FromDateTime(invoice.DateFacturation) <= purchaseOrder.EndDate;
        }

        private async Task TrySendPurchaseOrderAlertEmailsAsync(
            IEnumerable<string> recipients,
            string subject,
            string body)
        {
            foreach (var recipient in recipients)
            {
                try
                {
                    await _emailService.SendEmailAsync(recipient, subject, body);
                }
                catch
                {
                }
            }
        }

        private async Task<HashSet<string>> GetPurchaseOrderAlertRecipientsAsync(Domain.PurchaseOrders.PurchaseOrder purchaseOrder)
        {
            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            TryAddEmail(recipients, purchaseOrder.UpdatedBy);
            TryAddEmail(recipients, purchaseOrder.CreatedBy);

            if (purchaseOrder.EmployeeId != null)
            {
                var employee = await _employeeRepository.GetByIdAsync(purchaseOrder.EmployeeId);
                TryAddEmail(recipients, employee?.Email);
            }

            return recipients;
        }

        private static void TryAddEmail(HashSet<string> recipients, string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            try
            {
                recipients.Add(new MailAddress(email).Address);
            }
            catch (FormatException)
            {
            }
        }

        private async Task<string> BuildPurchaseOrderAlertBodyAsync(
            string reference,
            decimal totalAmount,
            decimal consumedAmount,
            decimal remainingAmount,
            decimal remainingPercentage,
            DateOnly endDate,
            int daysRemaining)
        {
            static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);

            var encodedReference = System.Net.WebUtility.HtmlEncode(reference);
            var isOverBudget = remainingAmount < 0;
            var isLowBudget = remainingPercentage <= 10m;
            var isNearDeadline = daysRemaining <= 30;
            var badges = new List<string>();

            if (isOverBudget)
                badges.Add("Dépassement");
            if (isLowBudget)
                badges.Add("Seuil 10% atteint");
            if (isNearDeadline)
                badges.Add("Échéance ≤ 30 jours");

            var template = await _emailTemplateFactory.GetTemplateAsync(EmailTemplateType.PurchaseOrderAlert);
            var replacements = new Dictionary<string, string>
            {
                { "Reference", encodedReference },
                { "AlertReasons", System.Net.WebUtility.HtmlEncode(string.Join(" • ", badges)) },
                { "TotalAmount", Money(totalAmount) },
                { "ConsumedAmount", Money(consumedAmount) },
                { "RemainingAmount", Money(remainingAmount) },
                { "RemainingPercentage", remainingPercentage.ToString("N2", CultureInfo.InvariantCulture) },
                { "EndDate", endDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) },
                { "DaysRemaining", daysRemaining.ToString(CultureInfo.InvariantCulture) }
            };

            foreach (var replacement in replacements)
                template = template.Replace($"{{{{{replacement.Key}}}}}", replacement.Value);

            return template;
        }

        private static List<InvoiceDocumentItemDto> BuildDocumentItems(Invoice invoice, Domain.Employees.Employee? employee, string billingMonthLabel)
        {
            if (invoice.Type == InvoiceType.InvoiceWP)
            {
                return invoice.WPItems?.Select(item => new InvoiceDocumentItemDto(
                    item.Designation,
                    item.Quantite,
                    item.PrixUnitaire,
                    Math.Round(item.PrixUnitaire * item.Quantite, 2, MidpointRounding.AwayFromZero),
                    null,
                    null,
                    item.ReferenceBC)).ToList()
                    ?? new List<InvoiceDocumentItemDto>();
            }

            var unitPrice = employee?.DailyRate is > 0
                ? Convert.ToDecimal(employee.DailyRate.Value)
                : 0m;
            var quantity = unitPrice > 0
                ? Math.Round(invoice.TotalHT / unitPrice, 2, MidpointRounding.AwayFromZero)
                : 0m;

            return new List<InvoiceDocumentItemDto>
            {
                new(
                    "Prestation de développement informatique",
                    quantity,
                    unitPrice,
                    Math.Round(invoice.TotalHT, 2, MidpointRounding.AwayFromZero),
                    employee?.FullName,
                    billingMonthLabel,
                    null)
            };
        }

        private static List<InvoiceWPItem> MapWPItems(IEnumerable<InvoiceWPItemDto> items, string? fallbackPurchaseOrderReference = null)
        {
            return items.Select(i => new InvoiceWPItem
            {
                ReferenceBC = string.IsNullOrWhiteSpace(i.PurchaseOrderReference)
                    ? fallbackPurchaseOrderReference
                    : i.PurchaseOrderReference,
                Designation = i.Description,
                PrixUnitaire = i.UnitPrice,
                Quantite = i.Quantity
            }).ToList();
        }

        private static string? GetWPPurchaseOrderReference(Invoice invoice)
        {
            return invoice.WPItems?.FirstOrDefault()?.ReferenceBC;
        }

        private static CreateInvoicesFromDeliveryNotesDto CreateSkipped(string deliveryNoteId, string? reference, string reason)
        {
            return new CreateInvoicesFromDeliveryNotesDto
            {
                DeliveryNoteId = deliveryNoteId,
                Reference = reference,
                Reason = reason
            };
        }

        private static string GetInnermostExceptionMessage(Exception exception)
        {
            var currentException = exception;
            while (currentException.InnerException != null)
            {
                currentException = currentException.InnerException;
            }

            if (currentException.Message.Contains("IX_Invoices_DeliveryNoteId", StringComparison.OrdinalIgnoreCase))
                return "Une facture existe déjà pour ce bon de livraison.";

            return currentException.Message;
        }

        private static string BuildDuplicateInvoiceReason(Invoice existingInvoice)
        {
            return existingInvoice.IsDeleted
                ? $"La facture {existingInvoice.Reference} existe déjà pour ce bon de livraison, mais elle est supprimée."
                : $"La facture {existingInvoice.Reference} existe déjà pour ce bon de livraison.";
        }

        private async Task<CreateInvoiceDto> BuildInvoiceFromDeliveryNoteAsync(
            Domain.DeliveryNotes.DeliveryNote deliveryNote,
            DateTime invoiceDate,
            CancellationToken cancellationToken)
        {
            if (deliveryNote.CompanyId == null)
                throw new ArgumentException("Le client est obligatoire.");

            var company = await _societeRepository.GetByIdAsync(deliveryNote.CompanyId, cancellationToken);
            if (company == null)
                throw new ArgumentException("Client introuvable.");

            if (!TryGetInvoiceQuantity(deliveryNote.Quantity, out var quantity))
                throw new ArgumentException("La quantité est obligatoire et doit être un nombre entier supérieur à zéro.");

            return deliveryNote.Type == DeliveryNoteType.AT
                ? await BuildATInvoiceFromDeliveryNoteAsync(deliveryNote, invoiceDate, quantity, cancellationToken)
                : await BuildWPInvoiceFromDeliveryNoteAsync(deliveryNote, invoiceDate, quantity, cancellationToken);
        }

        private async Task<CreateInvoiceDto> BuildATInvoiceFromDeliveryNoteAsync(
            Domain.DeliveryNotes.DeliveryNote deliveryNote,
            DateTime invoiceDate,
            int quantity,
            CancellationToken cancellationToken)
        {
            if (deliveryNote.EmployeeId == null)
                throw new ArgumentException("La ressource est obligatoire pour un bon de livraison AT.");

            if (!deliveryNote.Month.HasValue || !deliveryNote.Year.HasValue)
                throw new ArgumentException("Le mois et l'année de facturation sont obligatoires pour une facture AT.");

            var employee = await _employeeRepository.GetByIdAsync(deliveryNote.EmployeeId, cancellationToken);
            if (employee == null)
                throw new ArgumentException("Ressource introuvable.");

            var totalExcludingTax = GetPositiveAmountOrFallback(
                deliveryNote.Amount,
                () => (employee.DailyRate ?? 0) * deliveryNote.Quantity);

            if (totalExcludingTax <= 0)
                throw new ArgumentException("Le total HT doit être supérieur à zéro.");

            return new CreateInvoiceDto
            {
                EmployeeId = deliveryNote.EmployeeId.Value.ToString(),
                CompanyId = deliveryNote.CompanyId.Value.ToString(),
                DeliveryNoteId = deliveryNote.DeliveryNoteId.Value,
                TotalExcludingTax = totalExcludingTax,
                VATRate = ATVatRate,
                InvoiceDate = invoiceDate,
                BillingMonth = deliveryNote.Month.Value,
                BillingYear = deliveryNote.Year.Value,
                Quantity = quantity,
                Type = InvoiceType.Invoice
            };
        }

        private async Task<CreateInvoiceDto> BuildWPInvoiceFromDeliveryNoteAsync(
            Domain.DeliveryNotes.DeliveryNote deliveryNote,
            DateTime invoiceDate,
            int quantity,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(deliveryNote.Designation))
                throw new ArgumentException("La désignation est obligatoire pour un bon de livraison WP.");

            if (!deliveryNote.UnitPrice.HasValue || deliveryNote.UnitPrice.Value <= 0)
                throw new ArgumentException("Le prix unitaire est obligatoire pour un bon de livraison WP.");

            var totalExcludingTax = GetPositiveAmountOrFallback(
                deliveryNote.Amount,
                () => deliveryNote.UnitPrice.Value * deliveryNote.Quantity);

            if (totalExcludingTax <= 0)
                throw new ArgumentException("Le total HT doit être supérieur à zéro.");

            var purchaseOrderReference = await GetDeliveryNotePurchaseOrderReferenceAsync(deliveryNote, cancellationToken);

            return new CreateInvoiceDto
            {
                CompanyId = deliveryNote.CompanyId.Value.ToString(),
                DeliveryNoteId = deliveryNote.DeliveryNoteId.Value,
                TotalExcludingTax = totalExcludingTax,
                VATRate = WPVatRate,
                InvoiceDate = invoiceDate,
                BillingYear = invoiceDate.Year,
                Quantity = quantity,
                Type = InvoiceType.InvoiceWP,
                PurchaseOrderReference = purchaseOrderReference,
                WPItems = new List<InvoiceWPItemDto>
                {
                    new()
                    {
                        PurchaseOrderReference = purchaseOrderReference ?? string.Empty,
                        Description = deliveryNote.Designation,
                        UnitPrice = Convert.ToDecimal(deliveryNote.UnitPrice.Value),
                        Quantity = quantity
                    }
                }
            };
        }

        private async Task<string?> GetDeliveryNotePurchaseOrderReferenceAsync(
            Domain.DeliveryNotes.DeliveryNote deliveryNote,
            CancellationToken cancellationToken)
        {
            if (deliveryNote.PurchaseOrderId == null)
                return null;

            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(deliveryNote.PurchaseOrderId, cancellationToken);
            return purchaseOrder?.Reference;
        }

        private static decimal GetPositiveAmountOrFallback(double amount, Func<double> fallback)
        {
            var selectedAmount = amount > 0 ? amount : fallback();
            return selectedAmount > 0 ? Convert.ToDecimal(selectedAmount) : 0m;
        }

        private static bool TryGetInvoiceQuantity(double value, out int quantity)
        {
            quantity = 0;

            if (value <= 0)
                return false;

            var rounded = Math.Round(value);
            if (Math.Abs(value - rounded) > 0.000001)
                return false;

            quantity = Convert.ToInt32(rounded);
            return quantity > 0;
        }

        private static string BuildAmountInWords(decimal amount, string currencyUnit)
        {
            var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            var dirhams = (long)decimal.Truncate(roundedAmount);
            var centimes = (int)((roundedAmount - dirhams) * 100);
            var currencyLabel = dirhams > 1 ? $"{currencyUnit}s" : currencyUnit;

            var dirhamsWords = NumberToFrenchWords(dirhams);
            if (centimes == 0)
                return $"{Capitalize(dirhamsWords)} {currencyLabel}";

            var centimesWords = NumberToFrenchWords(centimes);
            return $"{Capitalize(dirhamsWords)} {currencyLabel} et {centimesWords} centimes";
        }

        private static string BuildFileName(string reference, bool isWinity)
        {
            if (!isWinity)
                return reference;

            var parts = reference.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 4)
                return $"FAC-{parts[^2]}-{parts[^1]}";

            return reference;
        }

        private static string NumberToFrenchWords(long number)
        {
            if (number == 0) return "zero";
            if (number < 0) return $"moins {NumberToFrenchWords(Math.Abs(number))}";

            if (number >= 1_000_000)
            {
                var millions = number / 1_000_000;
                var remainder = number % 1_000_000;
                var millionLabel = millions > 1 ? "millions" : "million";
                return remainder == 0
                    ? $"{NumberToFrenchWords(millions)} {millionLabel}"
                    : $"{NumberToFrenchWords(millions)} {millionLabel} {NumberToFrenchWords(remainder)}";
            }

            if (number >= 1000)
            {
                var thousands = number / 1000;
                var remainder = number % 1000;
                var thousandsPart = thousands == 1 ? "mille" : $"{NumberToFrenchWords(thousands)} mille";
                return remainder == 0 ? thousandsPart : $"{thousandsPart} {NumberToFrenchWords(remainder)}";
            }

            if (number >= 100)
            {
                var hundreds = number / 100;
                var remainder = number % 100;
                var hundredsPart = hundreds == 1 ? "cent" : $"{Units[hundreds]} cent";
                if (remainder == 0 && hundreds > 1)
                    hundredsPart += "s";
                return remainder == 0 ? hundredsPart : $"{hundredsPart} {NumberToFrenchWords(remainder)}";
            }

            if (number < 20)
                return Units[number];

            if (number < 70)
            {
                var tens = (int)(number / 10);
                var remainder = (int)(number % 10);
                var tensWord = Tens[tens];
                if (remainder == 0) return tensWord;
                if (remainder == 1) return $"{tensWord} et un";
                return $"{tensWord}-{Units[remainder]}";
            }

            if (number < 80)
            {
                if (number == 71) return "soixante et onze";
                return $"soixante-{NumberToFrenchWords(number - 60)}";
            }

            if (number == 80) return "quatre-vingts";
            return $"quatre-vingt-{NumberToFrenchWords(number - 80)}";
        }

        private static string Capitalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? value
                : char.ToUpperInvariant(value[0]) + value[1..];
        }

        private static readonly Dictionary<long, string> Units = new()
        {
            [0] = "zero",
            [1] = "un",
            [2] = "deux",
            [3] = "trois",
            [4] = "quatre",
            [5] = "cinq",
            [6] = "six",
            [7] = "sept",
            [8] = "huit",
            [9] = "neuf",
            [10] = "dix",
            [11] = "onze",
            [12] = "douze",
            [13] = "treize",
            [14] = "quatorze",
            [15] = "quinze",
            [16] = "seize",
            [17] = "dix-sept",
            [18] = "dix-huit",
            [19] = "dix-neuf"
        };

        private static readonly Dictionary<int, string> Tens = new()
        {
            [2] = "vingt",
            [3] = "trente",
            [4] = "quarante",
            [5] = "cinquante",
            [6] = "soixante"
        };

        private async Task<string> GenerateReferenceAsync()
        {
            var invoices = await _invoiceRepository.GetAllAsync();
            var year = DateTime.UtcNow.Year;

            var lastInvoice = invoices
                .Where(f => f.Reference != null && f.Reference.Contains($"-{year}-"))
                .OrderByDescending(f => f.Reference)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                var lastPart = lastInvoice.Reference.Split('-').Last();
                nextNumber = int.Parse(lastPart) + 1;
            }

            return $"IN-C-{year}-{nextNumber.ToString("D4")}";
        }

        private async Task<Invoice?> GetInvoiceByIdOrReferenceAsync(string idOrReference)
        {
            if (Guid.TryParse(idOrReference, out var id))
                return await _invoiceRepository.GetByIdAsync(InvoiceId.FromGuid(id));

            return await _invoiceRepository.GetByReferenceAsync(idOrReference);
        }

        private static DateTime NormalizeDateTimeForPostgres(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private async Task<EmployeeEntity?> GetWPEmployeeAsync(Invoice invoice)
        {
            if (invoice.WPItems == null || !invoice.WPItems.Any())
                return null;

            var firstReference = invoice.WPItems[0].ReferenceBC;
            if (string.IsNullOrEmpty(firstReference))
                return null;

            var purchaseOrder = await _purchaseOrderRepository.GetByReferenceAsync(firstReference);
            if (purchaseOrder?.EmployeeId == null)
                return null;

            return await _employeeRepository.GetByIdAsync(purchaseOrder.EmployeeId);
        }

        private async Task<string?> GetATBonCmdReferenceAsync(Invoice invoice)
        {
            if (!string.IsNullOrWhiteSpace(invoice.PurchaseOrderReference))
                return invoice.PurchaseOrderReference;

            if (string.IsNullOrEmpty(invoice.RessourceId) || string.IsNullOrEmpty(invoice.SocieteId))
                return null;

            var allPurchaseOrders = await _purchaseOrderRepository.GetAllAsync();
            var bonCmd = allPurchaseOrders.FirstOrDefault(p =>
                p.EmployeeId != null &&
                ((Guid)p.EmployeeId).ToString() == invoice.RessourceId &&
                ((Guid)p.CompanyId).ToString() == invoice.SocieteId &&
                p.EngagementMode == EngagementMode.AT
            );

            return bonCmd?.Reference;
        }
    }
}
