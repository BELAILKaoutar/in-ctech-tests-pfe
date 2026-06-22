using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.TimeSheets.Submissions.Enums;

namespace in_ctech_management_backend.Domain.TimeSheets.Submissions
{
    public class TimeSheetSubmission
    {
        public SubmissionId Id { get; private set; }
        public EmployeeId EmployeeId { get; private set; }
        public int Year { get; private set; }
        public int Month { get; private set; }
        public SubmissionStatus Status { get; private set; }
        public DateTime SubmittedAt { get; private set; }
        public string SubmittedBy { get; private set; }
        public DateTime? ReviewedAt { get; private set; }
        public string? ReviewedBy { get; private set; }
        public string? RejectionReason { get; private set; }

        private TimeSheetSubmission() { }

        // Création automatique au 1er save d'une feuille du mois.
        // Représente l'agrégat mensuel en cours de saisie, pas encore soumis.
        public static TimeSheetSubmission CreateDraft(
            EmployeeId employeeId,
            int year,
            int month,
            string createdBy)
        {
            if (month < 1 || month > 12)
                throw new DomainException($"Mois invalide : {month}.");

            return new TimeSheetSubmission
            {
                Id = new SubmissionId(Guid.NewGuid()),
                EmployeeId = employeeId,
                Year = year,
                Month = month,
                Status = SubmissionStatus.Draft,
                SubmittedAt = DateTime.UtcNow,
                SubmittedBy = createdBy
            };
        }

        // Première soumission : Draft -> Pending
        public void Submit(string submittedBy)
        {
            if (Status != SubmissionStatus.Draft)
                throw new DomainException("Seule une soumission 'Brouillon' peut être soumise.");

            Status = SubmissionStatus.Pending;
            SubmittedAt = DateTime.UtcNow;
            SubmittedBy = submittedBy;
        }

        // Re-soumission après "À corriger" : on remet en Pending
        public void Resubmit(string submittedBy)
        {
            if (Status != SubmissionStatus.ToCorrect)
                throw new DomainException("Seule une soumission 'À corriger' peut être re-soumise.");

            Status = SubmissionStatus.Pending;
            SubmittedAt = DateTime.UtcNow;
            SubmittedBy = submittedBy;
            RejectionReason = null;
            ReviewedAt = null;
            ReviewedBy = null;
        }

        public void Approve(string reviewedBy)
        {
            if (Status != SubmissionStatus.Pending)
                throw new DomainException("Seule une soumission 'En attente' peut être validée.");

            Status = SubmissionStatus.Approved;
            ReviewedAt = DateTime.UtcNow;
            ReviewedBy = reviewedBy;
        }

        public void MarkToCorrect(string reviewedBy, string reason)
        {
            if (Status != SubmissionStatus.Pending)
                throw new DomainException("Seule une soumission 'En attente' peut être renvoyée pour correction.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Le motif de correction est obligatoire.");

            Status = SubmissionStatus.ToCorrect;
            ReviewedAt = DateTime.UtcNow;
            ReviewedBy = reviewedBy;
            RejectionReason = reason;
        }
    }
}
