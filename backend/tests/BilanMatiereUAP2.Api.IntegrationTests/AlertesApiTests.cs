using System.Net;
using System.Net.Http.Json;
using BilanMatiereUAP2.Application.Features.Alertes;
using BilanMatiereUAP2.Contracts.Alertes;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class AlertesApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private static readonly DateTime DetectionUtc = new(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc);
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_ListePaginee_FiltreLaPeriodeEtRetourneLeResume()
    {
        await AjouterAsync(
            CreerAlerte("CLE-1", SourcesAlertes.BilanUsinage2, "2026-01", "OF-1"),
            CreerAlerte("CLE-2", SourcesAlertes.BilanUsinage2, "2026-02", "OF-2"));

        var response = await _client.GetAsync("/api/v1/alertes?periode=2026-01&statut=Ouverte&page=1&pageSize=10");
        var payload = await response.Content.ReadFromJsonAsync<PagedAlertesResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Total);
        Assert.Equal(1, payload.Summary.Ouvertes);
        Assert.Equal("OF-1", Assert.Single(payload.Items).NumeroOF);
        Assert.Equal(DateTimeKind.Utc, payload.Items[0].DetecteeLeUtc.Kind);
    }

    [Fact]
    public async Task Get_Detail_RetourneLesDonneesAssocieesSansSecret()
    {
        var alerte = CreerAlerte("CLE-DETAIL", SourcesAlertes.BilanUsinage2, "2026-01", "OF-1");
        await AjouterAsync(alerte);

        var response = await _client.GetAsync($"/api/v1/alertes/{alerte.Id}");
        var payload = await response.Content.ReadFromJsonAsync<AlerteDetailResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("pieces", payload!.DonneesAssociees["unite"]);
        Assert.DoesNotContain("token", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_Acquitter_EstIdempotentEtPersisteLaTrace()
    {
        var alerte = CreerAlerte("CLE-ACK", SourcesAlertes.BilanUsinage2, "2026-01", "OF-1");
        await AjouterAsync(alerte);

        var first = await _client.PostAsJsonAsync(
            $"/api/v1/alertes/{alerte.Id}/acquitter",
            new AcquitterAlerteRequest("Pris en charge"));
        var second = await _client.PostAsJsonAsync(
            $"/api/v1/alertes/{alerte.Id}/acquitter",
            new AcquitterAlerteRequest("Second appel"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var persisted = await LireAsync(alerte.Id);
        Assert.Equal(StatutAlerte.Acquittee, persisted.Statut);
        Assert.Equal("Utilisateur développement", persisted.AcquitteePar);
        Assert.Equal("Pris en charge", persisted.CommentaireTraitement);
        Assert.Contains(
            await LireTracesAsync(alerte.Id),
            trace => trace.Action == ActionAudit.Acquittement && trace.Domaine == DomaineAudit.Alerte);
    }

    [Fact]
    public async Task Post_Resoudre_RendLAlerteInactiveEtConserveLHistorique()
    {
        var alerte = CreerAlerte("CLE-RESOLVE", SourcesAlertes.BilanUsinage2, "2026-01", "OF-1");
        await AjouterAsync(alerte);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/alertes/{alerte.Id}/resoudre",
            new ResoudreAlerteRequest("Cause source corrigée"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var persisted = await LireAsync(alerte.Id);
        Assert.Equal(StatutAlerte.Resolue, persisted.Statut);
        Assert.False(persisted.EstActive);
        Assert.NotNull(persisted.ResolueLeUtc);
        Assert.Contains(
            await LireTracesAsync(alerte.Id),
            trace => trace.Action == ActionAudit.Resolution && trace.Domaine == DomaineAudit.Alerte);
    }

    [Fact]
    public async Task Post_Ignorer_SansJustification_RetourneBadRequest()
    {
        var alerte = CreerAlerte("CLE-IGNORE", SourcesAlertes.BilanUsinage2, "2026-01", "OF-1");
        await AjouterAsync(alerte);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/alertes/{alerte.Id}/ignorer",
            new IgnorerAlerteRequest(" "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(StatutAlerte.Ouverte, (await LireAsync(alerte.Id)).Statut);
    }

    [Fact]
    public async Task Post_Ignorer_AvecJustification_PersisteLaTrace()
    {
        var alerte = CreerAlerte("CLE-IGNORE-VALID", SourcesAlertes.BilanUsinage2, "2026-01", "OF-1");
        await AjouterAsync(alerte);

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/alertes/{alerte.Id}/ignorer",
            new IgnorerAlerteRequest("Écart analysé et volontairement écarté"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StatutAlerte.Ignoree, (await LireAsync(alerte.Id)).Statut);
        Assert.Contains(
            await LireTracesAsync(alerte.Id),
            trace => trace.Action == ActionAudit.Ignorance && trace.Domaine == DomaineAudit.Alerte);
    }

    [Fact]
    public async Task Get_AlerteHorsPerimetreDuRole_RetourneForbidden()
    {
        var alerte = CreerAlerte("CLE-SCOPE", SourcesAlertes.BilanUsinage2, "2026-01", "OF-1");
        await AjouterAsync(alerte);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/alertes/{alerte.Id}");
        request.Headers.Add("X-Development-Role", "ResponsableFonderie");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_Inexistante_RetourneNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/alertes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("2026")]
    [InlineData("2026-13")]
    [InlineData("01-2026")]
    public async Task Get_PeriodeInvalide_RetourneBadRequest(string periode)
    {
        var response = await _client.GetAsync($"/api/v1/alertes?periode={periode}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_RecalculerDeuxFois_NeCreePasDeDoublon()
    {
        var first = await _client.PostAsync("/api/v1/alertes/recalculer?periode=2026-01", null);
        var second = await _client.PostAsync("/api/v1/alertes/recalculer?periode=2026-01", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        var keys = await context.AlertesMetier.Select(item => item.CleDeduplication).ToListAsync();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        var recalculs = await context.AuditTraces
            .AsNoTracking()
            .Where(trace => trace.Action == ActionAudit.Recalcul && trace.EntiteId == "2026-01")
            .ToListAsync();
        Assert.Equal(2, recalculs.Count);
        Assert.All(recalculs, trace =>
        {
            Assert.Equal(DomaineAudit.Alerte, trace.Domaine);
            Assert.Contains("\"periode\":\"2026-01\"", trace.InformationsJson);
            Assert.Contains("\"nombreCreees\"", trace.InformationsJson);
            Assert.Contains("\"nombreMisesAJour\"", trace.InformationsJson);
            Assert.Contains("\"nombreResolues\"", trace.InformationsJson);
        });
    }

    private async Task AjouterAsync(params AlerteMetier[] alertes)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        context.AlertesMetier.AddRange(alertes);
        await context.SaveChangesAsync();
    }

    private async Task<AlerteMetier> LireAsync(Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        return await context.AlertesMetier.SingleAsync(item => item.Id == id);
    }

    private async Task<List<TraceAudit>> LireTracesAsync(Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        return await context.AuditTraces
            .AsNoTracking()
            .Where(trace => trace.EntiteId == id.ToString())
            .ToListAsync();
    }

    private static AlerteMetier CreerAlerte(string cle, string source, string periode, string numeroOf)
        => AlerteMetier.Creer(
            CodesAlertes.U2QuantiteNonReceptionnee,
            TypeAlerte.AnomalieMetier,
            SeveriteAlerte.Avertissement,
            "Quantité non réceptionnée",
            "La quantité non réceptionnée fournie par le bilan est positive.",
            source,
            periode,
            numeroOf,
            "ART-1",
            "M3000L0",
            cle,
            true,
            "{\"quantitePieces\":\"10\",\"unite\":\"pieces\"}",
            DetectionUtc);
}
