namespace in_ctech_management_backend.Application.Equipment.DTOs
{
    public class EquipmentDto
    {
        public Guid EquipmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public Guid? CurrentEmployeeId { get; set; }
        public List<EquipmentAssignmentDto> History { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}