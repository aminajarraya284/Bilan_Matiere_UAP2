using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Contracts.DechetsFonderie;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Contracts.TypesMatiere;

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
