namespace BilanMatiereUAP2.Contracts.ImportsX3;

public sealed record ImportX3FileResponse(
    string TypeFichier,
    string NomFichier,
    string Extension,
    long TailleOctets,
    string HashSha256);

public sealed record ImportX3StatisticsResponse(
    int LignesLues,
    int LignesValides,
    int LignesIgnorees);

public sealed record ImportX3AnomalyResponse(
    Guid Id,
    string TypeFichier,
    int? NumeroLigneSource,
    string Statut,
    string Message,
    string? NumeroOF,
    string? CodeComposant);

public sealed record ConsommationComposantResponse(
    Guid Id,
    DateOnly DateImputation,
    string NumeroOF,
    string ArticlePrincipal,
    string? DesignationPrincipale,
    Guid ComposantId,
    string CodeComposant,
    string CodeTypeMatiere,
    string? DesignationComposant,
    string Unite,
    decimal SommeQuantiteX3Signee,
    decimal Consommation,
    string Statut,
    Guid ImportBatchId);

public sealed record ImportX3Response(
    Guid ImportBatchId,
    string Statut,
    bool EstDoublon,
    ImportX3StatisticsResponse EncoursProduct,
    ImportX3StatisticsResponse MouvementsStock,
    int SortiesGenerees,
    int NombreAnomalies,
    IReadOnlyList<ImportX3FileResponse> Fichiers,
    IReadOnlyList<ConsommationComposantResponse> Consommations,
    IReadOnlyList<ImportX3AnomalyResponse> Anomalies,
    DateTime DateCreationUtc,
    DateTime? DateFinUtc);

public sealed record PagedConsommationsComposantsResponse(
    IReadOnlyList<ConsommationComposantResponse> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record X3OperationOFResponse(
    Guid Id,
    int NumeroLigneSource,
    string NumeroOperation,
    string CodePoste,
    string? TypePoste,
    string? NumeroPoste,
    decimal? QuantitePrevue,
    decimal? QuantiteRestante,
    DateOnly? DateOperation);

public sealed record X3MouvementStockResponse(
    Guid Id,
    int NumeroLigneSource,
    DateOnly DateImputation,
    string? Code,
    decimal QuantiteX3Signee,
    string? Unite,
    string? Transaction,
    string? TypePiece,
    string? PieceOrigine,
    string StatutRapprochement);

public sealed record OrdreFabricationContexteResponse(
    Guid Id,
    string NumeroOF,
    string? ArticlePrincipal,
    string? DesignationPrincipale,
    string? SituationOF,
    string? Atelier,
    DateOnly? DateObjectifOF,
    string? StatutX3,
    IReadOnlyList<X3OperationOFResponse> Operations,
    IReadOnlyList<X3MouvementStockResponse> Mouvements,
    IReadOnlyList<ConsommationComposantResponse> Consommations);
