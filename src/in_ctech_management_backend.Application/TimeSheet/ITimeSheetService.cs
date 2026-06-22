using in_ctech_management_backend.Application.TimeSheets.DTOs;

namespace in_ctech_management_backend.Application.TimeSheets
{
    public interface ITimeSheetService
    {
        Task<TimeSheetDto> GetAsync(Guid employeeId, int year, int weekNumber, CancellationToken ct = default);
        Task<TimeSheetDto> SaveAsync(Guid employeeId, SaveTimeSheetDto dto, CancellationToken ct = default);
        // Édition par le manager de la feuille d'un de ses collaborateurs : sans restriction de mois,
        // contourne le verrou, passe la soumission à "Validé" et n'envoie aucun email.
        Task<TimeSheetDto> SaveByManagerAsync(Guid managerId, Guid employeeId, SaveTimeSheetDto dto, CancellationToken ct = default);
        Task<IReadOnlyList<TimeSheetDto>> GetMonthAsync(Guid employeeId,int year,int month,CancellationToken cancellationToken = default);
    }
}