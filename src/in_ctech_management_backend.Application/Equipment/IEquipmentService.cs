using in_ctech_management_backend.Application.Equipment.DTOs;

namespace in_ctech_management_backend.Application.Equipment
{
    public interface IEquipmentService
    {
        Task<EquipmentDto> CreateAsync(CreateEquipmentDto dto, CancellationToken cancellationToken = default);
        Task<EquipmentDto?> GetByIdAsync(Guid equipmentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EquipmentDto>> GetAllAsync(string? name = null,string? reference = null,CancellationToken cancellationToken = default); 
        Task<EquipmentDto> UpdateAsync(Guid equipmentId, UpdateEquipmentDto dto, CancellationToken cancellationToken = default);
        Task AssignToEmployeeAsync(Guid equipmentId, AssignEquipmentDto dto, CancellationToken cancellationToken = default);
        Task ReturnFromEmployeeAsync(Guid equipmentId, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    }
}