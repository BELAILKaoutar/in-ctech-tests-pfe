namespace in_ctech_management_backend.Domain.Equipment
{
    public sealed record EquipmentId(Guid Value)
    {
        public static explicit operator Guid(EquipmentId equipmentId) => equipmentId.Value;
    }
}
