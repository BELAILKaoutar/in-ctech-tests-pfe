namespace in_ctech_management_backend.Application.PuchaseOrder.DTOs
{
    /// <summary>
    /// Marge réalisée sur un BC en mode AT : écart entre le tarif journalier facturé
    /// au client et le prix d'achat de la ressource (Employee).
    /// Tous les montants sont normalisés en MAD (devise pivot, voir CurrencyConverter).
    /// Les devises d'origine sont conservées pour traçabilité :
    /// - le tarif journalier est dans la devise du client (Maroc => MAD, sinon EUR),
    /// - le prix d'achat est dans sa propre devise (MAD ou EUR).
    /// </summary>
    public record PurchaseOrderMarginDto(
        decimal DailyRateMad,
        string DailyRateCurrency,
        decimal PurchasePriceMad,
        string PurchasePriceCurrency,
        decimal MarginValue
    );
}
