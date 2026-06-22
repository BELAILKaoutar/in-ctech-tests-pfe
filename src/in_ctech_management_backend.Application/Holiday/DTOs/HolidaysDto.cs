using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.Holiday.DTOs
{
    public record HolidayDto(
        Guid Id,
        string Title,
        DateOnly Date,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? CreatedBy,
        string? UpdatedBy
    );
}
