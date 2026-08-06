using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BilanMatiereUAP2.Api.Authentication;
using BilanMatiereUAP2.Application.Features.Dashboard;
using BilanMatiereUAP2.Contracts.Dashboard;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class DashboardApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_PeriodeValide_RetourneContratReelEtUnitesSeparees()
    {
        var response = await _client.GetAsync("/api/v1/dashboard?periode=2026-01");
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dashboard);
        Assert.Equal("2026-01", dashboard!.Periode);
        Assert.Equal("kg", dashboard.Fonderie.Unite);
        Assert.Equal("pieces", dashboard.Usinage2.Unite);
        Assert.Equal("pieces", dashboard.Uap2.Unite);
        Assert.Null(dashboard.Fonderie.Ecart);
        Assert.Null(dashboard.Usinage2.Ecart);
        Assert.Null(dashboard.Uap2.Ecart);
        Assert.NotEmpty(dashboard.Avertissements);
    }

    [Fact]
    public async Task Get_AvecDonnees_RetourneLesTroisEcartsSansMelangerLesUnites()
    {
        using var responseFactory = new DashboardResponseFactory(CreerDashboardComplet());
        using var client = responseFactory.CreateClient();

        var response = await client.GetAsync("/api/v1/dashboard?periode=2026-01");
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dashboard);
        Assert.Equal(12.5m, dashboard!.Fonderie.Ecart);
        Assert.Equal("kg", dashboard.Fonderie.Unite);
        Assert.Equal(8m, dashboard.Usinage2.Ecart);
        Assert.Equal("pieces", dashboard.Usinage2.Unite);
        Assert.Equal(0m, dashboard.Uap2.Ecart);
        Assert.Equal("pieces", dashboard.Uap2.Unite);
    }

    [Fact]
    public async Task Get_DonneesPartielles_ConserveNullEtAvertissement()
    {
        var complet = CreerDashboardComplet();
        var partiel = complet with
        {
            StatutGlobal = DashboardStatuts.Partiel,
            Uap2 = complet.Uap2 with
            {
                Ecart = null,
                NombreDonneesManquantes = 1,
                Statut = DashboardStatuts.Partiel
            },
            Avertissements =
            [
                new DashboardWarning(
                    "UAP2_STATUT_PARTIEL",
                    "UAP2",
                    "Bilan UAP2 partiel.",
                    DashboardStatuts.Partiel)
            ]
        };
        using var responseFactory = new DashboardResponseFactory(partiel);
        using var client = responseFactory.CreateClient();

        var response = await client.GetAsync("/api/v1/dashboard?periode=2026-01");
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dashboard);
        Assert.Null(dashboard!.Uap2.Ecart);
        Assert.Equal(DashboardStatuts.Partiel, dashboard.Uap2.Statut);
        Assert.Equal(DashboardStatuts.Partiel, dashboard.StatutGlobal);
        Assert.Contains(dashboard.Avertissements, warning => warning.Code == "UAP2_STATUT_PARTIEL");
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("01-2026")]
    [InlineData("2026")]
    public async Task Get_PeriodeInvalide_RetourneBadRequest(string periode)
    {
        var response = await _client.GetAsync($"/api/v1/dashboard?periode={periode}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_SansPeriode_RetourneBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/dashboard");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("ResponsableFonderie")]
    [InlineData("ResponsableUsinage2")]
    public async Task Get_RoleMetierAutorise_RetourneOk(string role)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dashboard?periode=2026-01");
        request.Headers.Add("X-Development-Role", role);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_UtilisateurAuthentifieSansRoleAutorise_RetourneForbidden()
    {
        var tokenService = factory.Services.GetRequiredService<IDevelopmentTokenService>();
        var account = new DevelopmentAccount(
            99,
            Guid.Parse("de000000-0000-0000-0000-000000000099"),
            "lecteur",
            "non-utilise",
            "Lecteur métier",
            "lecteur@sopal.invalid",
            "Lecteur");
        var (token, _) = tokenService.Issue(account);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dashboard?periode=2026-01");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public void Modele_NAjouteAucuneMigrationDashboard()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();

        Assert.DoesNotContain(
            context.Database.GetMigrations(),
            migration => migration.Contains("Dashboard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Get_SansAuthentificationHorsDevelopment_RetourneUnauthorized()
    {
        using var productionFactory = new DashboardProductionFactory();
        using var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/api/v1/dashboard?periode=2026-01");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class DashboardProductionFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Authentication:DevelopmentMode", "false");
            builder.UseSetting(
                "Authentication:Jwt:Authority",
                "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0");
            builder.UseSetting("Authentication:Jwt:Audience", "api://bilan-matiere-uap2");
        }
    }

    private sealed class DashboardResponseFactory(DashboardResponse response) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDashboardService>();
                services.AddSingleton<IDashboardService>(new StubDashboardService(response));
            });
        }
    }

    private sealed class StubDashboardService(DashboardResponse response) : IDashboardService
    {
        public Task<DashboardResponse> GetAsync(
            string periode,
            CancellationToken cancellationToken = default)
            => Task.FromResult(response);
    }

    private static DashboardResponse CreerDashboardComplet()
        => new(
            "2026-01",
            new DateTime(2026, 1, 31, 10, 30, 0, DateTimeKind.Utc),
            DashboardStatuts.Ok,
            new DashboardBilanSummary("FONDERIE", 12.5m, "kg", 2, null, 2, 0, 0, 0, DashboardStatuts.Ok),
            new DashboardBilanSummary("USINAGE2", 8m, "pieces", 2, 2, 2, 0, 0, 0, DashboardStatuts.Ok),
            new DashboardBilanSummary("UAP2", 0m, "pieces", 2, 2, 2, 0, 0, 0, DashboardStatuts.Ok),
            new DashboardOperationsSummary(1, 2, 3, 4, null, 0),
            new DashboardImportSummary(5, 0, 0, 1, null),
            new DashboardDataQualitySummary(0, 0, 0, 0, DashboardStatuts.Ok),
            []);
}
