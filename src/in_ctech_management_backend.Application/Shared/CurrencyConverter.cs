using in_ctech_management_backend.Domain.Employees.Enums;

namespace in_ctech_management_backend.Application.Shared
{
    /// <summary>
    /// Conversion des montants vers le MAD, devise pivot des indicateurs financiers
    /// (dashboard, indicateurs employé). Centralise le taux et les règles de devise
    /// pour éviter toute divergence entre les services.
    /// </summary>
    public static class CurrencyConverter
    {
        // Taux de conversion provisoire EUR -> MAD.
        public const decimal EurToMadRate = 10.65m;

        // Convertit un montant vers le MAD (devise pivot).
        public static decimal ToMad(decimal amount, Currency currency)
            => currency == Currency.EUR ? amount * EurToMadRate : amount;

        // Devise d'un BC déduite du pays du client : "maroc" => MAD, sinon EUR.
        public static Currency GetBcCurrency(Domain.Companies.Company? company)
            => company?.Pays?.ToLower().Contains("maroc") == true ? Currency.MAD : Currency.EUR;
    }
}
