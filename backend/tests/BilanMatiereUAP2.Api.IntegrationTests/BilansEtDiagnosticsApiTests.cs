using System.Net;
using System.Net.Http.Json;
using BilanMatiereUAP2.Contracts.Bilans;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class BilansEtDiagnosticsApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetBilanUsinage2_PeriodeSansDonnees_RetourneListeVide()
    {
        var result = await _client.GetFromJsonAsync<BilanUsinage2Response>("/api/v1/bilans/usinage2/2026-07");

        Assert.Equal("2026-07", result!.Periode);
        Assert.Empty(result.Lignes);
        Assert.Equal(0, result.NombreLignesDonneesManquantes);
    }

    [Fact]
    public async Task GetBilanUsinage2_PeriodeInvalide_RetourneBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/bilans/usinage2/pas-une-periode");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBilanUsinage2Export_RetourneUnCsv()
    {
        var response = await _client.GetAsync("/api/v1/bilans/usinage2/2026-07/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("NumeroOF;CodeArticle", content);
    }

    [Fact]
    public async Task GetBilanUsinage2_AvecRoleNonAutorise_RetourneForbidden()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bilans/usinage2/2026-07");
        request.Headers.Add("X-Development-Role", "ResponsableFonderie");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPreparationBilans_RetourneLesDeuxBilans()
    {
        var response = await _client.GetAsync("/api/v1/diagnostics/preparation-bilans/2026-07");
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(json.TryGetProperty("bilanFonderie", out _));
        Assert.True(json.TryGetProperty("bilanUap2", out var uap2));
        // Le Bilan UAP2 ne doit jamais s'auto-activer tant que la regle CTRL n'est pas confirmee.
        Assert.False(uap2.GetProperty("formuleActivable").GetBoolean());
    }

    [Fact]
    public async Task GetPreparationBilans_PeriodeInvalide_RetourneBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/diagnostics/preparation-bilans/invalide");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetResolutionEntreeOf_AucunMouvement_RetourneMouvementAbsent()
    {
        var result = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>(
            "/api/v1/mouvements-entree-of/resolution/OF999999?annee=2026&mois=7");

        Assert.Equal("MouvementAbsent", result.GetProperty("statut").GetString());
    }
}
