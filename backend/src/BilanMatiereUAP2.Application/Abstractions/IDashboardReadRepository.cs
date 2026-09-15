namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record DashboardOperationsData(
    int InventairesBrouillon,
    int InventairesValides,
    int ReceptionsValidees);

public sealed record DashboardImportsData(
    int Reussis,
    int Partiels,
    int Echoues,
    int FichiersIdentiques,
    int EnCours,
    int NombreAnomalies,
    DateTime? DernierImportUtc);

/// <summary>
/// Agrégats SQL strictement opérationnels du Dashboard. Les bilans restent calculés
/// par leurs services applicatifs dédiés et ne sont jamais reproduits ici.
/// </summary>
public interface IDashboardReadRepository
{
    Task<DashboardOperationsData> GetOperationsAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default);

    Task<DashboardImportsData> GetImportsAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default);
}
