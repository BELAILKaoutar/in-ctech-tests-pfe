using in_ctech_management_backend.Application.CreditNotes.DTOs;
using in_ctech_management_backend.Application.Invoices.DTOs;
using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.Companies.Repositories;
using in_ctech_management_backend.Domain.CreditNotes;
using in_ctech_management_backend.Domain.CreditNotes.Repositories;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.Invoices;
using in_ctech_management_backend.Domain.Invoices.Enums;
using in_ctech_management_backend.Domain.Invoices.Repositories;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;
using in_ctech_management_backend.Domain.PurchaseOrders.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmployeeEntity = in_ctech_management_backend.Domain.Employees.Employee;

namespace in_ctech_management_backend.Application.CreditNotes
{
    public class CreditNoteService : ICreditNoteService
    {
        private readonly ICreditNoteRepository _creditNoteRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        public CreditNoteService(
            ICreditNoteRepository creditNoteRepository,
            IInvoiceRepository invoiceRepository,
            IEmployeeRepository employeeRepository,
            ICompanyRepository companyRepository,
            IPurchaseOrderRepository purchaseOrderRepository)
        {
            _creditNoteRepository = creditNoteRepository;
            _invoiceRepository = invoiceRepository;
            _employeeRepository = employeeRepository;
            _companyRepository = companyRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
        }

        public async Task<CreditNoteDto> CreateCreditNoteAsync(CreateCreditNoteDto dto, string createdBy)
        {
            var reference = await GenerateReferenceAsync();
            CreditNote creditNote;

            if (dto.IsFromInvoice)
            {
                if (string.IsNullOrWhiteSpace(dto.OriginalInvoiceReference))
                    throw new ArgumentException("La référence de la facture d'origine est obligatoire.");

                var originalInvoice = await _invoiceRepository.GetByReferenceAsync(dto.OriginalInvoiceReference);
                if (originalInvoice == null)
                    throw new KeyNotFoundException($"Facture '{dto.OriginalInvoiceReference}' introuvable.");

                var designation = $"Avoir sur facture : {originalInvoice.Reference}";

                decimal unitPrice;
                decimal quantity;

                if (originalInvoice.Type == InvoiceType.InvoiceWP)
                {
                    unitPrice = (decimal)(originalInvoice.WPItems?.FirstOrDefault()?.PrixUnitaire ?? 0);
                    quantity = originalInvoice.WPItems?.FirstOrDefault()?.Quantite ?? 1;
                }
                else
                {
                    var employee = !string.IsNullOrEmpty(originalInvoice.RessourceId)
                        ? await _employeeRepository.GetByIdAsync(new EmployeeId(Guid.Parse(originalInvoice.RessourceId)))
                        : null;
                    unitPrice = (decimal)(employee?.DailyRate ?? 0);
                    quantity = originalInvoice.Quantity;
                }

                Domain.Companies.Company? invoiceCompany = null;
                if (!string.IsNullOrEmpty(originalInvoice.SocieteId))
                    invoiceCompany = await _companyRepository.GetByIdAsync(new CompanyId(Guid.Parse(originalInvoice.SocieteId)));
                var currency = invoiceCompany?.Pays?.ToLower().Contains("maroc") == true ? "DH" : "€";

                creditNote = CreditNote.CreateFromInvoice(
                    reference: reference,
                    originalInvoiceId: originalInvoice.Id,
                    designation: designation,
                    unitPrice: unitPrice,
                    quantity: -Math.Abs(quantity),
                    vatRate: originalInvoice.TauxTVA,
                    invoiceDate: originalInvoice.DateFacturation,
                    billingMonth: originalInvoice.MoisFacturation,
                    societeId: originalInvoice.SocieteId,
                    ressourceId: originalInvoice.RessourceId,
                    currency: currency
                );
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Designation))
                    throw new ArgumentException("La désignation est obligatoire.");
                if (dto.UnitPrice == null || dto.Quantity == null || dto.InvoiceDate == null)
                    throw new ArgumentException("Prix unitaire, quantité et date sont obligatoires.");

                Domain.Companies.Company? manualCompany = null;
                if (!string.IsNullOrEmpty(dto.SocieteId))
                    manualCompany = await _companyRepository.GetByIdAsync(new CompanyId(Guid.Parse(dto.SocieteId)));
                var currency = manualCompany?.Pays?.ToLower().Contains("maroc") == true
                    ? "DH"
                    : dto.Currency ?? "€";

                creditNote = CreditNote.CreateManual(
                    reference: reference,
                    designation: dto.Designation,
                    unitPrice: dto.UnitPrice.Value,
                    quantity: -Math.Abs(dto.Quantity.Value),
                    vatRate: dto.VATRate ?? 0,
                    invoiceDate: dto.InvoiceDate.Value,
                    billingMonth: dto.BillingMonth ?? 0,
                    societeId: dto.SocieteId,
                    ressourceId: dto.RessourceId,
                    currency: currency,
                    address: dto.Address,
                    companyCode: dto.CompanyCode,
                    companyName: dto.CompanyName
                );
            }

            creditNote.CreatedBy = createdBy;
            await _creditNoteRepository.AddAsync(creditNote);
            return await MapToDtoAsync(creditNote, creditNote.OriginalInvoice);
        }

        public async Task<List<CreditNoteDto>> GetAllCreditNotesAsync()
        {
            var creditNotes = await _creditNoteRepository.GetAllAsync();
            var result = new List<CreditNoteDto>();

            foreach (var creditNote in creditNotes)
            {
                Invoice? originalInvoice = null;
                if (creditNote.IsFromInvoice && creditNote.OriginalInvoiceId != null)
                    originalInvoice = creditNote.OriginalInvoice
                        ?? await _invoiceRepository.GetByIdAsync(creditNote.OriginalInvoiceId);

                var dto = await MapToDtoAsync(creditNote, originalInvoice);
                result.Add(dto);
            }

            return result;
        }

        public async Task<CreditNoteDto?> GetCreditNoteByIdAsync(string id)
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(CreditNoteId.FromGuid(Guid.Parse(id)));
            if (creditNote == null) return null;

            Invoice? originalInvoice = null;
            if (creditNote.IsFromInvoice && creditNote.OriginalInvoiceId != null)
                originalInvoice = creditNote.OriginalInvoice
                    ?? await _invoiceRepository.GetByIdAsync(creditNote.OriginalInvoiceId);

            return await MapToDtoAsync(creditNote, originalInvoice);
        }

        public async Task<bool> DeleteCreditNoteAsync(string id, string deletedBy)
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(CreditNoteId.FromGuid(Guid.Parse(id)));
            if (creditNote == null) return false;

            await _creditNoteRepository.DeleteAsync(creditNote.Id);
            return true;
        }

        public async Task<CreditNoteDocumentDataDto?> GetCreditNoteDocumentDataAsync(string id, string? template, bool signed, CancellationToken cancellationToken = default)
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(CreditNoteId.FromGuid(Guid.Parse(id)));
            if (creditNote == null) return null;

            EmployeeEntity? employee = null;
            Domain.Companies.Company? company = null;
            string? purchaseOrderReference = null;
            Invoice? originalInvoice = null;

            if (creditNote.IsFromInvoice && creditNote.OriginalInvoiceId != null)
            {
                originalInvoice = creditNote.OriginalInvoice
                    ?? await _invoiceRepository.GetByIdAsync(creditNote.OriginalInvoiceId);

                if (originalInvoice != null)
                {
                    if (!string.IsNullOrEmpty(originalInvoice.RessourceId))
                        employee = await _employeeRepository.GetByIdAsync(new EmployeeId(Guid.Parse(originalInvoice.RessourceId)));

                    if (!string.IsNullOrEmpty(originalInvoice.SocieteId))
                        company = await _companyRepository.GetByIdAsync(new CompanyId(Guid.Parse(originalInvoice.SocieteId)));

                    if (!string.IsNullOrEmpty(originalInvoice.RessourceId) && !string.IsNullOrEmpty(originalInvoice.SocieteId))
                    {
                        var allPurchaseOrders = await _purchaseOrderRepository.GetAllAsync();
                        var bonCmd = allPurchaseOrders.FirstOrDefault(p =>
                            p.EmployeeId != null &&
                            ((Guid)p.EmployeeId).ToString() == originalInvoice.RessourceId &&
                            ((Guid)p.CompanyId).ToString() == originalInvoice.SocieteId &&
                            p.EngagementMode == EngagementMode.AT
                        );
                        purchaseOrderReference = bonCmd?.Reference;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(creditNote.RessourceId))
                    employee = await _employeeRepository.GetByIdAsync(new EmployeeId(Guid.Parse(creditNote.RessourceId)));

                if (!string.IsNullOrEmpty(creditNote.SocieteId))
                    company = await _companyRepository.GetByIdAsync(new CompanyId(Guid.Parse(creditNote.SocieteId)));
            }

            var isMoroccan = company?.Pays?.ToLower().Contains("maroc") == true;
            var currency = isMoroccan ? "DH" : (creditNote.Currency ?? "€");

            var items = new List<InvoicePdfDataItemDto>();

            if (creditNote.IsFromInvoice && originalInvoice != null)
            {
                var isWP = originalInvoice.Type == InvoiceType.InvoiceWP;

                if (isWP && originalInvoice.WPItems != null)
                {
                    items = originalInvoice.WPItems.Select(i => new InvoicePdfDataItemDto(
                        i.Designation,
                        creditNote.Quantity,
                        i.PrixUnitaire,
                        creditNote.Quantity * i.PrixUnitaire,
                        employee?.FullName,
                        null,
                        i.ReferenceBC
                    )).ToList();
                }
                else
                {
                    var dailyRate = employee != null ? (decimal)(employee.DailyRate ?? 0) : 0;
                    items.Add(new InvoicePdfDataItemDto(
                        Description: creditNote.Designation,
                        Quantity: creditNote.Quantity,
                        UnitPrice: dailyRate,
                        Total: creditNote.Quantity * dailyRate,
                        ResourceName: employee?.FullName,
                        BillingMonthLabel: originalInvoice.MoisFacturation > 0
                            ? originalInvoice.MoisFacturation.ToString("D2") + "/" + originalInvoice.DateFacturation.Year
                            : null,
                        PurchaseOrderReference: purchaseOrderReference
                    ));
                }
            }
            else
            {
                items.Add(new InvoicePdfDataItemDto(
                    Description: creditNote.Designation,
                    Quantity: creditNote.Quantity,
                    UnitPrice: creditNote.UnitPrice,
                    Total: creditNote.Quantity * creditNote.UnitPrice,
                    ResourceName: employee?.FullName,
                    BillingMonthLabel: creditNote.BillingMonth > 0
                        ? creditNote.BillingMonth.ToString("D2") + "/" + creditNote.InvoiceDate.Year
                        : null,
                    PurchaseOrderReference: null
                ));
            }

            var ht = items.Sum(i => i.Total);
            decimal tva;
            if (creditNote.IsFromInvoice)
            {
                var isWP = originalInvoice?.Type == InvoiceType.InvoiceWP;
                tva = isMoroccan && !isWP ? ht * 0.20m : 0;
            }
            else
                tva = creditNote.VATRate > 0 ? ht * (creditNote.VATRate / 100m) : 0;
            var ttc = ht + tva;
            var amountForWords = tva > 0 ? Math.Abs(ttc) : Math.Abs(ht);

            return new CreditNoteDocumentDataDto(
                Reference: creditNote.Reference,
                OriginalInvoiceReference: originalInvoice?.Reference ?? creditNote.Reference,
                FileName: $"AVOIR-{creditNote.Reference}",
                Template: template ?? "in-ctech",
                ClientName: company?.Nom ?? creditNote.CompanyName ?? "-",
                ClientAddress: company?.Adresse ?? creditNote.Address ?? "-",
                ClientCode: company?.Code ?? creditNote.CompanyCode ?? "-",
                ClientCountry: company?.Pays ?? (creditNote.Currency == "DH" ? "Maroc" : "France"),
                InvoiceDate: creditNote.InvoiceDate,
                BCNumber: purchaseOrderReference,
                Ht: ht,
                Tva: tva,
                Ttc: ttc,
                Currency: currency,
                AmountInWords: "",
                Signed: signed,
                Items: items
            );
        }

        public async Task<CreditNoteDto?> UpdateCreditNoteAsync(string id, UpdateCreditNoteDto dto, string updatedBy)
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(CreditNoteId.FromGuid(Guid.Parse(id)));
            if (creditNote == null) return null;

            if (dto.Quantity >= 0)
                throw new ArgumentException("La quantité doit être négative pour un avoir.");

            creditNote.Update(
                designation: dto.Designation ?? creditNote.Designation,
                unitPrice: dto.UnitPrice ?? creditNote.UnitPrice,
                quantity: dto.Quantity,
                vatRate: dto.VATRate ?? creditNote.VATRate,
                invoiceDate: dto.InvoiceDate ?? creditNote.InvoiceDate,
                billingMonth: dto.BillingMonth ?? creditNote.BillingMonth,
                currency: dto.Currency,
                address: dto.Address,
                companyCode: dto.CompanyCode,
                companyName: dto.CompanyName
            );

            creditNote.UpdatedBy = updatedBy;
            creditNote.UpdatedAt = DateTime.UtcNow;

            await _creditNoteRepository.UpdateAsync(creditNote);

            var originalInvoice = creditNote.IsFromInvoice
                ? (creditNote.OriginalInvoice ?? await _invoiceRepository.GetByIdAsync(creditNote.OriginalInvoiceId!))
                : null;

            return await MapToDtoAsync(creditNote, originalInvoice);
        }
        private async Task<CreditNoteDto> MapToDtoAsync(CreditNote creditNote, Invoice? originalInvoice)
        {
            EmployeeEntity? employee = null;
            Domain.Companies.Company? company = null;
            string? purchaseOrderReference = null;

            string? societeId;
            string? ressourceId;
            decimal unitPrice;
            decimal vatRate;
            DateTime invoiceDate;
            int billingMonth;

            if (creditNote.IsFromInvoice && originalInvoice != null)
            {
                societeId = originalInvoice.SocieteId;
                ressourceId = originalInvoice.RessourceId;
                vatRate = originalInvoice.TauxTVA;
                invoiceDate = originalInvoice.DateFacturation;
                billingMonth = originalInvoice.MoisFacturation;

                if (originalInvoice.Type == InvoiceType.InvoiceWP)
                {
                    unitPrice = (decimal)(originalInvoice.WPItems?.FirstOrDefault()?.PrixUnitaire ?? 0);
                }
                else
                {
                    var emp = !string.IsNullOrEmpty(ressourceId)
                        ? await _employeeRepository.GetByIdAsync(new EmployeeId(Guid.Parse(ressourceId)))
                        : null;
                    unitPrice = (decimal)(emp?.DailyRate ?? 0);
                }
            }
            else
            {
                societeId = creditNote.SocieteId;
                ressourceId = creditNote.RessourceId;
                unitPrice = creditNote.UnitPrice;
                vatRate = creditNote.VATRate;
                invoiceDate = creditNote.InvoiceDate;
                billingMonth = creditNote.BillingMonth;
            }

            if (!string.IsNullOrEmpty(ressourceId))
                employee = await _employeeRepository.GetByIdAsync(new EmployeeId(Guid.Parse(ressourceId)));

            if (!string.IsNullOrEmpty(societeId))
                company = await _companyRepository.GetByIdAsync(new CompanyId(Guid.Parse(societeId)));

            if (!string.IsNullOrEmpty(ressourceId) && !string.IsNullOrEmpty(societeId))
            {
                var allPurchaseOrders = await _purchaseOrderRepository.GetAllAsync();
                var bonCmd = allPurchaseOrders.FirstOrDefault(p =>
                    p.EmployeeId != null &&
                    ((Guid)p.EmployeeId).ToString() == ressourceId &&
                    ((Guid)p.CompanyId).ToString() == societeId &&
                    p.EngagementMode == EngagementMode.AT
                );
                purchaseOrderReference = bonCmd?.Reference;
            }

            var isMoroccan = company?.Pays?.ToLower().Contains("maroc") == true;
            var currency = creditNote.IsFromInvoice
                ? (isMoroccan ? "DH" : "€")
                : (creditNote.Currency ?? "€");

            return new CreditNoteDto(
                Id: creditNote.Id.Value.ToString(),
                Reference: creditNote.Reference,
                IsFromInvoice: creditNote.IsFromInvoice,
                OriginalInvoiceReference: originalInvoice?.Reference,
                OriginalInvoiceType: originalInvoice?.Type.ToString(),
                Designation: creditNote.IsFromInvoice && originalInvoice != null
                    ? $"Avoir sur facture : {originalInvoice.Reference}"
                    : creditNote.Designation,
                UnitPrice: unitPrice,
                Quantity: creditNote.Quantity,
                VATRate: vatRate,
                InvoiceDate: invoiceDate,
                BillingMonth: billingMonth,
                PurchaseOrderReference: purchaseOrderReference,
                EmployeeFullName: employee?.FullName,
                EmployeeTrigram: employee?.Trigram,
                CompanyName: creditNote.IsFromInvoice ? company?.Nom : (company?.Nom ?? creditNote.CompanyName),
                CompanyAddress: company?.Adresse,
                CompanyCountry: company?.Pays,
                Currency: currency,
                Status: creditNote.Status.ToString(),
                Address: creditNote.IsFromInvoice ? null : creditNote.Address,
                CompanyCode: creditNote.IsFromInvoice ? company?.Code : creditNote.CompanyCode,
                CreatedBy: creditNote.CreatedBy,
                CreatedAt: creditNote.CreatedAt,
                UpdatedBy: creditNote.UpdatedBy,
                UpdatedAt: creditNote.UpdatedAt,
                DeletedBy: creditNote.DeletedBy,
                DeletedAt: creditNote.DeletedAt
            );
        }

        private async Task<string> GenerateReferenceAsync()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"IN-C-{year}-";

            var invoices = await _invoiceRepository.GetAllAsync();
            var lastInvoiceNumber = invoices
                .Where(f => f.Reference != null && f.Reference.StartsWith(prefix))
                .Select(f => {
                    var lastPart = f.Reference.Split('-').Last();
                    return int.TryParse(lastPart, out int n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            var lastAvoirRef = await _creditNoteRepository.GetLastReferenceAsync(year);
            int lastAvoirNumber = 0;
            if (lastAvoirRef != null)
            {
                var lastPart = lastAvoirRef.Split('-').Last();
                int.TryParse(lastPart, out lastAvoirNumber);
            }

            var nextNumber = Math.Max(lastInvoiceNumber, lastAvoirNumber) + 1;
            return $"IN-C-{year}-{nextNumber:D4}";
        }

        public async Task<CreditNoteDto?> UpdateCreditNoteStatusAsync(string id, string status, string updatedBy)
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(CreditNoteId.FromGuid(Guid.Parse(id)));
            if (creditNote == null) return null;

            if (!Enum.TryParse<CreditNoteStatus>(status, out var creditNoteStatus))
                throw new ArgumentException($"Statut invalide : {status}");

            creditNote.UpdateStatus(creditNoteStatus);
            creditNote.UpdatedBy = updatedBy;
            creditNote.UpdatedAt = DateTime.UtcNow;

            await _creditNoteRepository.UpdateAsync(creditNote);

            var originalInvoice = creditNote.IsFromInvoice && creditNote.OriginalInvoiceId != null
                ? creditNote.OriginalInvoice ?? await _invoiceRepository.GetByIdAsync(creditNote.OriginalInvoiceId)
                : null;

            return await MapToDtoAsync(creditNote, originalInvoice);
        }
    }
}
