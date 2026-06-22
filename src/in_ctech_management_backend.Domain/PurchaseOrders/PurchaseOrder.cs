using in_ctech_management_backend.Domain.PurchaseOrders.Enums;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Companies;
using in_ctech_management_backend.Domain.Common;

namespace in_ctech_management_backend.Domain.PurchaseOrders
{
    public class PurchaseOrder : AuditableEntity
    {
        public PurchaseOrderId PurchaseOrderId { get; private set; }
        public DocType DocType { get; private set; }
        public string Reference { get; private set; }
        public EngagementMode EngagementMode { get; private set; }
        public PaymentMethod PaymentMode { get; private set; }
        public PurchaseOrderStatus PurchaseOrderStatus { get; private set; }
        public string Description { get; private set; }
        public DateOnly StartDate { get; private set; }
        public DateOnly EndDate { get; private set; }

        // Relation avec Company (Client)
        public CompanyId CompanyId { get; private set; }

        // Champs AT
        public EmployeeId? EmployeeId { get; private set; }
        public double? DailyRate { get; private set; }

        // Champs WP
        public string? Designation { get; private set; }
        public double? Quantity { get; private set; }
        public double? UnitPrice { get; private set; }

        // Montant total calculé automatiquement
        public double TotalAmount { get; private set; }

        private PurchaseOrder()
        {
            PurchaseOrderId = new PurchaseOrderId(Guid.NewGuid());
            Reference = string.Empty;
            Description = string.Empty;
            CreatedBy = string.Empty;
        }

        // Constructeur AT
        private PurchaseOrder(
            string reference,
            DocType docType,
            CompanyId companyId,
            DateOnly startDate,
            DateOnly endDate,
            PaymentMethod paymentMode,
            string description,
            EmployeeId? employeeId,
            double dailyRate,
            double quantity,
            double totalAmount,
            string createdBy)
        {
            PurchaseOrderId = new PurchaseOrderId(Guid.NewGuid());
            Reference = reference;
            DocType = docType;
            EngagementMode = EngagementMode.AT;
            CompanyId = companyId;
            StartDate = startDate;
            EndDate = endDate;
            PaymentMode = paymentMode;
            Description = description;
            EmployeeId = employeeId;
            DailyRate = dailyRate;
            Quantity = quantity;
            TotalAmount = totalAmount;
            PurchaseOrderStatus = PurchaseOrderStatus.ACTIVE;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy;
        }

        // Constructeur WP
        private PurchaseOrder(
            string reference,
            DocType docType,
            CompanyId companyId,
            DateOnly startDate,
            DateOnly endDate,
            PaymentMethod paymentMode,
            string description,
            string designation,
            double quantity,
            double unitPrice,
            double totalAmount,
            string createdBy)
        {
            PurchaseOrderId = new PurchaseOrderId(Guid.NewGuid());
            Reference = reference;
            DocType = docType;
            EngagementMode = EngagementMode.WP;
            CompanyId = companyId;
            StartDate = startDate;
            EndDate = endDate;
            PaymentMode = paymentMode;
            Description = description;
            Designation = designation;
            Quantity = quantity;
            UnitPrice = unitPrice;
            TotalAmount = totalAmount;
            PurchaseOrderStatus = PurchaseOrderStatus.ACTIVE;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy;
        }

        // Factory method AT
        public static PurchaseOrder CreateAT(
           string reference,
           DocType docType,
           CompanyId companyId,
           DateOnly startDate,
           DateOnly endDate,
           PaymentMethod paymentMode,
           string description,
           EmployeeId? employeeId,
           double dailyRate,
           double? quantity,
           double? totalAmount,
           string createdBy)
        {
            ValidateReference(reference);
            ValidateDates(startDate, endDate);
            ValidateDailyRate(dailyRate);
            var amounts = CalculateQuantityAndTotalAmount(quantity, totalAmount, dailyRate);
            return new PurchaseOrder(reference, docType, companyId, startDate, endDate, paymentMode, description, employeeId, dailyRate, amounts.Quantity, amounts.TotalAmount, createdBy);
        }

        // Factory method WP
        public static PurchaseOrder CreateWP(
            string reference,
            DocType docType,
            CompanyId companyId,
            DateOnly startDate,
            DateOnly endDate,
            PaymentMethod paymentMode,
            string description,
            string designation,
            double? quantity,
            double unitPrice,
            double? totalAmount,
            string createdBy)
        {
            ValidateReference(reference);
            ValidateDates(startDate, endDate);
            ValidateDesignation(designation);
            ValidateUnitPrice(unitPrice);
            var amounts = CalculateQuantityAndTotalAmount(quantity, totalAmount, unitPrice);
            return new PurchaseOrder(reference, docType, companyId, startDate, endDate, paymentMode, description, designation, amounts.Quantity, unitPrice, amounts.TotalAmount, createdBy);
        }

        public void UpdateAT(
            EmployeeId? newEmployeeId,
            CompanyId newCompanyId,
            DateOnly newStartDate,
            DateOnly newEndDate,
            EngagementMode newEngagementMode,
            PaymentMethod newPaymentMode,
            double newDailyRate,
            double? newQuantity,
            double? newTotalAmount,
            string updatedBy)
        {
            ValidateDates(newStartDate, newEndDate);
            ValidateDailyRate(newDailyRate);
            var amounts = CalculateQuantityAndTotalAmount(newQuantity, newTotalAmount, newDailyRate);

            EmployeeId = newEmployeeId;
            CompanyId = newCompanyId;
            StartDate = newStartDate;
            EndDate = newEndDate;
            EngagementMode = newEngagementMode;
            PaymentMode = newPaymentMode;
            DailyRate = newDailyRate;
            Quantity = amounts.Quantity;
            TotalAmount = amounts.TotalAmount;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }


        public void UpdateWP(
            string newReference,
            string newDesignation,
            CompanyId newCompanyId,
            DateOnly newStartDate,
            DateOnly newEndDate,
            EngagementMode newEngagementMode,
            PaymentMethod newPaymentMode,
            double? newQuantity,
            double newUnitPrice,
            double? newTotalAmount,
            string updatedBy)
        {
            ValidateDates(newStartDate, newEndDate);
            ValidateDesignation(newDesignation);
            ValidateUnitPrice(newUnitPrice);
            var amounts = CalculateQuantityAndTotalAmount(newQuantity, newTotalAmount, newUnitPrice);

            Reference = newReference;
            Designation = newDesignation;
            CompanyId = newCompanyId;
            StartDate = newStartDate;
            EndDate = newEndDate;
            EngagementMode = newEngagementMode;
            PaymentMode = newPaymentMode;
            Quantity = amounts.Quantity;
            UnitPrice = newUnitPrice;
            TotalAmount = amounts.TotalAmount;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }


        // Changer le statut (actif/inactif)
        public void ChangeStatus(PurchaseOrderStatus newStatus, string updatedBy)
        {
            if (PurchaseOrderStatus == newStatus)
                throw new DomainException($"PurchaseOrder is already in status {newStatus}");
            PurchaseOrderStatus = newStatus;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        // Méthodes de validation
        private static void ValidateReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new DomainException("Purchase Order reference is required");
            if (reference.Length > 50)
                throw new DomainException("Purchase Order reference cannot exceed 50 characters");
        }

        private static void ValidateDates(DateOnly startDate, DateOnly endDate)
        {
            if (endDate <= startDate)
                throw new DomainException("End date must be after start date");
        }

        private static void ValidateDailyRate(double dailyRate)
        {
            if (dailyRate <= 0)
                throw new DomainException("Daily rate must be greater than zero");
        }
        private static void ValidateTotalAmount(double totalAmount)
        {
            if (totalAmount <= 0)
                throw new DomainException("Total amount must be greater than zero");
        }
        private static void ValidateDesignation(string designation)
        {
            if (string.IsNullOrWhiteSpace(designation))
                throw new DomainException("Designation is required for WP purchase order");
        }

        private static void ValidateUnitPrice(double unitPrice)
        {
            if (unitPrice <= 0)
                throw new DomainException("Unit price must be greater than zero");
        }

        private static (double Quantity, double TotalAmount) CalculateQuantityAndTotalAmount(
            double? quantity,
            double? totalAmount,
            double unitAmount)
        {
            if (!quantity.HasValue && !totalAmount.HasValue)
                throw new DomainException("Quantity or TotalAmount is required");

            if (quantity.HasValue && quantity.Value <= 0)
                throw new DomainException("Quantity must be greater than zero");

            if (totalAmount.HasValue)
                ValidateTotalAmount(totalAmount.Value);

            var calculatedQuantity = quantity ?? totalAmount!.Value / unitAmount;
            var calculatedTotalAmount = totalAmount ?? quantity!.Value * unitAmount;

            return (calculatedQuantity, calculatedTotalAmount);
        }
    }
}
