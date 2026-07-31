namespace BilanMatiereUAP2.Contracts.InventairesFonderie;

public sealed record CreateInventaireFonderieRequest(
    string Periode,
    Guid TypeMatiereId,
    decimal? StockDebutKg,
    decimal StockFinKg,
    string? Observation);

public sealed record UpdateInventaireFonderieRequest(
    string Periode,
    Guid TypeMatiereId,
    decimal? StockDebutKg,
    decimal StockFinKg,
    string? Observation);

public sealed record InventaireFonderieResponse(
    Guid Id,
    string Periode,
    Guid TypeMatiereId,
    string TypeMatiereCode,
    string TypeMatiereLibelle,
    decimal? StockDebutKg,
    decimal StockFinKg,
    string? Observation,
    string Statut,
    string? CreePar,
    DateTime CreeLeUtc,
    string? ModifiePar,
    DateTime? ModifieLeUtc,
    string? ValidePar,
    DateTime? ValideLeUtc,
    string? AnnulePar,
    DateTime? AnnuleLeUtc);

public sealed record PagedInventairesFonderieResponse(
    IReadOnlyList<InventaireFonderieResponse> Items,
    int Total,
    int Page,
    int PageSize);
