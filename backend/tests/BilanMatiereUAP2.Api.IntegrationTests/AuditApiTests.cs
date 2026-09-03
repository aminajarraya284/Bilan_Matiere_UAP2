using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BilanMatiereUAP2.Application.Features.Alertes;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Contracts.Alertes;
using BilanMatiereUAP2.Contracts.Audit;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class AuditApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private static readonly DateTime FirstDateUtc = new(2026, 8, 17, 8, 0, 0, DateTimeKind.Utc);
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_Liste_FiltreTrieEtPagineLesTraces()
    {
        await AjouterAsync(
            CreerTrace(FirstDateUtc, ActionAudit.Creation, DomaineAudit.InventaireFonderie, "INV-001", "CORR-1"),
            CreerTrace(FirstDateUtc.AddHours(1), ActionAudit.Validation, DomaineAudit.InventaireFonderie, "INV-002", "CORR-2"),
            CreerTrace(FirstDateUtc.AddHours(2), ActionAudit.Validation, DomaineAudit.ReceptionUsinage2, "REC-001", "CORR-3"));

        var response = await _client.GetAsync(
            "/api/v1/audit?action=Validation&domaine=InventaireFonderie&page=1&pageSize=1");
        var payload = await response.Content.ReadFromJsonAsync<PagedAuditResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Total);
        Assert.Equal(1, payload.Page);
        Assert.Equal(1, payload.PageSize);
        Assert.Equal("INV-002", Assert.Single(payload.Items).ReferenceMetier);
        Assert.Equal(FirstDateUtc.AddHours(1), payload.DerniereActiviteUtc);
        Assert.Equal(DateTimeKind.Utc, payload.Items[0].DateUtc.Kind);
    }

    [Fact]
    public async Task Get_Detail_RetourneLesComparaisonsStructureesSansSecret()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IAuditService>();
            await service.EnregistrerAsync(new AuditEntry(
                ActionAudit.Modification,
                DomaineAudit.ControlePoidsFonderie,
                "ControlePoidsFonderie",
                "CTRL-1",
                "OF-001",
                "Test HTTP",
                "Modification du controle",
                new Dictionary<string, object?>
                {
                    ["poidsKg"] = null,
                    ["password"] = "ne-doit-jamais-sortir"
                },
                new Dictionary<string, object?>
                {
                    ["poidsKg"] = 12.5m,
                    ["accessToken"] = "secret-token"
                }));
        }

        Guid id;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
            id = await context.AuditTraces.Select(trace => trace.Id).SingleAsync();
        }

        var response = await _client.GetAsync($"/api/v1/audit/{id}");
        var raw = await response.Content.ReadAsStringAsync();
        var payload = await response.Content.ReadFromJsonAsync<AuditDetailResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("ControlePoidsFonderie", payload!.Entite);
        Assert.True(payload.AnciennesValeurs!.Value.TryGetProperty("poidsKg", out var oldWeight));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, oldWeight.ValueKind);
        Assert.Equal(12.5m, payload.NouvellesValeurs!.Value.GetProperty("poidsKg").GetDecimal());
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-token", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_DetailInexistant_RetourneNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/audit/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/audit?page=0")]
    [InlineData("/api/v1/audit?pageSize=201")]
    [InlineData("/api/v1/audit?action=Inconnue")]
    [InlineData("/api/v1/audit?domaine=Inconnu")]
    [InlineData("/api/v1/audit?dateDepuis=2026-08-18&dateJusqua=2026-08-17")]
    public async Task Get_FiltresInvalides_RetourneBadRequest(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("ResponsableFonderie")]
    [InlineData("ResponsableUsinage2")]
    public async Task Get_RoleNonAdmin_RetourneForbidden(string role)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/audit");
        request.Headers.Add("X-Development-Role", role);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_TokenInvalide_RetourneUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/audit");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token.invalide");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ActionMetier_ConserveLeCorrelationIdDansLaTraceEtLaReponse()
    {
        var alerte = AlerteMetier.Creer(
            CodesAlertes.U2QuantiteNonReceptionnee,
            TypeAlerte.AnomalieMetier,
            SeveriteAlerte.Avertissement,
            "Quantite non receptionnee",
            "La quantite non receptionnee fournie par le bilan est positive.",
            SourcesAlertes.BilanUsinage2,
            "2026-08",
            "OF-CORR",
            "ART-CORR",
            "M3000L0",
            "AUDIT-CORRELATION",
            true,
            "{\"quantitePieces\":\"10\",\"unite\":\"pieces\"}",
            FirstDateUtc);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
            context.AlertesMetier.Add(alerte);
            await context.SaveChangesAsync();
        }

        const string correlationId = "AUDIT-HTTP-20260818";
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/alertes/{alerte.Id}/acquitter")
        {
            Content = JsonContent.Create(new AcquitterAlerteRequest("Pris en charge"))
        };
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
        using var verificationScope = factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        var trace = await verificationContext.AuditTraces.SingleAsync();
        Assert.Equal(ActionAudit.Acquittement, trace.Action);
        Assert.Equal(DomaineAudit.Alerte, trace.Domaine);
        Assert.Equal(correlationId, trace.CorrelationId);
        Assert.Equal(alerte.Id.ToString(), trace.EntiteId);
    }

    private async Task AjouterAsync(params TraceAudit[] traces)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        context.AuditTraces.AddRange(traces);
        await context.SaveChangesAsync();
    }

    private static TraceAudit CreerTrace(
        DateTime dateUtc,
        ActionAudit action,
        DomaineAudit domaine,
        string referenceMetier,
        string correlationId)
        => TraceAudit.Creer(
            dateUtc,
            action,
            domaine,
            "EntiteTest",
            Guid.NewGuid().ToString(),
            referenceMetier,
            "admin",
            "Administrateur SOPAL",
            "admin@sopal.local",
            "Admin",
            "Tests HTTP",
            correlationId,
            $"Trace {referenceMetier}",
            null,
            "{\"quantite\":10,\"unite\":\"pieces\"}",
            null);
}
