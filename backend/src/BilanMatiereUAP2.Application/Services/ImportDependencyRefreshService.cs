using BilanMatiereUAP2.Application.Abstractions;

namespace BilanMatiereUAP2.Application.Services;

public sealed class ImportDependencyRefreshService : IImportDependencyRefreshService
{
    public Task RefreshAfterMvtEntreeOfAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RefreshAfterRapportMachineAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
