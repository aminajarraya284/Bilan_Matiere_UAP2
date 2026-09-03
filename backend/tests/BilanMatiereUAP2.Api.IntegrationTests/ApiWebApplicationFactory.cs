using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BilanMatiereUAP2.Api.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ApiWebApplicationFactory()
    {
        _connection.Open();
    }

    /// <summary>
    /// Mot de passe des comptes de developpement pour la duree des tests. Ce n'est pas un
    /// secret : les vraies valeurs vivent dans les User Secrets du poste et ne sont jamais
    /// versionnees. Le fournir ici fait passer les tests par le vrai chemin de resolution.
    /// </summary>
    public const string DevelopmentPassword = "mot-de-passe-de-test-non-secret";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["DevelopmentAccounts:admin:Password"] = DevelopmentPassword,
                ["DevelopmentAccounts:fonderie:Password"] = DevelopmentPassword,
                ["DevelopmentAccounts:usinage2:Password"] = DevelopmentPassword
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BilanMatiereUAP2DbContext>>();
            services.RemoveAll<BilanMatiereUAP2DbContext>();
            services.AddDbContext<BilanMatiereUAP2DbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
