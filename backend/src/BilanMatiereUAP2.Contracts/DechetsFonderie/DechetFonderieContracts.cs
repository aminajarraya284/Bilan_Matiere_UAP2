namespace BilanMatiereUAP2.Contracts.DechetsFonderie;

public sealed record CreateDechetFonderieRequest(
    DateOnly DateDeclaration,
    Guid TypeMatiereId,
    string TypeDechet,
    decimal PoidsKg,
    string? Observation);

public sealed record UpdateDechetFonderieRequest(
    DateOnly DateDeclaration,
    Guid TypeMatiereId,
    string TypeDechet,
    decimal PoidsKg,
    string? Observation);

public sealed record DechetFonderieResponse(
    Guid Id,
    DateOnly DateDeclaration,
    int SemaineIso,
    Guid TypeMatiereId,
    string TypeMatiereCode,
    string TypeMatiereLibelle,
    string TypeDechet,
    decimal PoidsKg,
    string? Observation,
    string Statut,
    string? Declarant,
    DateTime DateCreationUtc,
    DateTime? DateModificationUtc);

public sealed record PagedDechetsFonderieResponse(
    IReadOnlyList<DechetFonderieResponse> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record ResumeDechetsFonderieItemResponse(
    Guid TypeMatiereId,
    string TypeMatiereCode,
    decimal TotalBavureKg,
    decimal TotalPoudreKg,
    decimal TotalKg);

public sealed record ResumeDechetsFonderieResponse(
    string Periode,
    IReadOnlyList<ResumeDechetsFonderieItemResponse> TotauxParTypeMatiere,
    decimal TotalGeneralKg);
