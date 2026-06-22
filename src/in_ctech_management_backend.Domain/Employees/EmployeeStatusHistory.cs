using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Domain.Employees
{
    public class EmployeeStatusHistory
    {
        public int Id { get; private set; }
        public EmployeeId EmployeeId { get; private set; }
        public Employee Employee { get; private set; } = null!;
        public bool OldStatus { get; private set; }
        public bool NewStatus { get; private set; }
        public DateTime ChangedAt { get; private set; }
        public EmployeeId? ChangedByEmployeeId { get; private set; }

        private EmployeeStatusHistory() { }

        public static EmployeeStatusHistory Create(
            EmployeeId employeeId,
            bool oldStatus,
            bool newStatus,
            EmployeeId? changedByEmployeeId)
        {
            return new EmployeeStatusHistory
            {
                EmployeeId = employeeId ?? throw new ArgumentNullException(nameof(employeeId)),
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByEmployeeId = changedByEmployeeId
            };
        }
    }
}