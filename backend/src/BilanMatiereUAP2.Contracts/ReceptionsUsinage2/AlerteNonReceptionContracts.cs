namespace BilanMatiereUAP2.Contracts.ReceptionsUsinage2;

/// <summary>
/// Ligne journalière, en pièces, agrégée par date et numéro d'OF.
/// Une valeur nulle signifie que la donnée source est absente.
/// </summary>
public sealed record AlerteNonReceptionLigneResponse(
    DateOnly Date,
    string NumeroOF,
    string? CodeArticle,
    string? DesignationArticle,
    string? CodeTypeMatiere,
    decimal? TotalEmrPieces,
    decimal? RebutEmrPieces,
    decimal? NetEmrPieces,
    decimal QuantiteReceptionneeU2Pieces,
    decimal? PiecesNonReceptionnees,
    string Statut,
    IReadOnlyList<string> Anomalies)
{
    // Alias de transition pour les consommateurs déjà déployés.
    public decimal? CtrlEmrPieces => TotalEmrPieces;
    public decimal? NombreDePieces => NetEmrPieces;
    public decimal? QuantiteNonReceptionneePieces => PiecesNonReceptionnees;
}

public sealed record AlerteNonReceptionResponse(
    DateOnly Date,
    IReadOnlyList<AlerteNonReceptionLigneResponse> Lignes,
    int Total,
    int Page,
    int PageSize,
    int NombreAVerifier,
    int NombreSurReceptions,
    int NombreDonneesManquantes,
    decimal? TotalPiecesNonReceptionnees);

public static class StatutAlerteNonReception
{
    public const string Ok = "OK";
    public const string AVerifier = "AVerifier";
    public const string SurReception = "SurReception";
    public const string AnomalieSurReception = SurReception;
    public const string DonneeManquante = "DonneeManquante";
    public const string AnomalieDonnees = "AnomalieDonnees";
}

public static class AnomalieAlerteNonReception
{
    public const string RapportMachineEmrIntrouvable = "RAPPORT_MACHINE_EMR_INTROUVABLE";
    public const string PlusieursArticlesPourOf = "PLUSIEURS_ARTICLES_POUR_OF";
}
