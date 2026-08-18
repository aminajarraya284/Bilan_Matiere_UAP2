namespace BilanMatiereUAP2.Contracts.Alertes;

public sealed record AlerteListItemResponse(
    Guid Id,
    string Code,
    string Type,
    string Severite,
    string Statut,
    string Titre,
    string Source,
    string? Periode,
    string? NumeroOF,
    string? CodeArticle,
    string? TypeMatiere,
    DateTime DetecteeLeUtc,
    DateTime DerniereDetectionLeUtc,
    string? Description = null,
    DateTime? AcquitteeLeUtc = null,
    string? AcquitteePar = null,
    DateTime? ResolueLeUtc = null,
    string? ResoluePar = null,
    DateTime? IgnoreeLeUtc = null,
    string? IgnoreePar = null,
    string? CommentaireTraitement = null);

public sealed record AlerteDetailResponse(
    Guid Id,
    string Code,
    string Type,
    string Severite,
    string Statut,
    string Titre,
    string Description,
    string Source,
    string? Periode,
    string? NumeroOF,
    string? CodeArticle,
    string? TypeMatiere,
    DateTime DetecteeLeUtc,
    DateTime DerniereDetectionLeUtc,
    DateTime? AcquitteeLeUtc,
    string? AcquitteePar,
    DateTime? ResolueLeUtc,
    string? ResoluePar,
    DateTime? IgnoreeLeUtc,
    string? IgnoreePar,
    string? CommentaireTraitement,
    bool ResolutionAutomatique,
    IReadOnlyDictionary<string, string?> DonneesAssociees);

public sealed record AlerteSummaryResponse(
    int Total,
    int Ouvertes,
    int Acquittees,
    int Resolues,
    int Ignorees);

public sealed record PagedAlertesResponse(
    IReadOnlyList<AlerteListItemResponse> Items,
    int Total,
    int Page,
    int PageSize,
    AlerteSummaryResponse Summary,
    DateTime? DerniereDetectionLeUtc);

public sealed record AcquitterAlerteRequest(string? Commentaire);

public sealed record ResoudreAlerteRequest(string? Commentaire);

public sealed record IgnorerAlerteRequest(string Commentaire);

public sealed record AlerteRecalculResponse(
    string Periode,
    DateTime RecalculeLeUtc,
    int NombreDetections,
    int NombreCreees,
    int NombreMisesAJour,
    int NombreResoluesAutomatiquement);
