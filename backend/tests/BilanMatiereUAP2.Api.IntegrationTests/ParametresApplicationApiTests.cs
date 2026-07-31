using System.Net;
using System.Net.Http.Json;
using BilanMatiereUAP2.Contracts.Parametres;
using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class ParametresApplicationApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetParametres_ReturnsBusinessParametersWithEditabilityMetadata()
    {
        var response = await _client.GetAsync("/api/parametres");
        var items = await response.Content.ReadFromJsonAsync<List<ParametreApplicationResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(items);
        Assert.Contains(items, item => item.Cle == ParametresApplicationCles.SeuilEcartFonderie && item.Valeur == "5.0");
        Assert.Contains(items, item => item.Cle == ParametresApplicationCles.SeuilEcartUap2 && item.Valeur == "1.5");
        Assert.Contains(items, item => item.Cle == ParametresApplicationCles.CodeAtelierUap2 && item.Valeur == "16");
        Assert.Contains(items, item => item.Cle == ParametresApplicationCles.TransactionEntreeOf);
        Assert.Contains(items, item => item.Cle == ParametresApplicationCles.TypesMatiere && !item.Modifiable);
    }

    [Fact]
    public async Task GetParametres_VersionedRoute_RemainsAvailable()
    {
        var response = await _client.GetAsync("/api/v1/parametres");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutParametre_UpdatesValidatedValue()
    {
        _client.DefaultRequestHeaders.Add("X-Development-Role", "Admin");

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/parametres/{ParametresApplicationCles.SeuilEcartFonderie}",
            new UpdateParametreApplicationRequest("6.25"));
        var item = await response.Content.ReadFromJsonAsync<ParametreApplicationResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(item);
        Assert.Equal("6.25", item.Valeur);
    }

    [Fact]
    public async Task PutTypesMatiereCsv_IsRejectedBecauseDedicatedReferenceIsAuthoritative()
    {
        _client.DefaultRequestHeaders.Add("X-Development-Role", "Admin");

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/parametres/{ParametresApplicationCles.TypesMatiere}",
            new UpdateParametreApplicationRequest("M9999"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
