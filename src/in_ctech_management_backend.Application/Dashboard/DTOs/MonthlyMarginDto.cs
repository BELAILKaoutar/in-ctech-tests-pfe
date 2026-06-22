namespace in_ctech_management_backend.Application.Dashboard.DTOs
{
    public record MonthlyMarginDto(
        int Year,
        int Month,
        decimal TotalPurchasePrice,
        double TotalBCValue,
        decimal Margin
    );
}
