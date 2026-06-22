using in_ctech_management_backend.Domain.Employees;

namespace in_ctech_management_backend.Domain.Equipment
{
    public class EquipmentAssignment
    {
        public EmployeeId EmployeeId { get; private set; }
        public DateTime AssignmentDate { get; private set; }
        public DateTime? ReturnDate { get; private set; }

        private EquipmentAssignment()
        {
            EmployeeId = default!;
        }

        private EquipmentAssignment(EmployeeId employeeId, DateTime assignmentDate)
        {
            EmployeeId = employeeId;
            AssignmentDate = assignmentDate;
            ReturnDate = null;
        }

        public static EquipmentAssignment Create(EmployeeId employeeId, DateTime assignmentDate)
        {
            if (employeeId is null)
                throw new DomainException("EmployeeId is required");

            if (assignmentDate == default)
                throw new DomainException("Assignment date is required");

            return new EquipmentAssignment(employeeId, assignmentDate);
        }

        public void MarkAsReturned(DateTime returnDate)
        {
            if (ReturnDate.HasValue)
                throw new DomainException("This assignment has already been returned");

            if (returnDate < AssignmentDate)
                throw new DomainException("Return date cannot be earlier than assignment date");

            ReturnDate = returnDate;
        }
    }
}