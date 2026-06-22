using in_ctech_management_backend.Domain.Invoices.Enums;
using System;
using System.Collections.Generic;

namespace in_ctech_management_backend.Domain.Invoices
{
    public class Invoice
    {
        public InvoiceId Id { get; private set; }
        public string Reference { get; private set; }
        public string? RessourceId { get; set; }
        public string SocieteId { get; set; }
        public string? ProjectId { get; set; }
        public string? PurchaseOrderReference { get; set; }
        public Guid? DeliveryNoteId { get; set; }
        public decimal TotalHT { get; set; }
        public decimal TauxTVA { get; set; }
        public DateTime DateFacturation { get; set; }
        public int MoisFacturation { get; set; }
        public int BillingYear { get; set; }
        public int Quantity { get; set; }
        public InvoiceType Type { get; set; }
        public List<InvoiceWPItem>? WPItems { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? TransferReference { get; set; }
        public string CreatedBy { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        private Invoice() { }

        public Invoice(string reference, string? ressourceId, string societeId, string? projectId,
                       decimal totalHT, decimal tauxTVA, DateTime dateFacturation,
                       int moisFacturation, int billingYear, InvoiceType type, string createdBy, int quantity = 0,
                       List<InvoiceWPItem>? wpItems = null, string? purchaseOrderReference = null, Guid? deliveryNoteId = null)
        {
            Id = InvoiceId.FromGuid(Guid.NewGuid());
            Reference = reference;
            RessourceId = ressourceId;
            SocieteId = societeId;
            ProjectId = projectId;
            PurchaseOrderReference = purchaseOrderReference;
            DeliveryNoteId = deliveryNoteId;
            TotalHT = totalHT;
            TauxTVA = tauxTVA;
            DateFacturation = dateFacturation;
            MoisFacturation = moisFacturation;
            BillingYear = billingYear;
            Quantity = quantity;
            Type = type;
            Status = InvoiceStatus.Envoyee;
            CreatedBy = createdBy;
            CreatedAt = DateTime.UtcNow;

            if (type == InvoiceType.InvoiceWP)
            {
                WPItems = wpItems ?? new List<InvoiceWPItem>();
            }
        }
    }

    public class InvoiceWPItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ReferenceBC { get; set; }
        public string Designation { get; set; }
        public decimal PrixUnitaire { get; set; }
        public int Quantite { get; set; }
    }
}
