using in_ctech_management_backend.Domain.Common;
using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.DeliveryNotes.Enums;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.PurchaseOrders;
namespace in_ctech_management_backend.Domain.DeliveryNotes
{
    public class DeliveryNote : AuditableEntity
    {
        public DeliveryNoteId DeliveryNoteId { get; private set; }
        public string Reference { get; private set; }
        public DeliveryNoteType Type { get; private set; }

        // Relation avec Societe (Client)
        public CompanyId CompanyId { get; private set; }

        // Champs AT
        public EmployeeId? EmployeeId { get; private set; }
        public int? Month { get; private set; }
        public int? Year { get; private set; }

        // Champs WP
        public string? Designation { get; private set; }
        public double? UnitPrice { get; private set; }
        public PurchaseOrderId? PurchaseOrderId { get; private set; }

        // Champs communs
        public double Quantity { get; private set; }
        public double Amount { get; private set; }
        public DateTime InvoiceDate { get; private set; }

        // Constructeur EF
        private DeliveryNote()
        {
            DeliveryNoteId = new DeliveryNoteId(Guid.NewGuid());
            Reference = string.Empty;
        }

        // Constructeur AT
        private DeliveryNote(
            string reference,
            CompanyId companyId,
            EmployeeId employeeId,
            int month,
            int year,
            double quantity,
            double amount,
            DateTime invoiceDate,
            string createdBy)
        {
            DeliveryNoteId = new DeliveryNoteId(Guid.NewGuid());
            Reference = reference;
            Type = DeliveryNoteType.AT;
            CompanyId = companyId;
            EmployeeId = employeeId;
            Month = month;
            Year = year;
            Quantity = quantity;
            Amount = amount;
            InvoiceDate = NormalizeInvoiceDate(invoiceDate);
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy;
        }

        // Constructeur WP
        private DeliveryNote(
            string reference,
            CompanyId societeId,
            string designation,
            double quantity,
            double unitPrice,
            PurchaseOrderId? purchaseOrderId,
            DateTime invoiceDate,
            string createdBy)
        {
            DeliveryNoteId = new DeliveryNoteId(Guid.NewGuid());
            Reference = reference;
            Type = DeliveryNoteType.WP;
            CompanyId = societeId;
            Designation = designation;
            Quantity = quantity;
            UnitPrice = unitPrice;
            Amount = quantity * unitPrice;
            PurchaseOrderId = purchaseOrderId;
            InvoiceDate = NormalizeInvoiceDate(invoiceDate);
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy;
        }

        // ==================== FACTORY METHODS ====================

        public static DeliveryNote CreateAT(
            string reference,
            CompanyId companyId,
            EmployeeId employeeId,
            int month,
            int year,
            double quantity,
            double amount,
            DateTime invoiceDate,
            string createdBy)
        {
            ValidateReference(reference);
            ValidateMonth(month);
            ValidateYear(year);
            ValidateQuantity(quantity);
            ValidateAmount(amount);
            return new DeliveryNote(reference, companyId, employeeId, month, year, quantity, amount, invoiceDate, createdBy);
        }

        public static DeliveryNote CreateWP(
            string reference,
            CompanyId companyId,
            string designation,
            double quantity,
            double unitPrice,
            PurchaseOrderId? purchaseOrderId,
            DateTime invoiceDate,
            string createdBy)
        {
            ValidateReference(reference);
            ValidateDesignation(designation);
            ValidateQuantity(quantity);
            ValidateUnitPrice(unitPrice);
            return new DeliveryNote(reference, companyId, designation, quantity, unitPrice, purchaseOrderId, invoiceDate, createdBy);
        }

        // ==================== UPDATE METHODS ====================

        public void UpdateAT(
            CompanyId newCompanyId,
            EmployeeId newEmployeeId,
            int newMonth,
            int newYear,
            double newQuantity,
            double newAmount,
            DateTime newInvoiceDate,
            string updatedBy)
        {
            ValidateMonth(newMonth);
            ValidateYear(newYear);
            ValidateQuantity(newQuantity);
            ValidateAmount(newAmount);

            CompanyId = newCompanyId;
            EmployeeId = newEmployeeId;
            Month = newMonth;
            Year = newYear;
            Quantity = newQuantity;
            Amount = newAmount;
            InvoiceDate = NormalizeInvoiceDate(newInvoiceDate);
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public void UpdateWP(
            CompanyId newCompanyId,
            string newDesignation,
            double newQuantity,
            double newUnitPrice,
            DateTime newInvoiceDate,
            string updatedBy)
        {
            ValidateDesignation(newDesignation);
            ValidateQuantity(newQuantity);
            ValidateUnitPrice(newUnitPrice);

            CompanyId = newCompanyId;
            Designation = newDesignation;
            Quantity = newQuantity;
            UnitPrice = newUnitPrice;
            Amount = newQuantity * newUnitPrice;
            InvoiceDate = NormalizeInvoiceDate(newInvoiceDate);
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        // ==================== VALIDATIONS ====================

        private static void ValidateReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new DomainException("Delivery Note reference is required");
            if (reference.Length > 50)
                throw new DomainException("Delivery Note reference cannot exceed 50 characters");
        }

        private static void ValidateMonth(int month)
        {
            if (month < 1 || month > 12)
                throw new DomainException("Month must be between 1 and 12");
        }

        private static void ValidateYear(int year)
        {
            if (year < 2000)
                throw new DomainException("Year must be at least 2000");
        }

        private static void ValidateQuantity(double quantity)
        {
            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than zero");
        }

        private static void ValidateAmount(double amount)
        {
            if (amount <= 0)
                throw new DomainException("Amount must be greater than zero");
        }

        private static void ValidateUnitPrice(double unitPrice)
        {
            if (unitPrice <= 0)
                throw new DomainException("Unit price must be greater than zero");
        }

        private static void ValidateDesignation(string designation)
        {
            if (string.IsNullOrWhiteSpace(designation))
                throw new DomainException("Designation is required for WP delivery note");
        }

        private static DateTime NormalizeInvoiceDate(DateTime invoiceDate)
        {
            return invoiceDate.Kind switch
            {
                DateTimeKind.Utc => invoiceDate,
                DateTimeKind.Local => invoiceDate.ToUniversalTime(),
                _ => DateTime.SpecifyKind(invoiceDate, DateTimeKind.Utc)
            };
        }
    }
}
