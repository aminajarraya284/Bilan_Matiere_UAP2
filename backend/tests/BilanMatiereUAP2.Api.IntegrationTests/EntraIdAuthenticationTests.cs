using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BilanMatiereUAP2.Api.IntegrationTests;

/// <summary>
/// Verrouille le comportement de demarrage hors Development (RM-SEC-003) : l'application
/// demarre avec une configuration Entra ID complete, et seulement dans ce cas.
/// </summary>
public sealed class EntraIdAuthenticationTests
{
    [Fact]
    public void Demarrage_HorsDevelopment_AvecAuthorityEtAudience_Reussit()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "false",
            ["Authentication:Jwt:Authority"] = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
            ["Authentication:Jwt:Audience"] = "api://bilan-matiere-uap2"
        });

        var exception = Record.Exception(() => factory.Services.GetService(typeof(object)));

        Assert.Null(exception);
    }

    // Une autorite sans audience laisserait passer des jetons emis pour une autre
    // application du meme tenant : la configuration doit etre complete ou absente.
    [Fact]
    public void Demarrage_HorsDevelopment_AvecAuthoritySansAudience_EchoueExplicitement()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "false",
            ["Authentication:Jwt:Authority"] = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0"
        });

        var exception = Record.Exception(() => factory.Services.GetService(typeof(object)));

        Assert.NotNull(exception);
        Assert.Contains("Authentification de production non configuree", exception.ToString());
    }

    // Le mode Development ne doit jamais pouvoir etre reactive hors Development.
    [Fact]
    public void Demarrage_HorsDevelopment_AvecDevelopmentModeForce_EchoueQuandMeme()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "true"
        });

        var exception = Record.Exception(() => factory.Services.GetService(typeof(object)));

        Assert.NotNull(exception);
        Assert.Contains("Authentification de production non configuree", exception.ToString());
    }

    [Fact]
    public async Task RequeteSansJeton_HorsDevelopment_EstRefusee()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "false",
            ["Authentication:Jwt:Authority"] = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
            ["Authentication:Jwt:Audience"] = "api://bilan-matiere-uap2"
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/types-matiere");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // L'en-tete de developpement ne doit plus rien signifier hors Development.
    [Fact]
    public async Task EnTeteDeveloppement_HorsDevelopment_NeDonneAucunAcces()
    {
        using var factory = new ConfiguredProductionFactory(new Dictionary<string, string?>
        {
            ["Authentication:DevelopmentMode"] = "false",
            ["Authentication:Jwt:Authority"] = "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
            ["Authentication:Jwt:Audience"] = "api://bilan-matiere-uap2"
        });
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/types-matiere");
        request.Headers.Add("X-Development-Role", "Admin");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class ConfiguredProductionFactory(Dictionary<string, string?> configuration)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            // UseSetting alimente la configuration de l'hote AVANT l'execution de Program.cs,
            // contrairement a ConfigureAppConfiguration qui interviendrait trop tard pour
            // les lectures faites au demarrage.
            foreach (var (cle, valeur) in configuration)
            {
                builder.UseSetting(cle, valeur);
            }
        }
    }
}
