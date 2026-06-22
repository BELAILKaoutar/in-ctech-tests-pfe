using in_ctech_management_backend.Application.Equipment;
using in_ctech_management_backend.Application.Equipment.DTOs;
using in_ctech_management_backend.Domain;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Equipment.Repositories;

namespace in_ctech_management_backend.Application.Equipment
{
    public class EquipmentService : IEquipmentService
    {
        private readonly IEquipmentRepository _equipmentRepository;

        public EquipmentService(IEquipmentRepository equipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
        }

        public async Task<EquipmentDto> CreateAsync(CreateEquipmentDto dto, CancellationToken cancellationToken = default)
        {
            var existingEquipment = await _equipmentRepository.GetByReferenceAsync(dto.Reference, cancellationToken);

            if (existingEquipment is not null)
                throw new DomainException("An equipment with the same reference already exists");

            var equipment = Domain.Equipment.Equipment.Create(dto.Name, dto.Reference);

            await _equipmentRepository.AddAsync(equipment, cancellationToken);
            await _equipmentRepository.SaveChangesAsync(cancellationToken);

            return MapToDto(equipment);
        }

        public async Task<EquipmentDto?> GetByIdAsync(Guid equipmentId, CancellationToken cancellationToken = default)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(new Domain.Equipment.EquipmentId(equipmentId), cancellationToken);

            return equipment is null ? null : MapToDto(equipment);
        }

        public async Task<IReadOnlyList<EquipmentDto>> GetAllAsync(
            string? name = null,
            string? reference = null,
            CancellationToken cancellationToken = default)
        {
            var equipments = await _equipmentRepository.GetAllAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(name))
            {
                equipments = equipments
                    .Where(e => e.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(reference))
            {
                equipments = equipments
                    .Where(e => e.Reference.Contains(reference, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return equipments.Select(MapToDto).ToList();
        }
        public async Task<EquipmentDto> UpdateAsync(Guid equipmentId, UpdateEquipmentDto dto, CancellationToken cancellationToken = default)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(new Domain.Equipment.EquipmentId(equipmentId), cancellationToken);

            if (equipment is null)
                throw new DomainException("Equipment not found");

            var existingEquipment = await _equipmentRepository.GetByReferenceAsync(dto.Reference, cancellationToken);

            if (existingEquipment is not null && existingEquipment.EquipmentId.Value != equipmentId)
                throw new DomainException("An equipment with the same reference already exists");

            equipment.Update(dto.Name, dto.Reference);

            await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
            await _equipmentRepository.SaveChangesAsync(cancellationToken);

            return MapToDto(equipment);
        }

        public async Task AssignToEmployeeAsync(Guid equipmentId, AssignEquipmentDto dto, CancellationToken cancellationToken = default)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(new Domain.Equipment.EquipmentId(equipmentId), cancellationToken);

            if (equipment is null)
                throw new DomainException("Equipment not found");

            equipment.AssignToEmployee(new EmployeeId(dto.EmployeeId));

            await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
            await _equipmentRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task ReturnFromEmployeeAsync(Guid equipmentId, CancellationToken cancellationToken = default)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(new Domain.Equipment.EquipmentId(equipmentId), cancellationToken);

            if (equipment is null)
                throw new DomainException("Equipment not found");

            equipment.ReturnFromEmployee();

            await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
            await _equipmentRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid equipmentId, CancellationToken cancellationToken = default)
        {
            var equipment = await _equipmentRepository.GetByIdAsync(new Domain.Equipment.EquipmentId(equipmentId), cancellationToken);

            if (equipment is null)
                throw new DomainException("Equipment not found");

            await _equipmentRepository.DeleteAsync(equipment, cancellationToken);
            await _equipmentRepository.SaveChangesAsync(cancellationToken);
        }

        private static EquipmentDto MapToDto(Domain.Equipment.Equipment equipment)
        {
            return new EquipmentDto
            {
                EquipmentId = equipment.EquipmentId.Value,
                Name = equipment.Name,
                Reference = equipment.Reference,
                CurrentEmployeeId = equipment.CurrentEmployeeId?.Value,
                CreatedAt = equipment.CreatedAt,
                CreatedBy = equipment.CreatedBy,
                UpdatedAt = equipment.UpdatedAt,
                UpdatedBy = equipment.UpdatedBy,
                History = equipment.History.Select(h => new EquipmentAssignmentDto
                {
                    EmployeeId = h.EmployeeId.Value,
                    AssignmentDate = h.AssignmentDate,
                    ReturnDate = h.ReturnDate
                }).ToList()
            };
        }
    }
}