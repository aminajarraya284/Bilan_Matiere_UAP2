using System.Net;
using System.Net.Http.Json;
using BilanMatiereUAP2.Contracts.Archivage;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class PeriodesBilanApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Admin_ArchivePuisRelitLeSnapshotSansRecalcul()
    {
        var creation = await _client.PostAsync("/api/v1/periodes-bilan/2026-07/enregistrer", null);
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var creee = await creation.Content.ReadFromJsonAsync<PeriodeBilanArchiveDetailResponse>();
        Assert.NotNull(creee);
        Assert.Equal("2026-07", creee!.Snapshot.Periode);
        Assert.Equal(1, creee.Snapshot.SchemaVersion);

        var lecture = await _client.GetFromJsonAsync<PeriodeBilanArchiveDetailResponse>(
            "/api/v1/periodes-bilan/2026-07");
        Assert.NotNull(lecture);
        Assert.Equal(creee.Snapshot.Fonderie.TotalEcartKg, lecture!.Snapshot.Fonderie.TotalEcartKg);
        Assert.Equal(creee.Snapshot.Usinage2.TotalBilanUsinage2Pieces, lecture.Snapshot.Usinage2.TotalBilanUsinage2Pieces);
        Assert.Equal(creee.Snapshot.Uap2.TotalEcartUap2Pieces, lecture.Snapshot.Uap2.TotalEcartUap2Pieces);
    }

    [Fact]
    public async Task DeuxiemeCloture_Retourne409EtConserveArchiveInitiale()
    {
        var premiere = await _client.PostAsync("/api/v1/periodes-bilan/2026-07/enregistrer", null);
        var initiale = await premiere.Content.ReadFromJsonAsync<PeriodeBilanArchiveDetailResponse>();

        var duplicate = await _client.PostAsync("/api/v1/periodes-bilan/2026-07/enregistrer", null);
        var relue = await _client.GetFromJsonAsync<PeriodeBilanArchiveDetailResponse>(
            "/api/v1/periodes-bilan/2026-07");

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(initiale!.Metadata.Id, relue!.Metadata.Id);
        Assert.Equal(initiale.Snapshot.DateEnregistrementUtc, relue.Snapshot.DateEnregistrementUtc);
    }

    [Fact]
    public async Task Rapport_VientDuSnapshotArchive()
    {
        await _client.PostAsync("/api/v1/periodes-bilan/2026-07/enregistrer", null);
        var response = await _client.GetAsync("/api/v1/periodes-bilan/2026-07/rapport");
        var contenu = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/markdown", response.Content.Headers.ContentType!.ToString());
        Assert.Contains("2026-07", contenu);
        Assert.Contains("snapshot SQL", contenu);
    }

    [Theory]
    [InlineData("ResponsableFonderie")]
    [InlineData("ResponsableUsinage2")]
    public async Task Archivage_EstInterditAuxRolesResponsables(string role)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/periodes-bilan");
        request.Headers.Add("X-Development-Role", role);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
