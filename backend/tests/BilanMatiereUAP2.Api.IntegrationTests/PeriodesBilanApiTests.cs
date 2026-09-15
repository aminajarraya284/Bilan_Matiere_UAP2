using System.Net;
using System.Net.Http.Json;
using BilanMatiereUAP2.Contracts.Archivage;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task Archive_ConserveLesReceptionsAuDelaDeLaPremierePage()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
            var snapshot = new ReceptionUsinage2.ContexteSnapshot(
                "ART1", "Article test", "En cours", "16", "M3010L0", 2m, "KG", 2m, 2m,
                null, 0m, 10m, 0m, 0m, 0m, 0m,
                "Trouve", "Trouve", "Trouve", "Trouve", "Trouve", "Trouve", [],
                "SortieMp", EligibleAtelier16: true);
            foreach (var index in Enumerable.Range(1, 51))
            {
                var reception = ReceptionUsinage2.Creer($"OF{index:D4}", new DateOnly(2026, 7, 5),
                    10m, 20m, "FONDERIE", null, snapshot, null, "Test", DateTime.UtcNow);
                reception.Valider(false, null, null, "Test", DateTime.UtcNow);
                db.ReceptionsUsinage2.Add(reception);
            }
            await db.SaveChangesAsync();
        }
        var creation = await _client.PostAsync("/api/v1/periodes-bilan/2026-07/enregistrer", null);
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var archive = await _client.GetFromJsonAsync<PeriodeBilanArchiveDetailResponse>("/api/v1/periodes-bilan/2026-07");

        Assert.Equal(51, archive!.Snapshot.Usinage2.Lignes.Count);
        Assert.Equal(51, archive.Snapshot.Usinage2.Total);
        Assert.Contains(archive.Snapshot.Usinage2.Lignes, ligne => ligne.NumeroOF == "OF0051");
    }

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
