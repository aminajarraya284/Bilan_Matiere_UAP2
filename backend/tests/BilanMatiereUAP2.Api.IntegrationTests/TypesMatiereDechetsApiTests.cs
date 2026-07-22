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
        var type = await GetSeedAsync("M3000L0");
        var response = await CreateWasteAsync(type.Id, 125.5m);
        var created = await response.Content.ReadFromJsonAsync<DechetFonderieResponse>(JsonOptions);
        var list = await _client.GetFromJsonAsync<PagedDechetsFonderieResponse>(
            "/api/v1/dechets-fonderie?periode=2026-06");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(27, created!.SemaineIso);
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

    [Fact]
    public async Task DeleteDechet_HidesItFromStandardLists()
    {
        var type = await GetSeedAsync("M3000L0");
        var createResponse = await CreateWasteAsync(type.Id, 10m);
        var created = await createResponse.Content.ReadFromJsonAsync<DechetFonderieResponse>(JsonOptions);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/dechets-fonderie/{created!.Id}");
        var list = await _client.GetFromJsonAsync<PagedDechetsFonderieResponse>(
            "/api/v1/dechets-fonderie?periode=2026-06");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.DoesNotContain(list!.Items, item => item.Id == created.Id);
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

    private Task<HttpResponseMessage> CreateWasteAsync(Guid typeMatiereId, decimal weight, string kind = "Bavure")
    {
        return _client.PostAsJsonAsync(
            "/api/v1/dechets-fonderie",
            new CreateDechetFonderieRequest(
                new DateOnly(2026, 6, 30),
                typeMatiereId,
                kind,
                weight,
                "Test intégration"));
    }
}
