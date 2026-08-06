namespace BilanMatiereUAP2.Contracts.Dashboard;

public static class DashboardStatuts
{
    public const string Ok = "OK";
    public const string AVerifier = "AVerifier";
    public const string Anomalie = "Anomalie";
    public const string Partiel = "Partiel";
    public const string NonDisponible = "NonDisponible";
}

/// <summary>
/// Projection mensuelle du pilotage UAP2. Les écarts restent séparés par unité :
/// Fonderie en kilogrammes, Usinage 2 et UAP2 en pièces.
/// </summary>
public sealed record DashboardResponse(
    string Periode,
    DateTime CalculeLeUtc,
    string StatutGlobal,
    DashboardBilanSummary Fonderie,
    DashboardBilanSummary Usinage2,
    DashboardBilanSummary Uap2,
    DashboardOperationsSummary Operations,
    DashboardImportSummary Imports,
    DashboardDataQualitySummary QualiteDonnees,
    IReadOnlyCollection<DashboardWarning> Avertissements);

public sealed record DashboardBilanSummary(
    string Code,
    decimal? Ecart,
    string Unite,
    int NombreLignes,
    int? NombreOf,
    int NombreOk,
    int NombreAVerifier,
    int NombreAnomalies,
    int NombreDonneesManquantes,
    string Statut);

public sealed record DashboardOperationsSummary(
    int InventairesBrouillon,
    int InventairesValides,
    int ReceptionsNonReceptionnees,
    int ReceptionsValidees,
    int? ControlesPoidsAmbigus,
    int OfReferencesManquantes);

public sealed record DashboardImportSummary(
    int Reussis,
    int Partiels,
    int Echoues,
    int FichiersIdentiques,
    DateTime? DernierImportUtc);

public sealed record DashboardDataQualitySummary(
    int NombreAnomalies,
    int NombreDonneesManquantes,
    int NombreConversionsImpossibles,
    int NombreReferencesManquantes,
    string StatutGlobal);

public sealed record DashboardWarning(
    string Code,
    string Source,
    string Message,
    string Statut);
