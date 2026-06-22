using in_ctech_management_backend.Application.Authentication;
using in_ctech_management_backend.Application.Employee.DTOs;
using in_ctech_management_backend.Application.Shared;
using in_ctech_management_backend.Domain;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Enums;
using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.Companies.Repositories;
using in_ctech_management_backend.Domain.Jobs;
using in_ctech_management_backend.Domain.Jobs.Repositories;
using in_ctech_management_backend.Domain.Projects;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;
using in_ctech_management_backend.Domain.PurchaseOrders.Repositories;
using Microsoft.Extensions.Logging;

namespace in_ctech_management_backend.Application.Employee
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IJobRepository _jobRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IAuthenticationService _authenticationService;
        private readonly IEmailService _emailService;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(IEmployeeRepository employeeRepository,
            IJobRepository jobRepository,
            IProjectRepository projectRepository,
            IAuthenticationService authenticationService,
            IEmailService emailService,
            IPurchaseOrderRepository purchaseOrderRepository,
            ICompanyRepository companyRepository,
            ILogger<EmployeeService> logger)
        {
            _employeeRepository = employeeRepository;
            _jobRepository = jobRepository;
            _projectRepository = projectRepository;
            _authenticationService = authenticationService;
            _emailService = emailService;
            _purchaseOrderRepository = purchaseOrderRepository;
            _companyRepository = companyRepository;
            _logger = logger;
        }

        // ==================== CREATE ====================

        public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default)
        {
            // Firstname and Lastname uniques
            var exists = await _employeeRepository.ExistsByFirstNameAndLastNameAsync(
                dto.FirstName.Trim(),
                dto.LastName.Trim(),
                cancellationToken);
            if (exists)
                throw new DomainException("Un employé portant le même prénom et le même nom de famille existe déjà.");

            // Email unique
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                // Employee side verification (table employee)
                var emailExists = await _employeeRepository.ExistsByEmailAsync(dto.Email.Trim(), cancellationToken);
                if (emailExists)
                    throw new DomainException("Un employé avec cet email existe déjà.");

                // Auth side verification (User Identity)
                var userExists = await _authenticationService.UserExistsByEmailAsync(dto.Email.Trim(), cancellationToken);
                if (userExists)
                    throw new DomainException("Un compte avec cet email existe déjà.");
            }

            // Job
            var employeejob = await _jobRepository.GetByIdAsync(new JobId(dto.JobId), cancellationToken);
            if (employeejob is null)
                throw new DomainException($"Job with id '{dto.JobId}' not found");

            // Manager
            // If it's not a manager, a manager must be assigned to him
            bool isManager = employeejob.Title.Equals("MANAGER", StringComparison.OrdinalIgnoreCase);
            if (!isManager && !dto.ManagerId.HasValue)
                throw new DomainException("Un manager doit être assigné pour cet employé");
            if (dto.ManagerId.HasValue)
            {
                var manager = await _employeeRepository.GetByIdAsync(new EmployeeId(dto.ManagerId.Value), cancellationToken);
                if (manager == null)
                    throw new DomainException($"Manager with id '{dto.ManagerId}' not found");
            }

            // Projects
            if (dto.ProjectIds == null || dto.ProjectIds.Count == 0)
                throw new DomainException("Un employé doit être assigné à au moins un projet.");
            var projects = new List<Project>();
            foreach (var projectId in dto.ProjectIds)
            {
                var project = await _projectRepository.GetByIdAsync(new ProjectId(projectId), cancellationToken);
                if (project == null)
                    throw new DomainException($"Project with id '{projectId}' not found");
                projects.Add(project);
            }

            // Leave allowance : obligatoire pour PERMANENT, FIXED_TERM, ANAPEC et null pour FREELANCE et INTERNSHIP
            ValidateLeaveAllowanceForContractType(dto.ContractType, dto.MonthlyLeaveAllowance);

            var isPermanent = dto.ContractType == ContractType.PERMANENT;
            var lastRegistrationNumber = await _employeeRepository.GetLastRegistrationNumberAsync(isPermanent, cancellationToken);
            var nextRegistrationNumber = GenerateNextRegistrationNumber(lastRegistrationNumber, isPermanent);
            var employee = Domain.Employees.Employee.Create(
                dto.FirstName.Trim(),
                dto.LastName.Trim(),
                dto.Trigram.Trim(),
                dto.Cnss.Trim(),
                dto.NationalId.Trim(),
                dto.PhoneNumber.Trim(),
                dto.Email?.Trim(),
                dto.BankAccountNumber?.Trim(),
                nextRegistrationNumber,
                dto.PaymentMethod,
                dto.ContractType,
                dto.FreelancerType,
                dto.StartDate,
                dto.ContractEndDate,
                new JobId(dto.JobId),
                dto.ManagerId.HasValue ? new EmployeeId(dto.ManagerId.Value) : null,
                dto.MonthlyLeaveAllowance,
                dto.LeaveBalance,
                dto.AnnualConsumedLeaves
            );
            employee.UpdatePurchasePrice(dto.PurchasePrice, dto.PurchasePriceCurrency);
            employee.AssignProjects(projects);

            var temporaryPassword = GenerateTemporaryPassword();

            // Employee persistence (avant l'envoi de l'email pour éviter d'envoyer un mail en cas d'échec DB)
            await _employeeRepository.AddAsync(employee, cancellationToken);
            await _employeeRepository.SaveChangesAsync(cancellationToken);

            var userCreated = false;

            // User Identity creation
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                try
                {
                    await _authenticationService.CreateEmployeeUserAsync(
                        dto.Email.Trim(),
                        temporaryPassword,
                        employee.Id.Value,
                        dto.FirstName.Trim(),
                        dto.LastName.Trim(),
                        cancellationToken);

                    userCreated = true;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(
                        ex,
                        "Employee {EmployeeId} was created, but identity user creation failed for email {Email}.",
                        employee.Id.Value,
                        dto.Email.Trim());
                }
            }

            // Send welcome email only when the identity account has been created.
            if (userCreated && !string.IsNullOrWhiteSpace(dto.Email))
            {
                try
                {
                    await _emailService.SendEmployeeWelcomeEmailAsync(
                        dto.Email.Trim(),
                        $"{dto.FirstName.Trim()} {dto.LastName.Trim()}",
                        temporaryPassword);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(
                        ex,
                        "Employee {EmployeeId} and identity user were created, but welcome email failed for {Email}.",
                        employee.Id.Value,
                        dto.Email.Trim());
                }
            }

            return await MapToDtoAsync(employee, cancellationToken);
        }


        // ==================== READ ====================

        public async Task<EmployeeDto?> GetByIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken);

            return employee is null ? null : await MapToDtoAsync(employee, cancellationToken);
        }
        public async Task<IReadOnlyList<EmployeeDto>> GetAllAsync(
            string? fullName = null,
            string? contractType = null,
            Guid? projectId = null,
            Guid? managerId = null,
            bool isActive = true,
            CancellationToken cancellationToken = default)
        {
            ContractType? parsedContractType = null;

            if (!string.IsNullOrWhiteSpace(contractType) &&
                Enum.TryParse<ContractType>(contractType, true, out var parsed))
                parsedContractType = parsed;

            var employees = await _employeeRepository.GetAllAsync(
                fullName,
                parsedContractType,
                projectId.HasValue ? new ProjectId(projectId.Value) : null,
                isActive,
                cancellationToken);

            if (managerId.HasValue)
            {
                employees = employees
                    .Where(e => e.ManagerId?.Value == managerId.Value)
                    .ToList();
            }


            var result = new List<EmployeeDto>();
            foreach (var employee in employees)
                result.Add(await MapToDtoAsync(employee, cancellationToken));

            return result;
        }

        public async Task<IReadOnlyList<EmployeeDto>> GetAllForFilterAsync(CancellationToken cancellationToken = default)
        {
            var employees = await _employeeRepository.GetAllWithoutStatusFilterAsync(cancellationToken);
            var result = new List<EmployeeDto>();
            foreach (var employee in employees)
                result.Add(await MapToDtoAsync(employee, cancellationToken));
            return result;
        }

        public async Task<List<ManagerSummaryDto>> GetManagersAsync(CancellationToken cancellationToken = default)
        {
            var managers = await _employeeRepository.GetManagersAsync(cancellationToken);
            return managers.Select(m => new ManagerSummaryDto(
                m.Id.Value,
                m.FullName,
                m.Email ?? ""
            )).ToList();
        }

        public async Task<List<EmployeeStatusHistoryDto>> GetStatusHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var history = await _employeeRepository.GetStatusHistoryByEmployeeIdAsync(
                new EmployeeId(employeeId), cancellationToken);

            var result = new List<EmployeeStatusHistoryDto>();
            foreach (var h in history)
            {
                string? changedByFullName = null;
                if (h.ChangedByEmployeeId != null)
                {
                    var changedBy = await _employeeRepository.GetByIdAsync(h.ChangedByEmployeeId, cancellationToken);
                    changedByFullName = changedBy?.FullName;
                }

                result.Add(new EmployeeStatusHistoryDto(
                    h.Id,
                    h.OldStatus,
                    h.NewStatus,
                    h.ChangedAt,
                    changedByFullName
                ));
            }
            return result;
        }

        // ==================== UPDATE ====================

        public async Task<EmployeeDto> UpdateAsync(Guid employeeId, UpdateEmployeeDto dto, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken);

            if (employee is null)
                throw new DomainException("Employee not found.");

            if (dto.ManagerId.HasValue)
            {
                var manager = await _employeeRepository.GetByIdAsync(new EmployeeId(dto.ManagerId.Value), cancellationToken);
                if (manager == null)
                    throw new DomainException($"Manager with id '{dto.ManagerId}' not found");
            }

            // Leave allowance : même règle qu'à la création
            ValidateLeaveAllowanceForContractType(dto.ContractType, dto.MonthlyLeaveAllowance);

            employee.Update(
                dto.FirstName.Trim(),
                dto.LastName.Trim(),
                dto.Trigram.Trim(),
                dto.Cnss.Trim(),
                dto.NationalId.Trim(),
                dto.PhoneNumber.Trim(),
                dto.Email?.Trim(),
                dto.BankAccountNumber?.Trim(),
                dto.PaymentMethod,
                dto.ContractType,
                dto.FreelancerType,
                dto.StartDate,
                dto.ContractEndDate,
                new JobId(dto.JobId),
                dto.ManagerId.HasValue ? new EmployeeId(dto.ManagerId.Value) : null,
                dto.MonthlyLeaveAllowance
            );

            employee.UpdateLeaveTracking(dto.LeaveBalance, dto.AnnualConsumedLeaves);
            employee.UpdatePurchasePrice(dto.PurchasePrice, dto.PurchasePriceCurrency);

            if (dto.ProjectIds != null)
            {
                var projects = new List<Domain.Projects.Project>();
                foreach (var projectId in dto.ProjectIds)
                {
                    var project = await _projectRepository.GetByIdAsync(new ProjectId(projectId), cancellationToken);
                    if (project == null)
                        throw new DomainException($"Project with id '{projectId}' not found");
                    projects.Add(project);
                }
                employee.AssignProjects(projects);
            }
            await _employeeRepository.UpdateAsync(employee, cancellationToken);
            await _employeeRepository.SaveChangesAsync(cancellationToken);
            return await MapToDtoAsync(employee, cancellationToken);
        }

        public async Task<EmployeeDto> UpdateDailyRateAsync(Guid employeeId, double? dailyRate, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken);

            if (employee is null)
                throw new DomainException($"Employee with id '{employeeId}' not found.");

            employee.UpdateDailyRate(dailyRate);

            await _employeeRepository.UpdateAsync(employee, cancellationToken);

            return await MapToDtoAsync(employee, cancellationToken);
        }

        public async Task<EmployeeDto> ActivateAsync(Guid employeeId, Guid? changedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken);
            if (employee is null)
                throw new DomainException($"Employee with id '{employeeId}' not found.");

            await _employeeRepository.SetActiveStatusAsync(
                new EmployeeId(employeeId),
                true,
                changedByEmployeeId.HasValue ? new EmployeeId(changedByEmployeeId.Value) : null,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(employee.Email))
                await _authenticationService.EnableUserAsync(employee.Email, cancellationToken);

            await _employeeRepository.SaveChangesAsync(cancellationToken);

            var updated = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken);
            return await MapToDtoAsync(updated!, cancellationToken);
        }

        public async Task<EmployeeDto> DeactivateAsync(Guid employeeId, Guid? changedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken);
            if (employee is null)
                throw new DomainException($"Employee with id '{employeeId}' not found.");

            await _employeeRepository.SetActiveStatusAsync(
                new EmployeeId(employeeId),
                false,
                changedByEmployeeId.HasValue ? new EmployeeId(changedByEmployeeId.Value) : null,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(employee.Email))
                await _authenticationService.DisableUserAsync(employee.Email, cancellationToken);

            await _employeeRepository.SaveChangesAsync(cancellationToken);

            var updated = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken);
            return await MapToDtoAsync(updated!, cancellationToken);
        }

        // ==================== DELETE ====================

        public async Task DeleteAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken);

            if (employee is null)
                throw new DomainException($"Employee with id '{employeeId}' not found.");

            var employeeEmail = employee.Email;

            await _employeeRepository.DeleteAsync(employee, cancellationToken);
            await _employeeRepository.SaveChangesAsync(cancellationToken);

            try
            {
                await _authenticationService.DeleteEmployeeUserAsync(
                    employeeId,
                    employeeEmail,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Employee {EmployeeId} was deleted, but identity user deletion failed.",
                    employeeId);
            }
        }

        // ==================== FINANCIAL INDICATORS ====================

        public async Task<FinancialIndicatorsDto?> GetFinancialIndicatorsAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId), cancellationToken);
            if (employee is null)
                throw new DomainException($"Employee with id '{employeeId}' not found.");

            if (!employee.PurchasePrice.HasValue || !employee.PurchasePriceCurrency.HasValue)
                return null;

            var purchaseOrders = await _purchaseOrderRepository.GetByEmployeeAsync(employee.Id, cancellationToken);
            var activePOs = purchaseOrders.Where(po => po.PurchaseOrderStatus == PurchaseOrderStatus.ACTIVE).ToList();

            if (activePOs.Count == 0)
                return null;

            // Tarif journalier du dernier bon de commande actif, dans sa devise d'origine
            // (déduite du pays du client).
            var latestPO = activePOs
                .OrderByDescending(po => po.CreatedAt)
                .First();
            var latestCompany = await _companyRepository.GetByIdAsync(latestPO.CompanyId, cancellationToken);
            var dailyRateCurrency = CurrencyConverter.GetBcCurrency(latestCompany);
            var dailyRate = latestPO.DailyRate ?? 0;

            // Marge calculée en MAD : tarif journalier et prix d'achat sont tous deux ramenés en MAD.
            var dailyRateMad = CurrencyConverter.ToMad((decimal)dailyRate, dailyRateCurrency);
            var purchasePriceMad = CurrencyConverter.ToMad(employee.PurchasePrice.Value, employee.PurchasePriceCurrency.Value);
            var marginValue = dailyRateMad - purchasePriceMad;

            return new FinancialIndicatorsDto(
                employee.PurchasePrice.Value,
                employee.PurchasePriceCurrency.Value,
                purchasePriceMad,
                dailyRate,
                dailyRateCurrency,
                dailyRateMad,
                marginValue
            );
        }

        // ==================== PRIVATE HELPERS ====================

        private async Task<EmployeeDto> MapToDtoAsync(Domain.Employees.Employee employee, CancellationToken cancellationToken)
        {
            ManagerSummaryDto? managerDto = null;
            if (employee.ManagerId != null)
            {
                var manager = await _employeeRepository.GetByIdAsync(employee.ManagerId, cancellationToken);
                if (manager != null)
                {
                    managerDto = new ManagerSummaryDto(
                        manager.Id.Value,
                        manager.FullName,
                        manager.Email ?? ""
                    );
                }
            }
            List<ProjectSummaryDto>? projectDtos = null;
            if (employee.Projects.Count > 0)
            {
                projectDtos = employee.Projects.Select(p => new ProjectSummaryDto(
                    p.ProjectId.Value,
                    p.Name,
                    p.Description
                )).ToList();
            }

            return new EmployeeDto(
                employee.Id.Value,
                employee.FirstName,
                employee.LastName,
                employee.FullName,
                employee.Trigram,
                employee.Cnss,
                employee.NationalId,
                employee.DailyRate,
                employee.PhoneNumber,
                employee.Email,
                employee.BankAccountNumber,
                employee.RegistrationNumber,
                employee.PaymentMethod,
                employee.ContractType,
                employee.FreelancerType,
                employee.StartDate,
                employee.ContractEndDate,
                employee.CreatedAt,
                employee.UpdatedAt,
                employee.CreatedBy,
                employee.UpdatedBy,
                employee.JobId.Value,
                projectDtos,
                managerDto,
                employee.MonthlyLeaveAllowance,
                employee.LeaveBalance,
                employee.AnnualConsumedLeaves,
                employee.LastBalanceUpdateAt,
                employee.PurchasePrice,
                employee.PurchasePriceCurrency,
                employee.IsActive
            );
        }

        private static void ValidateLeaveAllowanceForContractType(ContractType contractType, decimal? monthlyLeaveAllowance)
        {
            var isEligible = Domain.Employees.Employee.IsLeaveEligible(contractType);

            if (isEligible && !monthlyLeaveAllowance.HasValue)
                throw new DomainException("Le nombre de jours de congé par mois est obligatoire pour les contrats CDI, CDD et ANAPEC.");

            if (!isEligible && monthlyLeaveAllowance.HasValue)
                throw new DomainException("Le nombre de jours de congé par mois ne s'applique pas aux freelances et stagiaires.");

            if (monthlyLeaveAllowance.HasValue && monthlyLeaveAllowance.Value < 0)
                throw new DomainException("Le nombre de jours de congé par mois doit être positif.");
        }

        private static string GenerateNextRegistrationNumber(string? lastRegistrationNumber, bool isPermanent)
        {
            var nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(lastRegistrationNumber))
            {
                var numericPart = new string(lastRegistrationNumber.Where(char.IsDigit).ToArray());

                if (int.TryParse(numericPart, out var parsedNumber))
                {
                    nextNumber = parsedNumber + 1;
                }
            }

            var formattedNumber = nextNumber.ToString("D3");

            return isPermanent ? formattedNumber : $"EXT-{formattedNumber}";
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            const string all = upper + lower + digits + special;

            var rng = new Random();

            // Garantir au moins 1 de chaque catégorie (requis par Identity)
            var password = new[]
            {
                upper  [rng.Next(upper.Length)],
                lower  [rng.Next(lower.Length)],
                digits [rng.Next(digits.Length)],
                special[rng.Next(special.Length)]
            }.Concat(
                Enumerable.Range(0, 8).Select(_ => all[rng.Next(all.Length)])
            );

            return new string(password.OrderBy(_ => rng.Next()).ToArray());
        }
    }
}
