using BilanMatiereUAP2.Contracts.Imports;

namespace BilanMatiereUAP2.Contracts.RapportsMachine;

public sealed record RapportMachineConsultationItemResponse(
    Guid Id,
    DateOnly DateProduction,
    string NumeroOF,
    string CodeArticle,
    string? DesignationArticle,
    string CodePoste,
    string NumeroOperation,
    string? Equipe,
    string Processus,
    decimal QuantiteTotale,
    decimal QuantiteRebut,
    decimal QuantiteNette,
    Guid ImportBatchId);

public sealed record RapportMachineKpiResponse(
    int NombreLignes,
    int NombreOF,
    int Ctrl,
    int Sci,
    int Emr,
    int Trn,
    int Inconnues,
    int LignesRejetees);

public sealed record RapportMachineConsultationResponse(
    IReadOnlyList<RapportMachineConsultationItemResponse> Items,
    int Total,
    int Page,
    int PageSize,
    RapportMachineKpiResponse Kpis,
    ImportTrackingResponse Imports);
