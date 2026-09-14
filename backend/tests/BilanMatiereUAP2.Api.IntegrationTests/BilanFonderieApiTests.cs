using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Contracts.DechetsFonderie;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Contracts.TypesMatiere;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BilanMatiereUAP2.Api.IntegrationTests;

/// <summary>
/// Bilan Fonderie de bout en bout (RM-FON-003). Les stocks proviennent de la projection
/// alimentee par le comptage d'inventaire : ces tests valident donc aussi le chainage
/// entre la verticale Inventaire et le bilan.
/// </summary>
[Collection(ApiTestCollection.Name)]
public sealed class BilanFonderieApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_PeriodeVide_RetourneUneLigneParTypeActifSansEcartCalculable()
    {
        var response = await _client.GetFromJsonAsync<BilanFonderieResponse>("/api/v1/bilans/fonderie/2026-07");

        Assert.NotEmpty(response!.Lignes);
        var ligne = response.Lignes[0];
        // Aucune projection d'inventaire : le stock de fin est inconnu, pas nul.
        Assert.Null(ligne.StockFinKg);
        Assert.Null(ligne.OutputKg);
        Assert.Null(ligne.EcartKg);
        Assert.Equal("Incomplet", ligne.Statut);
        Assert.Null(response.TotalEcartKg);
    }

    [Fact]
    public async Task Get_PeriodeInvalide_RetourneBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/bilans/fonderie/07-2026");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ApresInventaireEtDechets_ChaineLesSourcesReelles()
    {
        var type = await GetTypeAsync("M3000L0");

        // Stock de fin via le comptage d'inventaire (RM-INV-004).
        var conteneur = await _client.PostAsJsonAsync(
            "/api/v1/inventaires-fonderie/comptage/conteneurs",
            new CreateInventaireComptageConteneurRequest(
                new DateOnly(2026, 7, 10), "C-900", null, null, null, type.Id, 200m, null));
        var cree = await conteneur.Content.ReadFromJsonAsync<InventaireComptageConteneurResponse>(JsonOptions);
        await _client.PostAsync(
            $"/api/v1/inventaires-fonderie/comptage/conteneurs/{cree!.Id}/valider", null);

        // Dechet valide de la meme periode.
        var dechet = await _client.PostAsJsonAsync(
            "/api/v1/dechets-fonderie",
            new CreateDechetFonderieRequest(
                new DateOnly(2026, 7, 12), type.Id, "Bavure", 30m, null, "Fonderie"));
        var dechetCree = await dechet.Content.ReadFromJsonAsync<DechetFonderieResponse>(JsonOptions);
        await _client.PostAsync($"/api/v1/dechets-fonderie/{dechetCree!.Id}/valider", null);

        var response = await _client.GetFromJsonAsync<BilanFonderieResponse>("/api/v1/bilans/fonderie/2026-07");

        var ligne = response!.Lignes.Single(l => l.CodeTypeMatiere == "M3000L0");
        Assert.Equal(200m, ligne.StockFinKg);
        Assert.Equal(30m, ligne.DechetsFonderieKg);
        // Premiere periode : pas de stock de debut, donc pas d'ecart calculable.
        Assert.Null(ligne.StockDebutKg);
        Assert.Null(ligne.EcartKg);
        Assert.Contains("STOCK_DEBUT_INDISPONIBLE", ligne.Anomalies);
    }

    [Fact]
    public async Task Get_SurDeuxPeriodes_CalculeUnEcartCompletGraceAuChainage()
    {
        var type = await GetTypeAsync("M3060L0");
        await CreerEtValiderConteneurAsync(type.Id, "C-901", 150m, new DateOnly(2026, 8, 5));
        // La periode suivante herite du stock de fin d'aout comme stock de debut (RM-INV-005).
        await CreerEtValiderConteneurAsync(type.Id, "C-902", 120m, new DateOnly(2026, 9, 5));

        var response = await _client.GetFromJsonAsync<BilanFonderieResponse>("/api/v1/bilans/fonderie/2026-09");

        var ligne = response!.Lignes.Single(l => l.CodeTypeMatiere == "M3060L0");
        Assert.Equal(150m, ligne.StockDebutKg);
        Assert.Equal(120m, ligne.StockFinKg);
        Assert.Equal(150m, ligne.InputKg);
        Assert.Equal(120m, ligne.OutputKg);
        Assert.Equal(30m, ligne.EcartKg);
        Assert.DoesNotContain("STOCK_DEBUT_INDISPONIBLE", ligne.Anomalies);
    }

    [Fact]
    public async Task ResponsableUsinage2_NePeutPasLireLeBilanFonderie()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bilans/fonderie/2026-07");
        request.Headers.Add("X-Development-Role", "ResponsableUsinage2");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?dateDebut=2026-07-10")]
    [InlineData("?dateFin=2026-07-20")]
    [InlineData("?dateDebut=&dateFin=2026-07-20")]
    [InlineData("?dateDebut=2026-07-21&dateFin=2026-07-20")]
    [InlineData("?dateDebut=10-07-2026&dateFin=2026-07-20")]
    [InlineData("?dateDebut=2026-02-30&dateFin=2026-07-20")]
    [InlineData("?dateDebut=2026-07-10&dateFin=2026-07-32")]
    [InlineData("?dateDebut=2026-07-10&dateFin=2026-7-20")]
    public async Task Intervalle_DatesInvalidesOuAbsentes_Retourne400(string query)
    {
        var response = await _client.GetAsync("/api/v1/bilans/fonderie" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("2026-07-10", "2026-07-20", 45, 3)]
    [InlineData("2026-07-10", "2026-07-15", 25, 2)]
    [InlineData("2026-07-15", "2026-07-20", 35, 2)]
    [InlineData("2026-07-20", "2026-07-20", 20, 1)]
    public async Task Intervalle_FiltreLesTroisFluxEtLesAnomaliesEnSQL_BornesInclusives(
        string debut, string fin, int somme, int nombre)
    {
        var type = await GetTypeAsync("M3000L0");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
            var maintenant = DateTime.UtcNow;
            var batch = ImportX3Batch.Demarrer(new string('A', 64), null, "Test intervalle", maintenant);
            var ordre = X3OrdreFabrication.Creer("OF-DATES", batch.Id, maintenant);
            ordre.MettreAJourContexte("ART-DATES", "Article dates", null, "16", null, null, batch.Id, maintenant);
            db.ImportX3Batches.Add(batch);
            db.OrdresFabrication.Add(ordre);
            foreach (var jour in new[] { 9, 10, 15, 20, 21 })
            {
                var date = new DateOnly(2026, 7, jour);
                db.ConsommationsComposants.Add(ConsommationComposant.Creer(
                    batch.Id, date, ordre, type.Code, null, "KG", -jour, maintenant));
                var dechet = DechetFonderie.Creer(date, type.Id, TypeDechet.Bavure, jour, null, null, "Test", maintenant);
                dechet.Valider(null, "Test", maintenant);
                db.DechetsFonderie.Add(dechet);
                var snapshot = new ReceptionUsinage2.ContexteSnapshot(
                    "ART-DATES", null, null, "16", type.Code, null, null, null, null, null,
                    null, null, null, null, null, null,
                    "Trouve", "Trouve", "Trouve", "Trouve", "PoidsPhysiqueManquant", "Trouve", []);
                var reception = ReceptionUsinage2.Creer("OF-DATES", date, 100m, jour,
                    null, null, snapshot, null, "Test", maintenant);
                reception.Valider(false, null, null, "Test", maintenant);
                db.ReceptionsUsinage2.Add(reception);
                var sansType = ReceptionUsinage2.Creer("OF-SANS-TYPE", date, 10m, 1m,
                    null, null, snapshot with { CodeTypeMatiere = null }, null, "Test", maintenant);
                sansType.Valider(false, null, null, "Test", maintenant);
                db.ReceptionsUsinage2.Add(sansType);
            }
            await db.SaveChangesAsync();
        }

        var response = await _client.GetFromJsonAsync<BilanFonderieIntervalleResponse>(
            $"/api/v1/bilans/fonderie?dateDebut={debut}&dateFin={fin}");
        var ligne = response!.Bilan.Lignes.Single(l => l.CodeTypeMatiere == type.Code);
        Assert.Equal((decimal)somme, ligne.SortiesMagasinX3Kg);
        Assert.Equal((decimal)somme, ligne.DechetsFonderieKg);
        Assert.Equal((decimal)somme, ligne.ReceptionsUsinage2Kg);
        Assert.Contains($"RECEPTIONS_U2_TYPE_MATIERE_NON_RESOLU:{nombre}", ligne.Anomalies);
        Assert.Null(ligne.StockDebutKg);
        Assert.Null(ligne.StockFinKg);
        Assert.Null(ligne.EcartKg);
    }

    [Fact]
    public async Task Intervalle_MoisNaturel_ConserveLeCalculMensuel()
    {
        var type = await GetTypeAsync("M3000L0");
        await CreerEtValiderConteneurAsync(type.Id, "C-JUIN", 150m, new DateOnly(2026, 6, 15));
        await CreerEtValiderConteneurAsync(type.Id, "C-JUILLET", 120m, new DateOnly(2026, 7, 15));
        var mensuel = (await _client.GetFromJsonAsync<BilanFonderieResponse>("/api/v1/bilans/fonderie/2026-07"))!;
        var intervalle = (await _client.GetFromJsonAsync<BilanFonderieIntervalleResponse>(
            "/api/v1/bilans/fonderie?dateDebut=2026-07-01&dateFin=2026-07-31"))!;
        var normalise = intervalle.Bilan with {
            Periode = mensuel.Periode,
            Lignes = intervalle.Bilan.Lignes.Select(l => l with { Periode = mensuel.Periode }).ToArray()
        };
        Assert.Equal(JsonSerializer.Serialize(mensuel, JsonOptions), JsonSerializer.Serialize(normalise, JsonOptions));
        Assert.Equal(30m, mensuel.Lignes.Single(l => l.CodeTypeMatiere == type.Code).EcartKg);
    }

    private async Task<TypeMatiereResponse> GetTypeAsync(string code)
    {
        var items = await _client.GetFromJsonAsync<List<TypeMatiereResponse>>("/api/v1/types-matiere?actif=true");
        return items!.Single(item => item.Code == code);
    }

    private async Task CreerEtValiderConteneurAsync(
        Guid typeMatiereId,
        string numeroConteneur,
        decimal quantiteKg,
        DateOnly dateInventaire)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/inventaires-fonderie/comptage/conteneurs",
            new CreateInventaireComptageConteneurRequest(
                dateInventaire, numeroConteneur, null, null, null, typeMatiereId, quantiteKg, null));
        var cree = await response.Content.ReadFromJsonAsync<InventaireComptageConteneurResponse>(JsonOptions);
        await _client.PostAsync($"/api/v1/inventaires-fonderie/comptage/conteneurs/{cree!.Id}/valider", null);
    }
}
