
using in_ctech_management_backend.Domain.Employees.Enums;

namespace in_ctech_management_backend.Domain.HrDocumentRequests.Enums
{
    public static class HrDocumentType
    {
        // ── CDI ───────────────────────────────────────────────────────
        public const string AttestationTravail = "Attestation de travail";
        public const string AttestationSalaire = "Attestation du salaire";
        public const string Stc = "STC";
        public const string BulletinPaie = "Bulletin de paie";
        public const string DomiciliationSalaire = "Domiciliation de salaire";

        // ── Stagiaire ─────────────────────────────────────────────────
        public const string ConventionStage = "Convention de stage";
        public const string AttestationStage = "Attestation de stage";
        public const string FicheAppreciation = "Fiche d'appréciation";
        public const string FichePresentation = "Fiche de présentation de stage";

        public static readonly string[] CdiTypes =
        [
            AttestationTravail,
            AttestationSalaire,
            Stc,
            BulletinPaie,
            DomiciliationSalaire,
        ];

        public static readonly string[] StagiaireTypes =
        [
            ConventionStage,
            AttestationStage,
            FicheAppreciation,
            FichePresentation,
        ];

        public static string[] GetTypesForContract(ContractType? contractType)
        {
            var isIntern = contractType is ContractType.INTERNSHIP;
            return isIntern ? StagiaireTypes : CdiTypes;
        }
    }
}
