namespace in_ctech_management_backend.Domain.PurchaseOrders
{
    public sealed record PurchaseOrderId(Guid Value)
    {
        public static explicit operator Guid(PurchaseOrderId purchaseOrderId) => purchaseOrderId.Value;
    }
}
