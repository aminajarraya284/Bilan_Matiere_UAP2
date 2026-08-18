using System.Net;
using System.Net.Http.Json;
using System.Text;
using BilanMatiereUAP2.Application.Features.Alertes;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Contracts.Rapports;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class RapportsApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_CatalogueAdmin_RetourneLesHuitRapportsReelsEtCsvSeulement()
    {
        var response = await _client.GetAsync("/api/v1/rapports/catalogue");
        var items = await response.Content.ReadFromJsonAsync<RapportCatalogueItemResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(items);
        Assert.Equal(8, items!.Length);
        Assert.All(items, item => Assert.Equal(new[] { "CSV" }, item.Formats));
        Assert.All(items, item => Assert.True(item.Disponible));
    }

    [Fact]
    public async Task Get_CatalogueResponsableFonderie_NExposePasLesRapportsU2()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rapports/catalogue");
        request.Headers.Add("X-Development-Role", "ResponsableFonderie");

        var response = await _client.SendAsync(request);
        var items = await response.Content.ReadFromJsonAsync<RapportCatalogueItemResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(items);
        Assert.DoesNotContain(items!, item => item.Code is CodesRapports.BilanUsinage2 or CodesRapports.BilanUap2 or CodesRapports.ReceptionsNonReceptionnees);
        Assert.Contains(items!, item => item.Code == CodesRapports.BilanFonderie);
    }

    [Fact]
    public async Task Get_Bilans_ConserveLesUnitesEtLesDonneesManquantes()
    {
        var fonderieResponse = await _client.GetAsync("/api/v1/rapports/fonderie/2026-01");
        var u2Response = await _client.GetAsync("/api/v1/rapports/usinage2/2026-01");
        var uap2Response = await _client.GetAsync("/api/v1/rapports/uap2/2026-01");

        var fonderie = await fonderieResponse.Content.ReadFromJsonAsync<RapportResponse<BilanFonderieResponse>>();
        var u2 = await u2Response.Content.ReadFromJsonAsync<RapportResponse<BilanUsinage2Response>>();
        var uap2 = await uap2Response.Content.ReadFromJsonAsync<RapportResponse<BilanUap2Response>>();

        Assert.Equal(HttpStatusCode.OK, fonderieResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, u2Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uap2Response.StatusCode);
        Assert.Equal("kg", fonderie!.Metadata.UnitePrincipale);
        Assert.Equal("pieces", u2!.Metadata.UnitePrincipale);
        Assert.Equal("pieces", uap2!.Metadata.UnitePrincipale);
        Assert.Equal("2026-01", fonderie.Donnees.Periode);
    }

    [Fact]
    public async Task Get_InventaireEtImports_RetourneDesProjectionsSqlPaginees()
    {
        await AjouterImportAsync();

        var inventaireResponse = await _client.GetAsync("/api/v1/rapports/inventaire/2026-01");
        var importsResponse = await _client.GetAsync("/api/v1/rapports/imports?page=1&pageSize=10");

        var inventaire = await inventaireResponse.Content.ReadFromJsonAsync<RapportResponse<InventaireSyntheseDto>>();
        var imports = await importsResponse.Content.ReadFromJsonAsync<RapportResponse<PagedRapportImportsResponse>>();

        Assert.Equal(HttpStatusCode.OK, inventaireResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, importsResponse.StatusCode);
        Assert.NotNull(inventaire);
        Assert.NotNull(imports);
        Assert.Equal(1, imports!.Donnees.Page);
        Assert.Equal(10, imports.Donnees.PageSize);
        var import = Assert.Single(imports.Donnees.Items);
        Assert.Equal(TypeImport.RapportMachine.ToString(), import.TypeImport);
        Assert.Equal(10, import.LignesLues);
        Assert.Equal(2, import.LignesInserees);
        Assert.Equal(5, import.LignesMisesAJour);
        Assert.Equal(7, import.LignesImportees);
        Assert.Equal(2, import.LignesRejetees);
    }

    [Fact]
    public async Task Get_DetailOfAlertesEtNonReceptions_RetourneLesSectionsReelles()
    {
        var ofResponse = await _client.GetAsync("/api/v1/rapports/of/OF-ABSENT?periode=2026-01");
        var alertesResponse = await _client.GetAsync("/api/v1/rapports/alertes?periode=2026-01&page=1&pageSize=10");
        var nonReceptionsResponse = await _client.GetAsync("/api/v1/rapports/receptions-non-receptionnees?date=2026-01-15&page=1&pageSize=10");

        var of = await ofResponse.Content.ReadFromJsonAsync<RapportOfDetailResponse>();

        Assert.Equal(HttpStatusCode.OK, ofResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, alertesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, nonReceptionsResponse.StatusCode);
        Assert.NotNull(of);
        Assert.Equal("OF-ABSENT", of!.Contexte.NumeroOF);
        Assert.True(of.Metadata.EstPartiel);
        Assert.NotEmpty(of.DonneesManquantes);
    }

    [Theory]
    [InlineData("2026")]
    [InlineData("2026-13")]
    [InlineData("01-2026")]
    public async Task Get_PeriodeInvalide_RetourneBadRequest(string periode)
    {
        var response = await _client.GetAsync($"/api/v1/rapports/fonderie/{periode}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_RapportHorsPerimetre_RetourneForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rapports/usinage2/2026-01");
        request.Headers.Add("X-Development-Role", "ResponsableFonderie");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExportCsv_EmetBomSepareParPointVirguleEtNeutraliseLesFormules()
    {
        await AjouterAlerteAsync();

        var response = await _client.GetAsync("/api/v1/rapports/RPT-005/export?periode=2026-01");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var csv = Encoding.UTF8.GetString(bytes);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.True(bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        Assert.Contains(';', csv);
        Assert.Contains("'=2+2", csv);
        Assert.EndsWith(".csv", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }

    [Fact]
    public void Modele_NAjouteAucuneMigrationRapports()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();

        Assert.DoesNotContain(context.Database.GetMigrations(), item => item.Contains("AddReports", StringComparison.OrdinalIgnoreCase) || item.Contains("AddRapports", StringComparison.OrdinalIgnoreCase));
    }

    private async Task AjouterAlerteAsync()
    {
        var alert = AlerteMetier.Creer(
            CodesAlertes.U2QuantiteNonReceptionnee,
            TypeAlerte.AnomalieMetier,
            SeveriteAlerte.Avertissement,
            "=2+2",
            "Donnee de test export.",
            SourcesAlertes.BilanUsinage2,
            "2026-01",
            "OF-CSV",
            "ART-CSV",
            null,
            "CSV-KEY",
            false,
            null,
            new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        context.AlertesMetier.Add(alert);
        await context.SaveChangesAsync();
    }

    private async Task AjouterImportAsync()
    {
        var debut = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var batch = ImportBatch.Demarrer(
            new string('A', 64),
            TypeImport.RapportMachine,
            "YRJMCH_V1",
            null,
            "Admin Test",
            debut);
        batch.TerminerImport(
            StatutImport.Partiel,
            debut.AddMinutes(1),
            readCount: 10,
            validCount: 8,
            insertedCount: 2,
            updatedCount: 5,
            unchangedCount: 1,
            rejectedCount: 2,
            nombreAnomalies: 2);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        context.ImportBatches.Add(batch);
        await context.SaveChangesAsync();
    }
}
