using in_ctech_management_backend.Domain.Common;
using in_ctech_management_backend.Domain.Employees.Enums;
using in_ctech_management_backend.Domain.Jobs;
using in_ctech_management_backend.Domain.Projects;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;
using System.Net.Mail;

namespace in_ctech_management_backend.Domain.Employees
{
    public class Employee : AuditableEntity
    {
        public EmployeeId Id { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string FullName { get; private set; }
        public string Trigram { get; private set; }
        public string NationalId { get; private set; }
        public string Cnss { get; private set; }
        public double? DailyRate { get; private set; }
        public string PhoneNumber { get; private set; }
        public string? Email { get; private set; }
        public string? BankAccountNumber { get; private set; }
        public string RegistrationNumber { get; private set; }
        public ContractType ContractType { get; private set; }
        public PaymentMethod PaymentMethod { get; private set; }
        public FreelancerType? FreelancerType { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? ContractEndDate { get; private set; }
        public JobId JobId { get; private set; }

        // ==================== Leave tracking ====================
        public decimal? MonthlyLeaveAllowance { get; private set; } // Saisi à la création uniquement pour PERMANENT, FIXED_TERM, ANAPEC
        public decimal? LeaveBalance { get; private set; } // Solde de congé courant, cumulé chaque 1er du mois par le cron job, uniquement pour PERMANENT, FIXED_TERM, ANAPEC
        public decimal AnnualConsumedLeaves { get; private set; } // Reset à 0 chaque 1er janvier par le cron job
        public DateTime? LastBalanceUpdateAt { get; private set; } // Garantit l'idempotence du cron job (pas de double crédit dans le même mois)
        // ========================================================

        // ==================== Purchase price tracking ====================
        public decimal? PurchasePrice { get; private set; }
        public Currency? PurchasePriceCurrency { get; private set; }
        // =================================================================

        private readonly List<Project> _projects = new();
        public IReadOnlyList<Project> Projects => _projects.AsReadOnly();
        public EmployeeId? ManagerId { get; private set; }
        public bool IsActive { get; private set; } = true;

        private Employee() { }

        private Employee(
            string firstName,
            string lastName,
            string trigram,
            string cnss,
            string nationalId,
            string phoneNumber,
            string? email,
            string? bankAccountNumber,
            string registrationNumber,
            PaymentMethod paymentMethod,
            ContractType contractType,
            FreelancerType? freelancerType,
            DateTime? startDate,
            DateTime? contractEndDate,
            JobId jobId,
            EmployeeId? managerId,
            decimal? monthlyLeaveAllowance,
            decimal? leaveBalance,
            decimal? annualConsumedLeaves)
        {
            FirstName = ValidateRequired(firstName, nameof(firstName));
            LastName = ValidateRequired(lastName, nameof(lastName));
            FullName = BuildFullName(FirstName, LastName);
            Trigram = ValidateRequired(trigram, nameof(trigram));
            Cnss = ValidateRequired(cnss, nameof(cnss));
            NationalId = ValidateRequired(nationalId, nameof(nationalId));
            PhoneNumber = ValidateRequired(phoneNumber, nameof(phoneNumber));
            Email = ValidateEmail(email);
            BankAccountNumber = NormalizeOptional(bankAccountNumber);
            RegistrationNumber = ValidateRequired(registrationNumber, nameof(registrationNumber));
            PaymentMethod = paymentMethod;
            ContractType = contractType;
            FreelancerType = freelancerType;
            StartDate = startDate;
            ContractEndDate = contractEndDate;
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            DailyRate = null;
            ManagerId = managerId;

            // Leave tracking initialization
            MonthlyLeaveAllowance = ValidateMonthlyLeaveAllowance(contractType, monthlyLeaveAllowance);
            LeaveBalance = IsLeaveEligible(contractType)
                ? ValidateLeaveBalance(leaveBalance) ?? 0m
                : null;
            AnnualConsumedLeaves = ValidateAnnualConsumedLeaves(annualConsumedLeaves) ?? 0m;
            LastBalanceUpdateAt = null;

            Id = new EmployeeId(Guid.NewGuid());
        }

        public static Employee Create(
            string firstName,
            string lastName,
            string trigram,
            string cnss,
            string nationalId,
            string phoneNumber,
            string? email,
            string? bankAccountNumber,
            string registrationNumber,
            PaymentMethod paymentMethod,
            ContractType contractType,
            FreelancerType? freelancerType,
            DateTime? startDate,
            DateTime? contractEndDate,
            JobId jobId,
            EmployeeId? managerId,
            decimal? monthlyLeaveAllowance,
            decimal? leaveBalance = null,
            decimal? annualConsumedLeaves = null)
        {
            return new Employee(
                firstName,
                lastName,
                trigram,
                cnss,
                nationalId,
                phoneNumber,
                email,
                bankAccountNumber,
                registrationNumber,
                paymentMethod,
                contractType,
                freelancerType,
                startDate,
                contractEndDate,
                jobId,
                managerId,
                monthlyLeaveAllowance,
                leaveBalance,
                annualConsumedLeaves);
        }

        public void Update(
            string firstName,
            string lastName,
            string trigram,
            string cnss,
            string nationalId,
            string phoneNumber,
            string? email,
            string? bankAccountNumber,
            PaymentMethod paymentMethod,
            ContractType contractType,
            FreelancerType? freelancerType,
            DateTime? startDate,
            DateTime? contractEndDate,
            JobId jobId,
            EmployeeId? managerId,
            decimal? monthlyLeaveAllowance)
        {
            FirstName = ValidateRequired(firstName, nameof(firstName));
            LastName = ValidateRequired(lastName, nameof(lastName));
            FullName = BuildFullName(FirstName, LastName);
            Trigram = ValidateRequired(trigram, nameof(trigram));
            Cnss = ValidateRequired(cnss, nameof(cnss));
            NationalId = ValidateRequired(nationalId, nameof(nationalId));
            PhoneNumber = ValidateRequired(phoneNumber, nameof(phoneNumber));
            Email = ValidateEmail(email);
            BankAccountNumber = NormalizeOptional(bankAccountNumber);
            PaymentMethod = paymentMethod;

            // Si le type de contrat change vers un type non éligible, on remet le solde à null
            // Si le type de contrat change vers un type éligible, on conserve l'ancien solde ou on initialise à 0
            var contractTypeChanged = ContractType != contractType;
            ContractType = contractType;
            FreelancerType = freelancerType;
            StartDate = startDate;
            ContractEndDate = contractEndDate;
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            ManagerId = managerId;

            MonthlyLeaveAllowance = ValidateMonthlyLeaveAllowance(contractType, monthlyLeaveAllowance);
            if (contractTypeChanged)
            {
                LeaveBalance = IsLeaveEligible(contractType) ? (LeaveBalance ?? 0m) : null;
            }
        }

        public void AssignProjects(List<Domain.Projects.Project> projects)
        {
            _projects.Clear();
            _projects.AddRange(projects);
        }

        public void Deactivate()
        {
            if (!IsActive)
                throw new InvalidOperationException("Employee is already inactive.");

            IsActive = false;
        }

        public void Activate()
        {
            if (IsActive)
                throw new InvalidOperationException("Employee is already active.");

            IsActive = true;
        }

        public void UpdateDailyRate(double? dailyRate)
        {
            DailyRate = dailyRate.HasValue ? ValidateDailyRate(dailyRate.Value) : null;
        }

        public void UpdatePurchasePrice(decimal? purchasePrice, Currency? currency)
        {
            if (purchasePrice.HasValue)
            {
                ValidatePurchasePrice(purchasePrice.Value);
                if (!currency.HasValue)
                    throw new ArgumentException("Currency is required when purchase price is set.", nameof(currency));
            }

            PurchasePrice = purchasePrice;
            PurchasePriceCurrency = purchasePrice.HasValue ? currency : null;
        }

        // ==================== Leave tracking methods ====================

        // Crédite le solde mensuel si l'employé est éligible et qu'il n'a pas déjà été crédité ce mois-ci.
        // Appelée par le cron job mensuel (1er du mois).
        // Retourne true si le crédit a été appliqué, false sinon.
        public bool AccrueMonthlyLeave(DateTime today)
        {
            if (!IsLeaveEligible(ContractType))
                return false;

            if (!MonthlyLeaveAllowance.HasValue || !StartDate.HasValue)
                return false;

            // L'employé doit avoir au moins 1 jour travaillé dans le mois précédent.
            var firstDayOfCurrentMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            if (StartDate.Value >= firstDayOfCurrentMonth)
                return false;

            // Idempotence : si on a déjà crédité ce mois-ci, on ne re-crédite pas
            if (LastBalanceUpdateAt.HasValue &&
                LastBalanceUpdateAt.Value.Year == today.Year &&
                LastBalanceUpdateAt.Value.Month == today.Month)
            {
                return false;
            }

            LeaveBalance = (LeaveBalance ?? 0m) + MonthlyLeaveAllowance.Value;
            LastBalanceUpdateAt = today;
            return true;
        }

        // Met à jour manuellement le solde de congé et / ou le total annuel consommé.
        // Champs optionnels : null = pas de changement.
        public void UpdateLeaveTracking(decimal? leaveBalance, decimal? annualConsumedLeaves)
        {
            if (leaveBalance.HasValue)
            {
                if (!IsLeaveEligible(ContractType))
                    throw new ArgumentException(
                        "leaveBalance cannot be set for ineligible contract types.",
                        nameof(leaveBalance));

                LeaveBalance = ValidateLeaveBalance(leaveBalance);
            }

            if (annualConsumedLeaves.HasValue)
                AnnualConsumedLeaves = ValidateAnnualConsumedLeaves(annualConsumedLeaves) ?? 0m;
        }

        // Reset le compteur annuel des congés consommés, appelée par le cron job annuel (1er janvier).
        public void ResetAnnualConsumedLeaves()
        {
            AnnualConsumedLeaves = 0m;
        }

        // Incrémente le compteur de congés consommés et déduit du solde de congé. Appelée lors de la validation d'une demande de congé.
        public void AddConsumedLeaves(decimal numberOfDays)
        {
            if (numberOfDays <= 0m)
                throw new ArgumentException("numberOfDays must be greater than zero.", nameof(numberOfDays));

            AnnualConsumedLeaves += numberOfDays;

            if (LeaveBalance.HasValue)
                LeaveBalance = LeaveBalance.Value - numberOfDays;
        }

        // Ajuste le compteur consommé et le solde de congé suite à un recalcul d'une demande approuvée
        // (ex: jour férié ajouté/supprimé qui change NumberOfDays). delta > 0 = créditer (jours rendus),
        // delta < 0 = redébiter.
        public void AdjustConsumedLeaves(decimal delta)
        {
            if (delta == 0m)
                return;

            var newConsumed = AnnualConsumedLeaves - delta;
            if (newConsumed < 0m)
                newConsumed = 0m;
            AnnualConsumedLeaves = newConsumed;

            if (LeaveBalance.HasValue)
                LeaveBalance = LeaveBalance.Value + delta;
        }

        public static bool IsLeaveEligible(ContractType contractType)
        {
            return contractType == ContractType.PERMANENT
                || contractType == ContractType.FIXED_TERM
                || contractType == ContractType.ANAPEC;
        }

        // ==================== Validations ====================

        private static string ValidateRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{paramName} cannot be empty.", paramName);
            }

            return value.Trim();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static double ValidateDailyRate(double dailyRate)
        {
            if (dailyRate < 0)
            {
                throw new ArgumentException("dailyRate cannot be negative.", nameof(dailyRate));
            }

            return dailyRate;
        }

        private static string? ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalizedEmail = email.Trim();

            try
            {
                var mailAddress = new MailAddress(normalizedEmail);
                return mailAddress.Address;
            }
            catch
            {
                throw new ArgumentException("Invalid email format.", nameof(email));
            }
        }

        private static decimal? ValidateLeaveBalance(decimal? value)
        {
            if (!value.HasValue)
                return null;

            if (value.Value < 0)
                throw new ArgumentException("leaveBalance cannot be negative.", nameof(value));

            return value.Value;
        }

        private static decimal? ValidateAnnualConsumedLeaves(decimal? value)
        {
            if (!value.HasValue)
                return null;

            if (value.Value < 0)
                throw new ArgumentException("annualConsumedLeaves cannot be negative.", nameof(value));

            return value.Value;
        }

        private static decimal? ValidateMonthlyLeaveAllowance(ContractType contractType, decimal? value)
        {
            if (!IsLeaveEligible(contractType))
                return null;

            if (!value.HasValue)
                throw new ArgumentException("monthlyLeaveAllowance is required for PERMANENT, FIXED_TERM and ANAPEC contracts.", nameof(value));

            if (value.Value < 0)
                throw new ArgumentException("monthlyLeaveAllowance cannot be negative.", nameof(value));

            return value.Value;
        }

        private static void ValidatePurchasePrice(decimal value)
        {
            if (value < 0)
                throw new ArgumentException("Purchase price cannot be negative.", nameof(value));
        }

        private static string BuildFullName(string firstName, string lastName)
        {
            return $"{firstName} {lastName}".Trim();
        }
    }
}