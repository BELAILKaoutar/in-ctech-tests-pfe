using in_ctech_management_backend.Application.HrDocumentRequest.DTOs;
using in_ctech_management_backend.Application.Shared;
using in_ctech_management_backend.Domain.Employees;

using in_ctech_management_backend.Domain.Employees.Repositories;
using in_ctech_management_backend.Domain.HrDocumentRequests;
using in_ctech_management_backend.Domain.HrDocumentRequests.Enums;
using in_ctech_management_backend.Domain.HrDocumentRequests.Repositories;

namespace in_ctech_management_backend.Application.HrDocumentRequest;

public class HrDocumentRequestService : IHrDocumentRequestService
{
    private readonly IHrDocumentRequestRepository _hrDocumentRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmailService _emailService;
    private readonly IEmployeeInfosService _employeeInfosService;

    public HrDocumentRequestService(
        IHrDocumentRequestRepository hrDocumentRequestRepository,
        IEmployeeRepository employeeRepository,
        IEmailService emailService,
        IEmployeeInfosService employeeInfosService)
    {
        _hrDocumentRequestRepository = hrDocumentRequestRepository;
        _employeeRepository = employeeRepository;
        _emailService = emailService;
        _employeeInfosService = employeeInfosService;
    }

    // ── GET MY REQUESTS ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<HrDocumentRequestDto>> GetMyRequestsAsync(
        Guid employeeId,
        int? year,
        HrDocumentRequestStatus? status,
        CancellationToken ct = default)
    {
        var requests = await _hrDocumentRequestRepository.GetByEmployeeAsync(
            new EmployeeId(employeeId), year, status, ct);

        var employeeInfo = await _employeeInfosService.GetCollaboratorEmailAsync(
            employeeId, ct);

        return requests
            .Select(r => HrDocumentRequestMapper.ToDto(r, employeeInfo.FullName))
            .ToList();
    }

    // ── GET ALL REQUESTS ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<HrDocumentRequestDto>> GetAllRequestsAsync(
        int? year,
        HrDocumentRequestStatus? status,
        CancellationToken ct = default)
    {
        var requests = await _hrDocumentRequestRepository.GetAllAsync(year, status, ct);

        var employeeIds = requests
            .Select(r => (Guid)r.EmployeeId)
            .Distinct()
            .ToList();

        var employeeSummaries = await _employeeInfosService.GetEmployeeSummariesByIdsAsync(
            employeeIds, ct);

        return requests
            .Select(r => HrDocumentRequestMapper.ToDto(
                r,
                employeeSummaries.GetValueOrDefault((Guid)r.EmployeeId)?.FullName))
            .ToList();
    }

    // ── GET DOCUMENT TYPES ────────────────────────────────────────────────────

    public async Task<string[]> GetDocumentTypesAsync(
        Guid employeeId,
        CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId));

        if (employee is null)
            throw new ArgumentException("Employé introuvable.");

        return HrDocumentType.GetTypesForContract(employee.ContractType);
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

    public async Task<HrDocumentRequestDto> CreateAsync(
        Guid employeeId,
        CreateHrDocumentRequestDto dto,
        CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId));
        if (employee is null)
            throw new ArgumentException("Employé introuvable.");

        var allowedTypes = HrDocumentType.GetTypesForContract(employee.ContractType);
        if (!allowedTypes.Contains(dto.DocumentType))
            throw new ArgumentException(
                $"Le type '{dto.DocumentType}' n'est pas disponible pour ce profil.");

        var request = Domain.HrDocumentRequests.HrDocumentRequest.Create(
            new EmployeeId(employeeId),
            dto.DocumentType,
            createdBy: employee.FullName ?? employeeId.ToString()); 

        await _hrDocumentRequestRepository.AddAsync(request, ct);

        // Email au manager
        var employeeInfo = await _employeeInfosService
            .GetCollaboratorWithManagerEmailAsync(employeeId, ct);

        if (!string.IsNullOrWhiteSpace(employeeInfo.ManagerEmail))
        {
            await _emailService.SendHrDocumentRequestCreatedEmailAsync(
                employeeInfo.ManagerEmail,
                employeeInfo.FullName ?? "N/A",
                employeeInfo.Email ?? "N/A",
                request.DocumentType,
                request.CreatedAt);
        }

        return HrDocumentRequestMapper.ToDto(request, employeeInfo.FullName);
    }

    // ── UPDATE STATUS ─────────────────────────────────────────────────────────

    public async Task<HrDocumentRequestDto> UpdateStatusAsync(
        Guid id,
        string updatedBy,
        UpdateHrDocumentStatusDto dto,
        CancellationToken ct = default)
    {
        var request = await _hrDocumentRequestRepository.GetByIdAsync(
            new HrDocumentRequestId(id), ct);

        if (request is null)
            throw new KeyNotFoundException("Demande de document introuvable.");

        // ── Résoudre le nom si updatedBy est un GUID ──────────────────
        if (Guid.TryParse(updatedBy, out var updatedByGuid))
        {
            var updater = await _employeeRepository.GetByIdAsync(new EmployeeId(updatedByGuid));
            updatedBy = updater?.FullName ?? updatedBy;
        }

        switch (dto.Status)
        {
            case HrDocumentRequestStatus.Approved:
                request.Approve(updatedBy);
                break;

            case HrDocumentRequestStatus.Rejected:
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    throw new ArgumentException("Le motif de refus est obligatoire.");
                request.Reject(updatedBy, dto.RejectionReason);
                break;

            default:
                throw new ArgumentException("Statut invalide.");
        }

        await _hrDocumentRequestRepository.UpdateAsync(request, ct);

        return HrDocumentRequestMapper.ToDto(request);
    }

    public async Task<HrDocumentRequestDto> UpdateAsync(
    Guid id,
    Guid employeeId,
    UpdateHrDocumentRequestDto dto,
    CancellationToken ct = default)
    {
        var request = await _hrDocumentRequestRepository.GetByIdAsync(
            new HrDocumentRequestId(id), ct);

        if (request is null)
            throw new KeyNotFoundException("Demande de document introuvable.");

        if ((Guid)request.EmployeeId != employeeId)
            throw new UnauthorizedAccessException(
                "Cette demande n'appartient pas à l'employé connecté.");

        // Validation du type selon le contrat
        var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId));
        if (employee is null)
            throw new ArgumentException("Employé introuvable.");

        var allowedTypes = HrDocumentType.GetTypesForContract(employee.ContractType);
        if (!allowedTypes.Contains(dto.DocumentType))
            throw new ArgumentException(
                $"Le type '{dto.DocumentType}' n'est pas disponible pour ce profil.");

        request.Update(dto.DocumentType, employee.FullName ?? employeeId.ToString());

        await _hrDocumentRequestRepository.UpdateAsync(request, ct);

        // Email au manager
        var employeeInfo = await _employeeInfosService
            .GetCollaboratorWithManagerEmailAsync(employeeId, ct);

        if (!string.IsNullOrWhiteSpace(employeeInfo.ManagerEmail))
        {
            await _emailService.SendHrDocumentRequestUpdatedEmailAsync(
                employeeInfo.ManagerEmail,
                employeeInfo.FullName ?? "N/A",
                employeeInfo.Email ?? "N/A",
                request.DocumentType,
                request.UpdatedAt ?? request.CreatedAt);
        }

        return HrDocumentRequestMapper.ToDto(request, employeeInfo.FullName);
    }

    // ── CANCEL ────────────────────────────────────────────────────────────────────

    public async Task<HrDocumentRequestDto> CancelAsync(
        Guid id,
        Guid employeeId,
        CancellationToken ct = default)
    {
        var request = await _hrDocumentRequestRepository.GetByIdAsync(
            new HrDocumentRequestId(id), ct);

        if (request is null)
            throw new KeyNotFoundException("Demande de document introuvable.");

        if ((Guid)request.EmployeeId != employeeId)
            throw new UnauthorizedAccessException(
                "Cette demande n'appartient pas à l'employé connecté.");

        var employee = await _employeeRepository.GetByIdAsync(new EmployeeId(employeeId));

        request.Cancel(employee?.FullName ?? employeeId.ToString());

        await _hrDocumentRequestRepository.UpdateAsync(request, ct);

        // Email au manager
        var employeeInfo = await _employeeInfosService
            .GetCollaboratorWithManagerEmailAsync(employeeId, ct);

        if (!string.IsNullOrWhiteSpace(employeeInfo.ManagerEmail))
        {
            await _emailService.SendHrDocumentRequestCancelledEmailAsync(
                employeeInfo.ManagerEmail,
                employeeInfo.FullName ?? "N/A",
                employeeInfo.Email ?? "N/A",
                request.DocumentType,
                request.UpdatedAt ?? request.CreatedAt);
        }

        return HrDocumentRequestMapper.ToDto(request, employeeInfo.FullName);
    }

}
