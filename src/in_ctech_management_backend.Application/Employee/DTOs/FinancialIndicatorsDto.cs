using in_ctech_management_backend.Domain.Employees.Enums;

namespace in_ctech_management_backend.Application.Employee.DTOs
{
    public record FinancialIndicatorsDto(
        decimal PurchasePrice,
        Currency PurchasePriceCurrency,
        decimal PurchasePriceMAD,
        double DailyRate,
        Currency DailyRateCurrency,
        decimal DailyRateMAD,
        decimal MarginValue
    );
}
