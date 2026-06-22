using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.LeaveRequests.Enums;

namespace in_ctech_management_backend.Domain.LeaveRequests
{
    public class LeaveRequest
    {
        public LeaveRequestId LeaveRequestId { get; private set; }
        public EmployeeId EmployeeId { get; private set; }
        public LeaveType LeaveType { get; private set; }
        public DateOnly StartDate { get; private set; }
        public DateOnly EndDate { get; private set; }
        public DayPeriod StartPeriod { get; private set; }
        public DayPeriod EndPeriod { get; private set; }
        public decimal NumberOfDays { get; private set; }
        public string Reason { get; private set; } = default!;
        public LeaveRequestStatus Status { get; private set; }
        public string RejectionReason { get; private set; } = string.Empty;
        public string CreatedBy { get; private set; } = default!;
        public string? UpdatedBy { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private LeaveRequest() { }

        private LeaveRequest(
            EmployeeId employeeId,
            LeaveType leaveType,
            DateOnly startDate,
            DateOnly endDate,
            DayPeriod startPeriod,
            DayPeriod endPeriod,
            decimal numberOfDays,
            string reason,
            string createdBy)
        {
            LeaveRequestId = new LeaveRequestId(Guid.NewGuid());
            EmployeeId = employeeId;
            LeaveType = leaveType;
            StartDate = startDate;
            EndDate = endDate;
            StartPeriod = startPeriod;
            EndPeriod = endPeriod;
            NumberOfDays = numberOfDays;
            Reason = reason;
            Status = LeaveRequestStatus.Pending;
            CreatedBy = createdBy;
            CreatedAt = DateTime.UtcNow;
        }

        public static LeaveRequest Create(
            EmployeeId employeeId,
            LeaveType leaveType,
            DateOnly startDate,
            DateOnly endDate,
            DayPeriod startPeriod,
            DayPeriod endPeriod,
            string reason,
            string createdBy,
            IReadOnlySet<DateOnly>? excludedDates = null)
        {
            if (startDate > endDate)
                throw new ArgumentException("La date de début ne peut pas être après la date de fin.");

            if (startDate == endDate &&
                startPeriod == DayPeriod.Afternoon &&
                endPeriod == DayPeriod.Morning)
            {
                throw new ArgumentException("Période invalide pour une même journée.");
            }

            var numberOfDays = CalculateNumberOfDays(
                startDate,
                endDate,
                startPeriod,
                endPeriod,
                excludedDates ?? new HashSet<DateOnly>());

            if (numberOfDays <= 0)
                throw new ArgumentException("Le nombre de jours doit être supérieur à zéro.");

            return new LeaveRequest(
                employeeId,
                leaveType,
                startDate,
                endDate,
                startPeriod,
                endPeriod,
                numberOfDays,
                reason.Trim(),
                createdBy
            );
        }

        public void Approve(string updatedBy)
        {
            if (Status != LeaveRequestStatus.Pending)
                throw new InvalidOperationException("Seules les demandes en attente peuvent être validées.");

            Status = LeaveRequestStatus.Approved;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        // Modification d'une demande par son auteur (collaborateur) tant qu'elle est en attente.
        public void UpdateByCollaborator(
            LeaveType leaveType,
            DateOnly startDate,
            DateOnly endDate,
            DayPeriod startPeriod,
            DayPeriod endPeriod,
            string reason,
            string updatedBy,
            IReadOnlySet<DateOnly>? excludedDates = null)
        {
            if (Status != LeaveRequestStatus.Pending)
                throw new InvalidOperationException("Seules les demandes en attente peuvent être modifiées.");

            if (startDate > endDate)
                throw new ArgumentException("La date de début ne peut pas être après la date de fin.");

            if (startDate == endDate &&
                startPeriod == DayPeriod.Afternoon &&
                endPeriod == DayPeriod.Morning)
            {
                throw new ArgumentException("Période invalide pour une même journée.");
            }

            var numberOfDays = CalculateNumberOfDays(
                startDate,
                endDate,
                startPeriod,
                endPeriod,
                excludedDates ?? new HashSet<DateOnly>());

            if (numberOfDays <= 0)
                throw new ArgumentException("Le nombre de jours doit être supérieur à zéro.");

            LeaveType = leaveType;
            StartDate = startDate;
            EndDate = endDate;
            StartPeriod = startPeriod;
            EndPeriod = endPeriod;
            NumberOfDays = numberOfDays;
            Reason = (reason ?? string.Empty).Trim();
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        // Annulation par le collaborateur. Aucun motif requis.
        public void Cancel(string updatedBy)
        {
            if (Status != LeaveRequestStatus.Pending)
                throw new InvalidOperationException("Seules les demandes en attente peuvent être annulées.");

            Status = LeaveRequestStatus.Cancelled;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reject(string updatedBy, string rejectionReason)
        {
            if (Status != LeaveRequestStatus.Pending)
                throw new InvalidOperationException("Seules les demandes en attente peuvent être refusées.");

            if (string.IsNullOrWhiteSpace(rejectionReason))
                throw new ArgumentException("Le motif de refus est obligatoire.");

            Status = LeaveRequestStatus.Rejected;
            RejectionReason = rejectionReason;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        // Calcul du nombre de jours sans création d'entité — utilisé pour la preview front-end
        // avant soumission (mêmes règles d'exclusion week-end / fériés / congés chevauchants).
        public static decimal PreviewNumberOfDays(
            DateOnly startDate,
            DateOnly endDate,
            DayPeriod startPeriod,
            DayPeriod endPeriod,
            IReadOnlySet<DateOnly>? excludedDates = null)
        {
            if (startDate > endDate)
                throw new ArgumentException("La date de début ne peut pas être après la date de fin.");

            if (startDate == endDate &&
                startPeriod == DayPeriod.Afternoon &&
                endPeriod == DayPeriod.Morning)
            {
                throw new ArgumentException("Période invalide pour une même journée.");
            }

            return CalculateNumberOfDays(
                startDate,
                endDate,
                startPeriod,
                endPeriod,
                excludedDates ?? new HashSet<DateOnly>());
        }

        // Recalcule NumberOfDays à partir d'un nouvel ensemble de jours non travaillés
        // (typiquement après ajout/modification/suppression d'un jour férié).
        // Retourne la variation = ancien - nouveau (> 0 si on a "rendu" des jours, < 0 sinon).
        public decimal Recalculate(IReadOnlySet<DateOnly> excludedDates, string updatedBy)
        {
            var oldNumberOfDays = NumberOfDays;
            var newNumberOfDays = CalculateNumberOfDays(
                StartDate,
                EndDate,
                StartPeriod,
                EndPeriod,
                excludedDates);

            if (newNumberOfDays == oldNumberOfDays)
                return 0m;

            NumberOfDays = newNumberOfDays;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;

            return oldNumberOfDays - newNumberOfDays;
        }

        private static decimal CalculateNumberOfDays(
            DateOnly startDate,
            DateOnly endDate,
            DayPeriod startPeriod,
            DayPeriod endPeriod,
            IReadOnlySet<DateOnly> excludedDates)
        {
            if (startDate == endDate)
            {
                if (IsNonWorkingDay(startDate, excludedDates))
                    return 0m;

                return startPeriod == endPeriod ? 0.5m : 1m;
            }

            decimal result = 0m;
            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                if (IsNonWorkingDay(d, excludedDates))
                    continue;

                decimal dayValue = 1m;

                if (d == startDate && startPeriod == DayPeriod.Afternoon)
                    dayValue -= 0.5m;

                if (d == endDate && endPeriod == DayPeriod.Morning)
                    dayValue -= 0.5m;

                result += dayValue;
            }

            return result;
        }

        private static bool IsNonWorkingDay(DateOnly date, IReadOnlySet<DateOnly> excludedDates)
            => date.DayOfWeek == DayOfWeek.Saturday
            || date.DayOfWeek == DayOfWeek.Sunday
            || excludedDates.Contains(date);
    }
}