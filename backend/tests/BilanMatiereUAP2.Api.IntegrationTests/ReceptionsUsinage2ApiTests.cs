using System.Net;
using System.Net.Http.Json;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Contracts.ReceptionsUsinage2;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BilanMatiereUAP2.Api.IntegrationTests;

public sealed class ReceptionsUsinage2ApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ReceptionsUsinage2ApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildFactory(FakeReceptionContextService fakeContext) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReceptionUsinage2ContextService>();
                services.AddScoped<IReceptionUsinage2ContextService>(_ => fakeContext);
            });
        });

    [Fact]
    public async Task Create_AvecContexteComplet_CreeUneReceptionBrouillon()
    {
        await _factory.ResetDatabaseAsync();
        var client = BuildFactory(FakeReceptionContextService.Complet()).CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/receptions-usinage2", new CreateReceptionUsinage2Request(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, "Fonderie", null));
        var created = await response.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Brouillon", created!.StatutReception);
        Assert.Equal("07CG201-1", created.CodeArticle);
        Assert.Equal(250m, created.PoidsTheoriqueTotalKg);
    }

    [Fact]
    public async Task Create_SansArticleTrouve_RetourneCreatedAvecAnomalieVisible()
    {
        await _factory.ResetDatabaseAsync();
        var client = BuildFactory(FakeReceptionContextService.ArticleIntrouvable()).CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/receptions-usinage2", new CreateReceptionUsinage2Request(
            "OF999", new DateOnly(2026, 7, 26), 100m, 250m, null, null));

        var created = await response.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("ArticleIntrouvable", created!.StatutArticle);
        Assert.Contains("Aucun article unique", created.Avertissements[0]);
    }

    // Un OF absent des QUATRE sources X3 est le seul cas de blocage lie au contexte :
    // il n'est pas enregistrable, contrairement a un OF reconnu au contexte incomplet.
    [Fact]
    public async Task Create_OFIntrouvable_EstBloque()
    {
        await _factory.ResetDatabaseAsync();
        var client = BuildFactory(FakeReceptionContextService.Null()).CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/receptions-usinage2", new CreateReceptionUsinage2Request(
            "OFXXX", new DateOnly(2026, 7, 26), 100m, 250m, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("OF_X3_INTROUVABLE", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CycleDeVieComplet_CreerModifierValiderAnnuler()
    {
        await _factory.ResetDatabaseAsync();
        var client = BuildFactory(FakeReceptionContextService.Complet()).CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/receptions-usinage2", new CreateReceptionUsinage2Request(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/receptions-usinage2/{created!.Id}",
            new UpdateReceptionUsinage2Request(150m, 300m, "Fonderie", "Ajusté"));
        var updated = await updateResponse.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();
        Assert.Equal(150m, updated!.NombrePiecesReceptionnees);

        var validateResponse = await client.PostAsJsonAsync($"/api/v1/receptions-usinage2/{created.Id}/valider",
            new ValiderReceptionUsinage2Request(false, null));
        var validated = await validateResponse.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();
        Assert.Equal("Valide", validated!.StatutReception);

        // Une reception validee ne peut plus etre modifiee.
        var updateAfterValidate = await client.PutAsJsonAsync($"/api/v1/receptions-usinage2/{created.Id}",
            new UpdateReceptionUsinage2Request(999m, 999m, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, updateAfterValidate.StatusCode);

        var cancelResponse = await client.PostAsync($"/api/v1/receptions-usinage2/{created.Id}/annuler", null);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();
        Assert.Equal("Annule", cancelled!.StatutReception);
    }

    // Le poids physique unitaire vient du controle poids Fonderie : c'est une donnee
    // AUTOMATIQUE. Son absence est une anomalie visible, pas un blocage de validation.
    [Fact]
    public async Task Valider_SansPoidsPhysiqueEtSansDerogation_Reussit()
    {
        await _factory.ResetDatabaseAsync();
        var client = BuildFactory(FakeReceptionContextService.PoidsManquant()).CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/receptions-usinage2", new CreateReceptionUsinage2Request(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();

        var validateResponse = await client.PostAsJsonAsync($"/api/v1/receptions-usinage2/{created!.Id}/valider",
            new ValiderReceptionUsinage2Request(false, null));
        var validated = await validateResponse.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();

        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
        Assert.Equal("Valide", validated!.StatutReception);
        Assert.Null(validated.PoidsPhysiqueUnitaireKg);
    }

    [Fact]
    public async Task Valider_SansPoidsPhysiqueAvecDerogationAdmin_Reussit()
    {
        await _factory.ResetDatabaseAsync();
        var client = BuildFactory(FakeReceptionContextService.PoidsManquant()).CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/receptions-usinage2", new CreateReceptionUsinage2Request(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();

        var validateResponse = await client.PostAsJsonAsync($"/api/v1/receptions-usinage2/{created!.Id}/valider",
            new ValiderReceptionUsinage2Request(true, "Dérogation SOPAL justifiée"));
        var validated = await validateResponse.Content.ReadFromJsonAsync<ReceptionUsinage2Response>();

        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
        Assert.Equal("Valide", validated!.StatutReception);
    }

    [Fact]
    public async Task Create_AvecRoleNonAutorise_RetourneForbidden()
    {
        await _factory.ResetDatabaseAsync();
        var client = BuildFactory(FakeReceptionContextService.Complet()).CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/receptions-usinage2")
        {
            Content = JsonContent.Create(new CreateReceptionUsinage2Request("OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null))
        };
        request.Headers.Add("X-Development-Role", "ResponsableFonderie");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_RetourneLesReceptionsCreees()
    {
        await _factory.ResetDatabaseAsync();
        var client = BuildFactory(FakeReceptionContextService.Complet()).CreateClient();
        await client.PostAsJsonAsync("/api/v1/receptions-usinage2", new CreateReceptionUsinage2Request(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null));

        var list = await client.GetFromJsonAsync<PagedReceptionsUsinage2Response>("/api/v1/receptions-usinage2");

        Assert.Equal(1, list!.Total);
        Assert.Equal("OF123", list.Items[0].NumeroOF);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("ResponsableUsinage2", false)]
    [InlineData("ResponsableFonderie", false)]
    public async Task Valider_ReserveAAdmin_MemeParAppelDirect(string role, bool autorise)
    {
        await _factory.ResetDatabaseAsync();
        using var app = BuildFactory(FakeReceptionContextService.Complet());
        using var client = app.CreateClient();
        var creation = await client.PostAsJsonAsync("/api/v1/receptions-usinage2",
            new CreateReceptionUsinage2Request("OF123", new DateOnly(2026, 7, 15), 10m, 25m, null, null));
        creation.EnsureSuccessStatusCode();
        var reception = (await creation.Content.ReadFromJsonAsync<ReceptionUsinage2Response>())!;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/receptions-usinage2/{reception.Id}/valider")
        { Content = JsonContent.Create(new ValiderReceptionUsinage2Request(false, null)) };
        request.Headers.Add("X-Development-Role", role);
        var response = await client.SendAsync(request);
        Assert.Equal(autorise ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
        var persiste = await client.GetFromJsonAsync<ReceptionUsinage2Response>($"/api/v1/receptions-usinage2/{reception.Id}");
        Assert.Equal(autorise ? "Valide" : "Brouillon", persiste!.StatutReception);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("ResponsableUsinage2", true)]
    [InlineData("ResponsableFonderie", false)]
    public async Task LectureEtGestion_ConserventLesPoliciesExistantes(string role, bool autorise)
    {
        await _factory.ResetDatabaseAsync();
        using var app = BuildFactory(FakeReceptionContextService.Complet());
        using var admin = app.CreateClient();
        var creation = await admin.PostAsJsonAsync("/api/v1/receptions-usinage2",
            new CreateReceptionUsinage2Request("OF123", new DateOnly(2026, 7, 15), 10m, 25m, null, null));
        creation.EnsureSuccessStatusCode();
        var reception = (await creation.Content.ReadFromJsonAsync<ReceptionUsinage2Response>())!;
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Role", role);
        var lecture = await client.GetAsync("/api/v1/receptions-usinage2");
        Assert.Equal(autorise ? HttpStatusCode.OK : HttpStatusCode.Forbidden, lecture.StatusCode);
        var ajout = await client.PostAsJsonAsync("/api/v1/receptions-usinage2",
            new CreateReceptionUsinage2Request("OF123", new DateOnly(2026, 7, 16), 10m, 25m, null, null));
        Assert.Equal(autorise ? HttpStatusCode.Created : HttpStatusCode.Forbidden, ajout.StatusCode);
        var modification = await client.PutAsJsonAsync($"/api/v1/receptions-usinage2/{reception.Id}",
            new UpdateReceptionUsinage2Request(12m, 30m, null, null));
        Assert.Equal(autorise ? HttpStatusCode.OK : HttpStatusCode.Forbidden, modification.StatusCode);
        var contexte = await client.PostAsync($"/api/v1/receptions-usinage2/{reception.Id}/recalculer-contexte", null);
        Assert.Equal(autorise ? HttpStatusCode.OK : HttpStatusCode.Forbidden, contexte.StatusCode);
        var annulation = await client.PostAsync($"/api/v1/receptions-usinage2/{reception.Id}/annuler", null);
        Assert.Equal(autorise ? HttpStatusCode.OK : HttpStatusCode.Forbidden, annulation.StatusCode);
    }

    private sealed class FakeReceptionContextService(ReceptionU2ContextResponse? response) : IReceptionUsinage2ContextService
    {
        public Task<ReceptionU2ContextResponse?> GetAsync(string numeroOF, DateOnly dateReception, CancellationToken cancellationToken = default)
            => Task.FromResult(response);

        public static FakeReceptionContextService Complet() => new(new ReceptionU2ContextResponse(
            "OF123", "07CG201-1", "CORPS TEST", "En cours", "16", "M3010L0",
            2.5m, "KG", 2.5m, null, 2.5m, Guid.NewGuid(), "ControlePoidsFonderie",
            10m, 100m, 5m, 0m, 0m, 0m, null, null, 5, 0,
            "Trouve", "Trouve", "Trouve", "Trouve", "Trouve", "Trouve", []));

        public static FakeReceptionContextService PoidsManquant() => new(new ReceptionU2ContextResponse(
            "OF123", "07CG201-1", "CORPS TEST", "En cours", "16", "M3010L0",
            2.5m, "KG", 2.5m, null, null, null, null,
            10m, 100m, 5m, 0m, 0m, 0m, null, null, 5, 0,
            "Trouve", "Trouve", "Trouve", "Trouve", "PoidsPhysiqueManquant", "Trouve", ["Le contrôle poids Fonderie validé est indisponible."]));

        public static FakeReceptionContextService ArticleIntrouvable() => new(new ReceptionU2ContextResponse(
            "OF999", null, null, "En cours", null, null,
            null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, 0, 0,
            "ArticleIntrouvable", "AtelierIntrouvable", "MouvementAbsent", "NomenclatureIntrouvable", "PoidsPhysiqueManquant", "RapportMachineIntrouvable",
            ["Aucun article unique n'est disponible pour cet OF."]));

        public static FakeReceptionContextService Null() => new((ReceptionU2ContextResponse?)null);
    }
}
