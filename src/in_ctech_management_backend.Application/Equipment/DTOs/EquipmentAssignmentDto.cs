namespace in_ctech_management_backend.Application.Equipment.DTOs
{
    public class EquipmentAssignmentDto
    {
        public Guid EmployeeId { get; set; }
        public DateTime AssignmentDate { get; set; }
        public DateTime? ReturnDate { get; set; }
    }
}