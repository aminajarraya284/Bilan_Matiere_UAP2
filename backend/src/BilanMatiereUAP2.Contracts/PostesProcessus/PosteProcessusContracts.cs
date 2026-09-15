namespace BilanMatiereUAP2.Contracts.PostesProcessus;

public sealed record CreatePosteProcessusRequest(
    string Poste,
    string? Libelle,
    string OrigineProcessus);

public sealed record UpdatePosteProcessusRequest(
    string? Libelle,
    string OrigineProcessus);

public sealed record ChangePosteProcessusActivationRequest(bool Actif);

public sealed record PosteProcessusResponse(
    Guid Id,
    string Poste,
    string? Libelle,
    string OrigineProcessus,
    bool Actif,
    DateTime DateCreationUtc,
    DateTime? DateModificationUtc,
    string? CreePar,
    string? ModifiePar);

public sealed record PagedPostesProcessusResponse(
    IReadOnlyList<PosteProcessusResponse> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record PosteProcessusHistoriqueResponse(
    Guid Id,
    Guid PosteProcessusId,
    string Poste,
    string OrigineProcessus,
    bool Actif,
    string Action,
    string? Utilisateur,
    DateTime DateUtc);

public sealed record PosteNonMappeResponse(
    string Poste,
    int NombreLignes,
    decimal QuantiteTotale,
    decimal QuantiteRebut,
    DateOnly PremiereDate,
    DateOnly DerniereDate,
    IReadOnlyList<string> ExemplesArticles,
    IReadOnlyList<string> ExemplesOF);

public sealed record RecalculClassificationPostesResponse(
    int LignesAnalysees,
    int LignesModifiees,
    int LignesInchangees,
    IReadOnlyDictionary<string, int> LignesParOrigine);
