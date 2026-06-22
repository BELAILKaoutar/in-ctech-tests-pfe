using in_ctech_management_backend.Application.Company.DTOs;
using in_ctech_management_backend.Application.PuchaseOrder.DTOs;
using in_ctech_management_backend.Application.PurchaseOrder.DTOs;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;
using DomainCompany = in_ctech_management_backend.Domain.Companies.Company;
using DomainEmployee = in_ctech_management_backend.Domain.Employees.Employee;
using DomainPurchaseOrder = in_ctech_management_backend.Domain.PurchaseOrders.PurchaseOrder;

namespace in_ctech_management_backend.Application.PurchaseOrder
{
    public static class PurchaseOrderMapper
    {
        public static PurchaseOrderDto ToDto(
            DomainPurchaseOrder po,
            CompanyResponseDto? company,
            EmployeeSummaryDto? employee,
            decimal consumedAmount,
            decimal remainingAmount,
            decimal remainingPercentage,
            int daysRemaining,
            PurchaseOrderMarginDto? margin)
        {
            return new PurchaseOrderDto(
                po.PurchaseOrderId.Value,
                po.DocType.ToString(),
                po.Reference,
                po.EngagementMode.ToString(),
                po.PaymentMode.ToString(),
                po.PurchaseOrderStatus.ToString(),
                po.Description,
                po.TotalAmount,
                consumedAmount,
                remainingAmount,
                remainingPercentage,
                daysRemaining,
                po.StartDate,
                po.EndDate,
                po.CreatedAt,
                po.UpdatedAt,
                po.CreatedBy,
                po.UpdatedBy,
                po.DailyRate,
                po.Designation,
                po.Quantity,
                po.UnitPrice,
                company,
                employee,
                margin
            );
        }

        public static PurchaseOrderDocumentDataDto ToDocumentDataDto(
            DomainPurchaseOrder po,
            string template,
            DomainCompany? company,
            DomainEmployee? employee)
        {
            var companyData = company == null
                ? null
                : new PurchaseOrderDocumentPartyDto(
                    company.CompanyId.Value.ToString(),
                    company.Nom,
                    company.Adresse,
                    company.Code,
                    company.Pays);

            var employeeData = employee == null
                ? null
                : new PurchaseOrderDocumentEmployeeDto(
                    employee.Id.Value,
                    employee.FullName);

            var isPurchase = po.DocType == DocType.PURCHASE;

            return new PurchaseOrderDocumentDataDto(
                po.PurchaseOrderId.Value,
                po.Reference,
                BuildDocumentFileName(po.Reference, template),
                template,
                po.CreatedAt,
                po.DocType.ToString(),
                po.EngagementMode.ToString(),
                po.PaymentMode.ToString(),
                po.StartDate,
                po.EndDate,
                po.Description,
                po.TotalAmount,
                po.DailyRate,
                po.Designation,
                po.Quantity,
                po.UnitPrice,
                isPurchase ? null : companyData,
                isPurchase ? companyData : null,
                employeeData);
        }

        private static string BuildDocumentFileName(string reference, string template)
        {
            if (string.Equals(template, "winity", StringComparison.OrdinalIgnoreCase))
                return $"{reference}-winity";

            return reference;
        }
    }
}
