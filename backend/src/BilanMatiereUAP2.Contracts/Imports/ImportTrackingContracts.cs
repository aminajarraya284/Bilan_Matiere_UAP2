namespace BilanMatiereUAP2.Contracts.Imports;

public sealed record ImportExecutionSummaryResponse(
    Guid ImportBatchId,
    string Statut,
    string? Message,
    string? NomFichier,
    DateTime DateCreationUtc,
    DateTime? DateFinUtc,
    int LignesLues,
    int LignesValides,
    int LignesRejetees,
    int LignesInserees,
    int LignesMisesAJour,
    int LignesInchangees);

public sealed record ImportTrackingResponse(
    ImportExecutionSummaryResponse? DerniereTentative,
    ImportExecutionSummaryResponse? DernierImportReussi);
