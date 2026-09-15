using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BilanMatiereUAP2.Contracts.DechetsFonderie;
using BilanMatiereUAP2.Contracts.TypesMatiere;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class TypesMatiereDechetsApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
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
    public async Task GetTypesMatiere_ReturnsTheThreeSeeds()
    {
        var items = await _client.GetFromJsonAsync<List<TypeMatiereResponse>>("/api/v1/types-matiere?actif=true");

        Assert.Equal(new[] { "M3000L0", "M3010L0", "M3060L0" }, items!.Select(item => item.Code));
    }

    [Fact]
    public async Task PostTypeMatiere_ValidRequest_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/types-matiere",
            new CreateTypeMatiereRequest(" test-01 ", "Matière de test"));
        var created = await response.Content.ReadFromJsonAsync<TypeMatiereResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("TEST-01", created!.Code);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task PostTypeMatiere_Duplicate_ReturnsConflict()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/types-matiere",
            new CreateTypeMatiereRequest("m3000l0", "Doublon"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostDechet_ValidRequest_ReturnsCreatedAndCanBeListed()
    {
        // Ce test porte sur la CREATION seule : il n'utilise pas le helper qui valide ensuite.
        var type = await GetSeedAsync("M3000L0");
        var response = await _client.PostAsJsonAsync(
            "/api/v1/dechets-fonderie",
            new CreateDechetFonderieRequest(new DateOnly(2026, 6, 30), type.Id, "Bavure", 125.5m, "Test intégration"));
        var created = await response.Content.ReadFromJsonAsync<DechetFonderieResponse>(JsonOptions);
        var list = await _client.GetFromJsonAsync<PagedDechetsFonderieResponse>(
            "/api/v1/dechets-fonderie?periode=2026-06");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Brouillon", created!.Statut);
        Assert.Equal(27, created.SemaineIso);
        Assert.Contains(list!.Items, item => item.Id == created.Id);
    }

    [Fact]
    public async Task PostDechet_ZeroWeight_ReturnsValidationProblem()
    {
        var type = await GetSeedAsync("M3000L0");
        var response = await CreateWasteAsync(type.Id, 0);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("poidsKg", payload);
    }

    // Un dechet annule reste TRACABLE dans l'historique : il n'est jamais supprime
    // physiquement ni masque. Il sort en revanche des totaux du resume.
    [Fact]
    public async Task DeleteDechet_LeConserveDansLHistoriqueEnAnnule()
    {
        var type = await GetSeedAsync("M3000L0");
        var createResponse = await CreateWasteAsync(type.Id, 10m);
        var created = await createResponse.Content.ReadFromJsonAsync<DechetFonderieResponse>(JsonOptions);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/dechets-fonderie/{created!.Id}");
        var list = await _client.GetFromJsonAsync<PagedDechetsFonderieResponse>(
            "/api/v1/dechets-fonderie?periode=2026-06");
        var resume = await _client.GetFromJsonAsync<ResumeDechetsFonderieResponse>(
            "/api/v1/dechets-fonderie/resume?periode=2026-06");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var conserve = Assert.Single(list!.Items, item => item.Id == created.Id);
        Assert.Equal("Annule", conserve.Statut);
        Assert.Equal(0m, resume!.TotalGeneralKg);
    }

    [Fact]
    public async Task Resume_AggregatesWasteAndExcludesDeletedRows()
    {
        var type = await GetSeedAsync("M3010L0");
        await CreateWasteAsync(type.Id, 10m, "Bavure");
        var deletedResponse = await CreateWasteAsync(type.Id, 99m, "Bavure");
        var deleted = await deletedResponse.Content.ReadFromJsonAsync<DechetFonderieResponse>(JsonOptions);
        await _client.DeleteAsync($"/api/v1/dechets-fonderie/{deleted!.Id}");
        await CreateWasteAsync(type.Id, 5m, "Poudre");

        var resume = await _client.GetFromJsonAsync<ResumeDechetsFonderieResponse>(
            "/api/v1/dechets-fonderie/resume?periode=2026-06");

        Assert.Equal(15m, resume!.TotalGeneralKg);
        var material = Assert.Single(resume.TotauxParTypeMatiere);
        Assert.Equal(10m, material.TotalBavureKg);
        Assert.Equal(5m, material.TotalPoudreKg);
    }

    [Fact]
    public async Task GetResumeDechetsFonderie_ReturnsAggregatedTotalsWithoutTranslationError()
    {
        var m3000 = await GetSeedAsync("M3000L0");
        var m3010 = await GetSeedAsync("M3010L0");
        await CreateWasteAsync(m3000.Id, 2.500m, "Poudre", new DateOnly(2026, 6, 15));
        await CreateWasteAsync(m3010.Id, 99m, "Bavure", new DateOnly(2026, 5, 31));
        await CreateWasteAsync(m3010.Id, 10.125m, "Bavure", new DateOnly(2026, 6, 1));
        await CreateWasteAsync(m3010.Id, 1.375m, "Bavure", new DateOnly(2026, 6, 30));
        await CreateWasteAsync(m3010.Id, 5.250m, "Poudre", new DateOnly(2026, 6, 30));
        await CreateWasteAsync(m3010.Id, 88m, "Poudre", new DateOnly(2026, 7, 1));
        var deletedResponse = await CreateWasteAsync(
            m3010.Id,
            77m,
            "Bavure",
            new DateOnly(2026, 6, 20));
        var deleted = await deletedResponse.Content.ReadFromJsonAsync<DechetFonderieResponse>(JsonOptions);
        await _client.DeleteAsync($"/api/v1/dechets-fonderie/{deleted!.Id}");

        var response = await _client.GetAsync("/api/v1/dechets-fonderie/resume?periode=2026-06");
        var resume = await response.Content.ReadFromJsonAsync<ResumeDechetsFonderieResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(resume);
        Assert.Equal(19.250m, resume.TotalGeneralKg);
        Assert.Collection(
            resume.TotauxParTypeMatiere,
            first =>
            {
                Assert.Equal("M3000L0", first.TypeMatiereCode);
                Assert.Equal(0m, first.TotalBavureKg);
                Assert.Equal(2.500m, first.TotalPoudreKg);
            },
            second =>
            {
                Assert.Equal("M3010L0", second.TypeMatiereCode);
                Assert.Equal(11.500m, second.TotalBavureKg);
                Assert.Equal(5.250m, second.TotalPoudreKg);
            });
    }

    [Fact]
    public async Task GetResumeDechetsFonderie_EmptyPeriod_ReturnsEmptyTotals()
    {
        var response = await _client.GetAsync("/api/v1/dechets-fonderie/resume?periode=2030-01");
        var resume = await response.Content.ReadFromJsonAsync<ResumeDechetsFonderieResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(resume);
        Assert.Empty(resume.TotauxParTypeMatiere);
        Assert.Equal(0m, resume.TotalGeneralKg);
    }

    [Fact]
    public async Task ResponsableUsinage2_CannotReadWaste()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dechets-fonderie?periode=2026-06");
        request.Headers.Add("X-Development-Role", "ResponsableUsinage2");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<TypeMatiereResponse> GetSeedAsync(string code)
    {
        var items = await _client.GetFromJsonAsync<List<TypeMatiereResponse>>("/api/v1/types-matiere?actif=true");
        return items!.Single(item => item.Code == code);
    }

    /// <summary>
    /// Cree un dechet puis le VALIDE : depuis l'introduction du cycle de vie, la creation
    /// produit un Brouillon, exclu des totaux. Seuls les dechets valides alimentent le resume.
    /// </summary>
    private async Task<HttpResponseMessage> CreateWasteAsync(
        Guid typeMatiereId,
        decimal weight,
        string kind = "Bavure",
        DateOnly? date = null)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/dechets-fonderie",
            new CreateDechetFonderieRequest(
                date ?? new DateOnly(2026, 6, 30),
                typeMatiereId,
                kind,
                weight,
                "Test intégration"));
        if (!response.IsSuccessStatusCode) return response;

        var created = await response.Content.ReadFromJsonAsync<DechetFonderieResponse>(JsonOptions);
        return await _client.PostAsync($"/api/v1/dechets-fonderie/{created!.Id}/valider", null);
    }
}
