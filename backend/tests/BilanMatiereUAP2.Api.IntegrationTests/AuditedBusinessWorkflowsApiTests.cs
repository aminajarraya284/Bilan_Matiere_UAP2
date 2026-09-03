using System.Net;
using System.Net.Http.Json;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Contracts.ControlePoidsFonderie;
using BilanMatiereUAP2.Contracts.DechetsFonderie;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Contracts.RebutsUsinage2;
using BilanMatiereUAP2.Contracts.ReceptionsUsinage2;
using BilanMatiereUAP2.Contracts.TypesMatiere;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class AuditedBusinessWorkflowsApiTests(ApiWebApplicationFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Inventaire_ValidationEtAnnulation_CreentDeuxTracesCiblees()
    {
        var type = await GetTypeMatiereAsync("M3000L0");
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/inventaires-fonderie/comptage/conteneurs",
            new CreateInventaireComptageConteneurRequest(
                new DateOnly(2026, 8, 1), "C-AUDIT", "OF-AUDIT", "ART-AUDIT", null,
                type.Id, 12.5m, "Test audit"));
        var created = await createResponse.Content.ReadFromJsonAsync<InventaireComptageConteneurResponse>();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var validation = await _client.PostAsync(
            $"/api/v1/inventaires-fonderie/comptage/conteneurs/{created!.Id}/valider", null);
        var annulation = await _client.PostAsync(
            $"/api/v1/inventaires-fonderie/comptage/conteneurs/{created.Id}/annuler", null);

        Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
        Assert.Equal(HttpStatusCode.OK, annulation.StatusCode);
        var traces = await LireTracesAsync(created.Id);
        Assert.Contains(traces, trace => trace.Action == ActionAudit.Validation && trace.Domaine == DomaineAudit.InventaireFonderie);
        Assert.Contains(traces, trace => trace.Action == ActionAudit.Annulation && trace.Domaine == DomaineAudit.InventaireFonderie);
        Assert.All(traces, trace => Assert.Contains("2026-08", trace.ReferenceMetier));
    }

    [Fact]
    public async Task Dechet_Validation_CreeUneTraceAvecPoidsEnKg()
    {
        var type = await GetTypeMatiereAsync("M3010L0");
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/dechets-fonderie",
            new CreateDechetFonderieRequest(
                new DateOnly(2026, 8, 18), type.Id, "Bavure", 7.25m, "Test audit"));
        var created = await createResponse.Content.ReadFromJsonAsync<DechetFonderieResponse>();

        var validation = await _client.PostAsync($"/api/v1/dechets-fonderie/{created!.Id}/valider", null);

        Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
        var trace = Assert.Single(
            await LireTracesAsync(created.Id),
            item => item.Action == ActionAudit.Validation);
        Assert.Equal(DomaineAudit.DechetFonderie, trace.Domaine);
        Assert.Contains("\"poidsKg\":7.25", trace.NouvellesValeursJson);
        Assert.Contains("\"statut\":\"Valide\"", trace.NouvellesValeursJson);
    }

    [Fact]
    public async Task ControlePoids_Validation_CreeUneTraceAvecPoidsUnitaireEnKg()
    {
        var type = await GetTypeMatiereAsync("M3060L0");
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/controles-poids-fonderie",
            new CreateControlePoidsFonderieRequest(
                new DateOnly(2026, 8, 18), "OF-CTRL", "ART-CTRL", type.Id,
                "LOT-1", 2.75m, 3, "Test audit"));
        var created = await createResponse.Content.ReadFromJsonAsync<ControlePoidsFonderieResponse>();

        var validation = await _client.PostAsync(
            $"/api/v1/controles-poids-fonderie/{created!.Id}/valider", null);

        Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
        var trace = Assert.Single(
            await LireTracesAsync(created.Id),
            item => item.Action == ActionAudit.Validation);
        Assert.Equal(DomaineAudit.ControlePoidsFonderie, trace.Domaine);
        Assert.Contains("\"poidsUnitaireKg\":2.75", trace.NouvellesValeursJson);
        Assert.Contains("\"statut\":\"Valide\"", trace.NouvellesValeursJson);
    }

    [Fact]
    public async Task ReceptionU2_ValidationEtAnnulation_CreentLesTracesAttendues()
    {
        using var application = BuildReceptionFactory();
        var client = application.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/receptions-usinage2",
            new CreateReceptionUsinage2Request(
                "OF-REC-AUDIT", new DateOnly(2026, 8, 18), 100m, 250m, "Fonderie", null));
        var created = await createResponse.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();

        var validation = await client.PostAsJsonAsync(
            $"/api/v1/receptions-usinage2/{created!.Id}/valider",
            new ValiderReceptionUsinage2Request(false, null));
        var annulation = await client.PostAsync(
            $"/api/v1/receptions-usinage2/{created.Id}/annuler", null);

        Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
        Assert.Equal(HttpStatusCode.OK, annulation.StatusCode);
        var traces = await LireTracesAsync(created.Id, application.Services);
        Assert.Contains(traces, trace => trace.Action == ActionAudit.Validation && trace.Domaine == DomaineAudit.ReceptionUsinage2);
        Assert.Contains(traces, trace => trace.Action == ActionAudit.Annulation && trace.Domaine == DomaineAudit.ReceptionUsinage2);
        var validationTrace = Assert.Single(traces, trace => trace.Action == ActionAudit.Validation);
        Assert.Contains("\"quantitePieces\":100", validationTrace.NouvellesValeursJson);
        Assert.Contains("\"poidsTotalReceptionneKg\":250", validationTrace.NouvellesValeursJson);
    }

    [Fact]
    public async Task RebutU2_Validation_CreeUneTraceEnPieces()
    {
        using var application = BuildRebutFactory();
        var client = application.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/rebuts-usinage2",
            new CreateRebutUsinage2Request
            {
                DateDeclaration = new DateOnly(2026, 8, 18),
                NumeroOF = "OF-REBUT-AUDIT",
                CodeArticle = "ART-REBUT-AUDIT",
                QuantiteRebutPieces = 4m,
                Motif = "Test audit"
            });
        var created = await createResponse.Content.ReadFromJsonAsync<RebutUsinage2Response>();

        var validation = await client.PostAsync($"/api/v1/rebuts-usinage2/{created!.Id}/valider", null);

        Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
        var trace = Assert.Single(
            await LireTracesAsync(created.Id, application.Services),
            item => item.Action == ActionAudit.Validation);
        Assert.Equal(DomaineAudit.RebutUsinage2, trace.Domaine);
        Assert.Contains("\"quantitePieces\":4", trace.NouvellesValeursJson);
        Assert.DoesNotContain("quantiteKg", trace.NouvellesValeursJson, StringComparison.OrdinalIgnoreCase);
    }

    private WebApplicationFactory<Program> BuildReceptionFactory()
        => factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IReceptionUsinage2ContextService>();
            services.AddScoped<IReceptionUsinage2ContextService>(_ => new FakeReceptionContextService());
        }));

    private WebApplicationFactory<Program> BuildRebutFactory()
        => factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOfContextResolver>();
            services.AddScoped<IOfContextResolver>(_ => new FakeOfContextResolver());
        }));

    private async Task<TypeMatiereResponse> GetTypeMatiereAsync(string code)
    {
        var items = await _client.GetFromJsonAsync<List<TypeMatiereResponse>>("/api/v1/types-matiere?actif=true");
        return items!.Single(item => item.Code == code);
    }

    private Task<List<TraceAudit>> LireTracesAsync(Guid entityId, IServiceProvider? services = null)
        => LireTracesAsync(entityId.ToString(), services ?? factory.Services);

    private static async Task<List<TraceAudit>> LireTracesAsync(string entityId, IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        return await context.AuditTraces
            .AsNoTracking()
            .Where(trace => trace.EntiteId == entityId)
            .OrderBy(trace => trace.DateUtc)
            .ToListAsync();
    }

    private sealed class FakeReceptionContextService : IReceptionUsinage2ContextService
    {
        public Task<ReceptionU2ContextResponse?> GetAsync(
            string numeroOF,
            DateOnly dateReception,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ReceptionU2ContextResponse?>(new ReceptionU2ContextResponse(
                numeroOF, "ART-REC-AUDIT", "Article test", "En cours", "16", "M3010L0",
                2.5m, "KG", 2.5m, null, 2.5m, Guid.NewGuid(), "ControlePoidsFonderie",
                10m, 100m, 5m, 0m, 0m, 0m, null, null, 5, 0,
                "Trouve", "Trouve", "Trouve", "Trouve", "Trouve", "Trouve", []));
    }

    private sealed class FakeOfContextResolver : IOfContextResolver
    {
        public Task<OfContextResult> ResolveAsync(
            string numeroOf,
            DateOnly? date,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new OfContextResult(
                numeroOf,
                "ART-REBUT-AUDIT",
                "Article test",
                "M3010L0",
                SourceTypeMatiere.SortieMp,
                "16",
                "UAP2",
                true,
                true,
                false,
                false,
                false,
                "Rapproche",
                []));
    }
}
