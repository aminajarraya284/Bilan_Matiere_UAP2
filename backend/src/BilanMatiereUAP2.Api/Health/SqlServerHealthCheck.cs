using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BilanMatiereUAP2.Api.Health;

public sealed class SqlServerHealthCheck(BilanMatiereUAP2DbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("SQL Server est accessible.")
                : HealthCheckResult.Unhealthy("SQL Server est inaccessible.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("La connexion SQL Server a échoué.", exception);
        }
    }
}
