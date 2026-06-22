namespace in_ctech_management_backend.Domain.TimeSheets.Submissions.Enums
{
    public enum SubmissionStatus
    {
        Draft,      // Brouillon (auto-créée au 1er save d'une feuille)
        Pending,    // En attente
        ToCorrect,  // À corriger
        Approved    // Validé
    }
}
