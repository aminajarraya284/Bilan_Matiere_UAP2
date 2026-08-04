using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Contracts.TypesMatiere;

namespace BilanMatiereUAP2.Api.IntegrationTests;

/// <summary>
/// Couvre le modele Comptage (InventaireConteneur/InventaireCategorieComptage/InventaireFourComptage),
/// seul modele reellement consomme par le frontend (frontend/src/features/inventaire/inventaire.service.ts,
/// basePath = '/v1/inventaires-fonderie/comptage'). Le modele CRUD riche expose par ailleurs sur ce meme
/// controleur (recherche/pagination/synthese sur InventaireConteneurOf/InventaireCategorie/InventaireFour)
/// n'a aucun consommateur frontend a ce jour et n'est pas couvert ici : voir docs/ReglesMetier.md, RM-INV.
/// </summary>
[Collection(ApiTestCollection.Name)]
public sealed class InventaireComptageApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
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
    public async Task PostConteneur_ValidRequest_ReturnsCreatedAsBrouillon()
    {
        var type = await GetSeedAsync("M3000L0");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/inventaires-fonderie/comptage/conteneurs",
            new CreateInventaireComptageConteneurRequest(
                new DateOnly(2026, 7, 15), "C-100", "OF-100", "ART-1", "Piece test",
                type.Id, 12.5m, "Test integration"));
        var created = await response.Content.ReadFromJsonAsync<InventaireComptageConteneurResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Brouillon", created!.Statut);
        Assert.Equal("M3000L0", created.CodeTypeMatiere);
        Assert.Equal(12.5m, created.QuantiteKg);
    }

    [Fact]
    public async Task PostConteneur_NegativeQuantity_ReturnsValidationProblem()
    {
        var type = await GetSeedAsync("M3000L0");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/inventaires-fonderie/comptage/conteneurs",
            new CreateInventaireComptageConteneurRequest(
                new DateOnly(2026, 7, 15), "C-101", "OF-101", null, null,
                type.Id, -1m, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostConteneur_InactiveTypeMatiere_ReturnsConflict()
    {
        var type = await GetSeedAsync("M3010L0");
        var deactivated = await _client.PatchAsJsonAsync(
            $"/api/v1/types-matiere/{type.Id}/activation",
            new ChangeTypeMatiereActivationRequest(false));
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/inventaires-fonderie/comptage/conteneurs",
            new CreateInventaireComptageConteneurRequest(
                new DateOnly(2026, 7, 15), "C-102", "OF-102", null, null,
                type.Id, 5m, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ValiderConteneur_RecalculeLaProjectionEtEstVisibleDansLaPeriode()
    {
        var type = await GetSeedAsync("M3000L0");
        var conteneur = await CreateConteneurAsync(type.Id, "C-200", 10m, new DateOnly(2026, 7, 1));

        var validated = await _client.PostAsync(
            $"/api/v1/inventaires-fonderie/comptage/conteneurs/{conteneur.Id}/valider", null);
        var validatedBody = await validated.Content.ReadFromJsonAsync<InventaireComptageConteneurResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, validated.StatusCode);
        Assert.Equal("Valide", validatedBody!.Statut);

        var periode = await _client.GetFromJsonAsync<InventaireComptagePeriodeResponse>(
            "/api/v1/inventaires-fonderie/comptage/2026-07");
        var total = periode!.TotauxParTypeMatiere!.Single(t => t.CodeTypeMatiere == "M3000L0");
        Assert.Equal(10m, total.StockFinKg);
    }

    [Fact]
    public async Task AnnulerConteneur_SortDuStockMaisResteConsultable()
    {
        var type = await GetSeedAsync("M3000L0");
        var conteneur = await CreateConteneurAsync(type.Id, "C-201", 8m, new DateOnly(2026, 7, 1));
        await _client.PostAsync($"/api/v1/inventaires-fonderie/comptage/conteneurs/{conteneur.Id}/valider", null);

        var cancelled = await _client.PostAsync(
            $"/api/v1/inventaires-fonderie/comptage/conteneurs/{conteneur.Id}/annuler", null);
        var cancelledBody = await cancelled.Content.ReadFromJsonAsync<InventaireComptageConteneurResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal("Annule", cancelledBody!.Statut);

        var periode = await _client.GetFromJsonAsync<InventaireComptagePeriodeResponse>(
            "/api/v1/inventaires-fonderie/comptage/2026-07");
        var total = periode!.TotauxParTypeMatiere!.Single(t => t.CodeTypeMatiere == "M3000L0");
        Assert.Equal(0m, total.StockFinKg);
        // La ligne annulee reste consultable dans le detail de la periode (tracabilite).
        Assert.Contains(periode.Conteneurs, item => item.Id == conteneur.Id && item.Statut == "Annule");
    }

    [Fact]
    public async Task GetPeriode_RependLeStockFinDuMoisPrecedentCommeStockDebut()
    {
        var type = await GetSeedAsync("M3060L0");
        var aout = await CreateConteneurAsync(type.Id, "C-300", 20m, new DateOnly(2026, 8, 1));
        await _client.PostAsync($"/api/v1/inventaires-fonderie/comptage/conteneurs/{aout.Id}/valider", null);

        var septembre = await _client.GetFromJsonAsync<InventaireComptagePeriodeResponse>(
            "/api/v1/inventaires-fonderie/comptage/2026-09");

        var total = septembre!.TotauxParTypeMatiere!.Single(t => t.CodeTypeMatiere == "M3060L0");
        Assert.Equal(20m, total.StockDebutKg);
        Assert.DoesNotContain("STOCK_DEBUT_INTROUVABLE", total.Anomalies);
    }

    [Fact]
    public async Task GetPeriode_SansHistorique_SignaleStockDebutIntrouvable()
    {
        var response = await _client.GetFromJsonAsync<InventaireComptagePeriodeResponse>(
            "/api/v1/inventaires-fonderie/comptage/2030-01");

        var total = response!.TotauxParTypeMatiere!.First();
        Assert.Null(total.StockDebutKg);
        Assert.Contains("STOCK_DEBUT_INTROUVABLE", total.Anomalies);
    }

    [Fact]
    public async Task ResponsableUsinage2_CannotReadOrManageInventaire()
    {
        using var readRequest = new HttpRequestMessage(
            HttpMethod.Get, "/api/v1/inventaires-fonderie/comptage/2026-07");
        readRequest.Headers.Add("X-Development-Role", "ResponsableUsinage2");
        var readResponse = await _client.SendAsync(readRequest);
        Assert.Equal(HttpStatusCode.Forbidden, readResponse.StatusCode);

        var type = await GetSeedAsync("M3000L0");
        using var writeRequest = new HttpRequestMessage(
            HttpMethod.Post, "/api/v1/inventaires-fonderie/comptage/conteneurs")
        {
            Content = JsonContent.Create(new CreateInventaireComptageConteneurRequest(
                new DateOnly(2026, 7, 15), "C-400", null, null, null, type.Id, 1m, null))
        };
        writeRequest.Headers.Add("X-Development-Role", "ResponsableUsinage2");
        var writeResponse = await _client.SendAsync(writeRequest);
        Assert.Equal(HttpStatusCode.Forbidden, writeResponse.StatusCode);
    }

    private async Task<TypeMatiereResponse> GetSeedAsync(string code)
    {
        var items = await _client.GetFromJsonAsync<List<TypeMatiereResponse>>("/api/v1/types-matiere?actif=true");
        return items!.Single(item => item.Code == code);
    }

    private async Task<InventaireComptageConteneurResponse> CreateConteneurAsync(
        Guid typeMatiereId,
        string numeroConteneur,
        decimal quantiteKg,
        DateOnly dateInventaire)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/inventaires-fonderie/comptage/conteneurs",
            new CreateInventaireComptageConteneurRequest(
                dateInventaire, numeroConteneur, null, null, null, typeMatiereId, quantiteKg, null));
        return (await response.Content.ReadFromJsonAsync<InventaireComptageConteneurResponse>(JsonOptions))!;
    }
}
