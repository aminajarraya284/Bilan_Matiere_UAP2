using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record RapportMachineConsultationQuery(
    DateOnly? DateDebut,
    DateOnly? DateFin,
    string? NumeroOF,
    string? CodeArticle,
    string? CodePoste,
    string? Processus,
    Guid? ImportBatchId,
    int Page = 1,
    int PageSize = 50,
    string? SortBy = null,
    string? SortDirection = null);

public sealed record RapportMachineConsultationFilter(
    DateOnly? DateDebut,
    DateOnly? DateFin,
    string? NumeroOF,
    string? CodeArticle,
    string? CodePoste,
    OrigineProcessus? Processus,
    Guid? ImportBatchId,
    int Page,
    int PageSize,
    string SortBy,
    bool Descending);

public sealed record ImportBatchSnapshot(
    ImportBatch Batch,
    string? NomFichier);

public sealed record ImportTrackingSnapshot(
    ImportBatchSnapshot? DerniereTentative,
    ImportBatchSnapshot? DernierImportReussi);

public sealed record RapportMachineKpiResult(
    int NombreLignes,
    int NombreOF,
    int Ctrl,
    int Sci,
    int Emr,
    int Trn,
    int Inconnues,
    int LignesRejetees);

public sealed record RapportMachineConsultationResult(
    IReadOnlyList<X3RapportMachine> Items,
    int Total,
    RapportMachineKpiResult Kpis,
    ImportTrackingSnapshot Imports);
