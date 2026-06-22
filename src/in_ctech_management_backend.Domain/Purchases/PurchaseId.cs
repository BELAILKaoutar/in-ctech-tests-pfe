namespace in_ctech_management_backend.Domain.Purchases
{
    public sealed record PurchaseId(Guid Value)
    {
        public static explicit operator Guid(PurchaseId purchaseId) => purchaseId.Value;
    }
}
