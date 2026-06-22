using in_ctech_management_backend.Application.Dashboard.DTOs;

namespace in_ctech_management_backend.Application.Dashboard
{
    public interface IDashboardService
    {
        Task<List<MonthlyMarginDto>> GetMonthlyMarginsAsync(int year, CancellationToken cancellationToken = default);
    }
}
