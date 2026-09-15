using BilanMatiereUAP2.Contracts.Bilans;

namespace BilanMatiereUAP2.Contracts.Archivage;

public sealed record PeriodeBilanSnapshot(
    int SchemaVersion,
    string VersionCalcul,
    string Periode,
    DateTime DateEnregistrementUtc,
    string EnregistrePar,
    string StatutGlobal,
    BilanFonderieResponse Fonderie,
    BilanUsinage2Response Usinage2,
    BilanUap2Response Uap2,
    IReadOnlyDictionary<string, string?> ParametresCalcul,
    IReadOnlyList<string> Anomalies);

public sealed record PeriodeBilanArchiveListItem(
    Guid Id,
    string Periode,
    DateTime DateEnregistrementUtc,
    string EnregistrePar,
    string StatutGlobal,
    int NombreAlertes,
    int NombreAnomalies,
    int NombreDonneesManquantes,
    int SchemaVersion,
    string VersionCalcul);

public sealed record PagedPeriodesBilanArchiveesResponse(
    IReadOnlyList<PeriodeBilanArchiveListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PeriodeBilanArchiveDetailResponse(
    PeriodeBilanArchiveListItem Metadata,
    PeriodeBilanSnapshot Snapshot);

