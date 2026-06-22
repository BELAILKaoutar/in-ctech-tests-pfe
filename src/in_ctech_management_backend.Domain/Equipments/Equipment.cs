using in_ctech_management_backend.Domain.Common;
using in_ctech_management_backend.Domain.Employees;

namespace in_ctech_management_backend.Domain.Equipment
{
    public class Equipment : AuditableEntity
    {
        private readonly List<EquipmentAssignment> _history = new();

        public EquipmentId EquipmentId { get; private set; }
        public string Name { get; private set; }
        public string Reference { get; private set; }
        public EmployeeId? CurrentEmployeeId { get; private set; }
        public IReadOnlyCollection<EquipmentAssignment> History => _history.AsReadOnly();

        private Equipment()
        {
            EquipmentId = new EquipmentId(Guid.NewGuid());
            Name = string.Empty;
            Reference = string.Empty;
        }

        private Equipment(
            string name,
            string reference)
        {
            EquipmentId = new EquipmentId(Guid.NewGuid());
            Name = name;
            Reference = reference;
            CurrentEmployeeId = null;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = null;
        }

        public static Equipment Create(
            string name,
            string reference)
        {
            ValidateName(name);
            ValidateReference(reference);

            return new Equipment(name, reference);
        }

        public void Update(string name, string reference)
        {
            ValidateName(name);
            ValidateReference(reference);

            Name = name;
            Reference = reference;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AssignToEmployee(EmployeeId employeeId)
        {
            if (employeeId is null)
                throw new DomainException("EmployeeId is required");


            if (CurrentEmployeeId is not null)
                throw new DomainException("Equipment is already assigned to an employee");

            CurrentEmployeeId = employeeId;
            _history.Add(EquipmentAssignment.Create(employeeId, DateTime.UtcNow));
            UpdatedAt = DateTime.UtcNow;
        }

        public void ReturnFromEmployee()
        {

            if (CurrentEmployeeId is null)
                throw new DomainException("Equipment is not assigned to any employee");

            var currentAssignment = _history.LastOrDefault(x => !x.ReturnDate.HasValue);

            if (currentAssignment is null)
                throw new DomainException("No active assignment found for this equipment");

            currentAssignment.MarkAsReturned(DateTime.UtcNow);
            CurrentEmployeeId = null;
            UpdatedAt = DateTime.UtcNow;
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Equipment name is required");

            if (name.Length > 100)
                throw new DomainException("Equipment name cannot exceed 100 characters");
        }

        private static void ValidateReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new DomainException("Equipment reference is required");

            if (reference.Length > 50)
                throw new DomainException("Equipment reference cannot exceed 50 characters");
        }


    }
}