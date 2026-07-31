namespace BilanMatiereUAP2.Contracts.ControlePoidsFonderie;

public sealed record CreateControlePoidsFonderieRequest(
    DateOnly DateControle,
    string? NumeroOF,
    string CodeArticle,
    Guid TypeMatiereId,
    string? NumeroLotOuSerie,
    decimal PoidsPhysiqueUnitaireKg,
    int? NombreEchantillons,
    string? Observation);

public sealed record UpdateControlePoidsFonderieRequest(
    DateOnly DateControle,
    string? NumeroOF,
    string CodeArticle,
    Guid TypeMatiereId,
    string? NumeroLotOuSerie,
    decimal PoidsPhysiqueUnitaireKg,
    int? NombreEchantillons,
    string? Observation);

public sealed record ControlePoidsFonderieResponse(
    Guid Id,
    DateOnly DateControle,
    string? NumeroOF,
    string CodeArticle,
    Guid TypeMatiereId,
    string TypeMatiereCode,
    string? NumeroLotOuSerie,
    decimal PoidsPhysiqueUnitaireKg,
    int? NombreEchantillons,
    string? Observation,
    string Statut,
    string? CreePar,
    DateTime CreeLeUtc,
    string? ValidePar,
    DateTime? ValideLeUtc);

public sealed record PagedControlesPoidsFonderieResponse(
    IReadOnlyList<ControlePoidsFonderieResponse> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record ControlePoidsFonderieCandidateResponse(
    Guid Id,
    DateOnly DateControle,
    string? NumeroOF,
    string CodeArticle,
    string CodeTypeMatiere,
    decimal PoidsPhysiqueUnitaireKg,
    string Statut);

public sealed record ControlePoidsFonderieResolutionResponse(
    string Statut,
    decimal? PoidsPhysiqueUnitaireKg,
    Guid? ControlePoidsFonderieId,
    string? Source,
    IReadOnlyList<ControlePoidsFonderieCandidateResponse> Candidats,
    string? Message);
