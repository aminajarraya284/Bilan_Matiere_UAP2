using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BilanMatiereUAP2.Api.IntegrationTests;

public sealed class ProductionAuthenticationGuardTests
{
    // Hors Development, l'application ne doit jamais demarrer avec DevelopmentAuthenticationHandler :
    // sans configuration JWT/OIDC reelle, le demarrage doit echouer explicitement plutot que de
    // tourner silencieusement sans authentification (ou d'accepter X-Development-Role).
    [Fact]
    public void Demarrage_EnProduction_SansConfigurationJwt_EchoueExplicitement()
    {
        using var factory = new ProductionWebApplicationFactory();

        var exception = Record.Exception(() => factory.Services.GetService(typeof(object)));

        Assert.NotNull(exception);
        Assert.Contains("Authentification de production non configuree", exception.ToString());
    }

    private sealed class ProductionWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                // S'assure qu'aucun DevelopmentMode ni configuration JWT ne fuite depuis un
                // appsettings local, pour verifier le comportement "hors Development" pur.
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:DevelopmentMode"] = "false"
                });
            });
        }
    }
}
