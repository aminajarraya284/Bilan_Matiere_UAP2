using System.Net;
using System.Net.Http.Json;
using BilanMatiereUAP2.Contracts.Bilans;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class BilanUap2ApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_PeriodeVide_RetourneUnBilanVideEtLeSeuilApplique()
    {
        var response = await _client.GetFromJsonAsync<BilanUap2Response>("/api/v1/bilans/uap2/2026-07");

        Assert.Empty(response!.Lignes);
        Assert.Equal(0, response.NombreLignes);
        Assert.True(response.SeuilEcartPourcent > 0m);
    }

    [Fact]
    public async Task Get_PeriodeInvalide_RetourneBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/bilans/uap2/07-2026");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_SansPeriode_UtiliseLeMoisCourant()
    {
        var response = await _client.GetFromJsonAsync<BilanUap2Response>("/api/v1/bilans/uap2");

        Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM"), response!.Periode);
    }

    [Fact]
    public async Task ResponsableFonderie_NePeutPasLireLeBilanUap2()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bilans/uap2/2026-07");
        request.Headers.Add("X-Development-Role", "ResponsableFonderie");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
