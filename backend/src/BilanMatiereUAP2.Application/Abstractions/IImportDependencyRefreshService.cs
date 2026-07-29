namespace BilanMatiereUAP2.Application.Abstractions;

public interface IImportDependencyRefreshService
{
    Task RefreshAfterMvtEntreeOfAsync(CancellationToken cancellationToken = default);
    Task RefreshAfterRapportMachineAsync(CancellationToken cancellationToken = default);
}
