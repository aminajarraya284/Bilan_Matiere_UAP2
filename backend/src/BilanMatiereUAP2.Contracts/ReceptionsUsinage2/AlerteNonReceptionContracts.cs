namespace BilanMatiereUAP2.Contracts.ReceptionsUsinage2;

/// <summary>
/// Une ligne de l'alerte JOURNALIERE des pieces non receptionnees, pour un Numero OF.
///
/// Toutes les quantites sont en PIECES. Cette alerte est distincte du Bilan Usinage 2,
/// qui est mensuel et repose sur Receptions - Rebuts U2 - Entrees OF.
///
/// Un champ nul signifie « donnee indisponible », jamais zero : l'absence de rapport
/// machine EMR ne doit pas etre lue comme une production nulle.
/// </summary>
public sealed record AlerteNonReceptionLigneResponse(
    DateOnly Date,
    string NumeroOF,
    /// <summary>Article associe issu des Sorties MP. « Plusieurs articles » si l'OF en porte plusieurs.</summary>
    string? CodeArticle,
    string? CodeTypeMatiere,
    decimal? CtrlEmrPieces,
    decimal? RebutEmrPieces,
    /// <summary>CtrlEmrPieces - RebutEmrPieces.</summary>
    decimal? NombreDePieces,
    decimal QuantiteReceptionneeU2Pieces,
    /// <summary>NombreDePieces - QuantiteReceptionneeU2Pieces. Le signe est conserve.</summary>
    decimal? QuantiteNonReceptionneePieces,
    string Statut,
    IReadOnlyList<string> Anomalies);

public sealed record AlerteNonReceptionResponse(
    DateOnly Date,
    IReadOnlyList<AlerteNonReceptionLigneResponse> Lignes,
    int Total,
    int Page,
    int PageSize,
    /// <summary>Nombre de lignes au statut AVerifier ou AnomalieSurReception.</summary>
    int NombreAVerifier,
    /// <summary>Nombre de lignes dont le rapport machine EMR est introuvable.</summary>
    int NombreDonneesManquantes);

public static class StatutAlerteNonReception
{
    public const string Ok = "OK";
    public const string AVerifier = "AVerifier";
    public const string AnomalieSurReception = "AnomalieSurReception";
    public const string DonneeManquante = "DonneeManquante";
    public const string AnomalieDonnees = "AnomalieDonnees";
}

public static class AnomalieAlerteNonReception
{
    public const string RapportMachineEmrIntrouvable = "RAPPORT_MACHINE_EMR_INTROUVABLE";
    public const string PlusieursArticlesPourOf = "PLUSIEURS_ARTICLES_POUR_OF";
}
