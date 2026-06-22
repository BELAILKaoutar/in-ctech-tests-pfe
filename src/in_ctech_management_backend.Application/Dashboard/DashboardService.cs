using in_ctech_management_backend.Application.Dashboard.DTOs;
using in_ctech_management_backend.Application.Shared;
using in_ctech_management_backend.Domain.Companies.Repositories;
using in_ctech_management_backend.Domain.Employees.Enums;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;
using in_ctech_management_backend.Domain.PurchaseOrders.Repositories;

namespace in_ctech_management_backend.Application.Dashboard
{
    public class DashboardService : IDashboardService
    {
        // Tous les montants du dashboard sont normalisés en MAD (voir CurrencyConverter).

        private readonly IEmployeeRepository _employeeRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ICompanyRepository _companyRepository;

        public DashboardService(
            IEmployeeRepository employeeRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            ICompanyRepository companyRepository)
        {
            _employeeRepository = employeeRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _companyRepository = companyRepository;
        }

        public async Task<List<MonthlyMarginDto>> GetMonthlyMarginsAsync(int year, CancellationToken cancellationToken = default)
        {
            var employees = await _employeeRepository.GetAllAsync(null, null, null, isActive: true, cancellationToken);
            var activePurchaseOrders = await _purchaseOrderRepository.GetByStatusAsync(PurchaseOrderStatus.ACTIVE, cancellationToken);
            var companies = await _companyRepository.GetAllAsync(null, null, cancellationToken);
            var companiesById = companies.ToDictionary(c => c.CompanyId);

            var result = new List<MonthlyMarginDto>();

            for (int month = 1; month <= 12; month++)
            {
                var periodStart = new DateOnly(year, month, 1);
                var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                decimal totalPurchasePrice = 0;
                decimal totalBCValue = 0;

                foreach (var po in activePurchaseOrders)
                {
                    if (po.EmployeeId is null)
                        continue;

                    // Check if the PO covers this month
                    if (po.StartDate > periodEnd || po.EndDate < periodStart)
                        continue;

                    var employee = employees.FirstOrDefault(e => e.Id == po.EmployeeId);
                    if (employee is null || !employee.PurchasePrice.HasValue)
                        continue;

                    // Prix d'achat : converti depuis sa propre devise vers MAD.
                    var purchaseCurrency = employee.PurchasePriceCurrency ?? Currency.MAD;
                    totalPurchasePrice += CurrencyConverter.ToMad(employee.PurchasePrice.Value, purchaseCurrency);

                    // BC : la devise dépend du pays du client (France => EUR, sinon MAD).
                    companiesById.TryGetValue(po.CompanyId, out var company);
                    var bcCurrency = CurrencyConverter.GetBcCurrency(company);
                    totalBCValue += CurrencyConverter.ToMad((decimal)po.TotalAmount, bcCurrency);
                }

                result.Add(new MonthlyMarginDto(
                    year,
                    month,
                    totalPurchasePrice,
                    (double)totalBCValue,
                    totalBCValue - totalPurchasePrice
                ));
            }

            return result;
        }
    }
}
