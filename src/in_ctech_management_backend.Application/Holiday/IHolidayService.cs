using in_ctech_management_backend.Application.Holiday.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace in_ctech_management_backend.Application.Holiday
{
    public interface IHolidayService
    {
        Task<Guid> CreateAsync(CreateHolidayRequest dto, CancellationToken cancellationToken = default);
        Task<HolidayDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<HolidayDto>> GetAllAsync(string? title = null, DateOnly? date = null, CancellationToken cancellationToken = default);
        Task UpdateAsync(Guid id, UpdateHolidayRequest dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
