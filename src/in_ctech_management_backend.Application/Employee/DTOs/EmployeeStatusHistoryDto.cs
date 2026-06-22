using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.Employee.DTOs
{
    public record EmployeeStatusHistoryDto(
        int Id,
        bool OldStatus,
        bool NewStatus,
        DateTime ChangedAt,
        string? ChangedByFullName
    );
}
