using System.Text.Json;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class AuditDataSanitizerTests
{
    [Fact]
    public void Sanitize_SupprimeTousLesSecretsSansTenirCompteDeLaCasseEtDansLesObjetsImbriques()
    {
        var sensitiveKeys = new[]
        {
            "Password", "password", "PASSWORD", "PasswordHash", "ClientSecret", "AccessToken",
            "RefreshToken", "IdToken", "Authorization", "Cookie", "ConnectionString", "ApiKey", "PrivateKey"
        };
        var nested = sensitiveKeys.ToDictionary(key => key, _ => (object?)"NE_DOIT_PAS_SORTIR");
        var input = new Dictionary<string, object?>
        {
            ["visible"] = "ok",
            ["nested"] = nested,
            ["rows"] = new object?[] { new Dictionary<string, object?> { ["secret"] = "CACHE", ["poidsKg"] = 0m } }
        };

        var sanitized = new AuditDataSanitizer().Sanitize(input);
        var json = JsonSerializer.Serialize(sanitized);

        Assert.Contains("visible", json);
        Assert.Contains("poidsKg", json);
        Assert.DoesNotContain("NE_DOIT_PAS_SORTIR", json);
        Assert.DoesNotContain("CACHE", json);
        foreach (var key in sensitiveKeys) Assert.DoesNotContain(key, json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_ConserveNullEtZeroCommeDeuxValeursDistinctes()
    {
        var sanitized = new AuditDataSanitizer().Sanitize(new Dictionary<string, object?>
        {
            ["absent"] = null,
            ["poidsKg"] = 0m
        });

        Assert.Null(sanitized["absent"]);
        Assert.Equal(0m, sanitized["poidsKg"]);
    }
}

public sealed class AuditServiceTests
{
    private static readonly DateTime NowUtc = new(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("development-user", "Utilisateur développement")]
    [InlineData("entra-object-id", "Utilisateur Entra")]
    public async Task Enregistrer_UtilisateurAuthentifie_CaptureContexteCorrelationEtValeurs(
        string userId,
        string displayName)
    {
        var repository = new FakeAuditRepository();
        var service = CreateService(repository, new FakeUser(true, userId, displayName, "u@sopal.com", ["Admin"]));

        await service.EnregistrerAsync(new AuditEntry(
            ActionAudit.Modification,
            DomaineAudit.ReceptionUsinage2,
            "ReceptionUsinage2",
            "reception-1",
            "OF-1|ART-1",
            "Tests",
            "Modification",
            new Dictionary<string, object?> { ["poidsTotalReceptionneKg"] = null },
            new Dictionary<string, object?> { ["poidsTotalReceptionneKg"] = 0m, ["AccessToken"] = "SECRET" }));

        var trace = Assert.Single(repository.Items);
        Assert.Equal(userId, trace.UtilisateurId);
        Assert.Equal(displayName, trace.UtilisateurNom);
        Assert.Equal("Admin", trace.Role);
        Assert.Equal("corr-audit-1", trace.CorrelationId);
        Assert.Contains(":null", trace.AnciennesValeursJson);
        Assert.Contains(":0", trace.NouvellesValeursJson);
        Assert.DoesNotContain("SECRET", trace.NouvellesValeursJson);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Enregistrer_SansUtilisateur_UtiliseLeCompteSystemeSansRoleAdmin()
    {
        var repository = new FakeAuditRepository();
        var service = CreateService(repository, new FakeUser(false, "ignored", "ignored", null, []));

        await service.EnregistrerAsync(Entry());

        var trace = Assert.Single(repository.Items);
        Assert.Equal("SYSTEM", trace.UtilisateurId);
        Assert.Equal("Système", trace.UtilisateurNom);
        Assert.Equal("System", trace.Role);
        Assert.Null(trace.UtilisateurEmail);
    }

    [Fact]
    public async Task Query_ValidePaginationFiltresEtRetourneDetail()
    {
        var trace = CreateTrace();
        var repository = new FakeAuditRepository { SearchItems = [trace] };
        var service = new AuditQueryService(repository);

        var response = await service.SearchAsync(new GetAuditQuery(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 18),
            " utilisateur ",
            " Admin ",
            "validation",
            "inventairefonderie",
            " InventaireConteneur ",
            " M3000 ",
            " corr ",
            " validation ",
            2,
            25));
        var detail = await service.GetAsync(trace.Id);

        Assert.Single(response.Items);
        Assert.Equal(2, repository.Criteria!.Page);
        Assert.Equal(25, repository.Criteria.PageSize);
        Assert.Equal(ActionAudit.Validation, repository.Criteria.Action);
        Assert.Equal(DomaineAudit.InventaireFonderie, repository.Criteria.Domaine);
        Assert.Equal(new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc), repository.Criteria.DateJusquaExclusiveUtc);
        Assert.Equal(trace.CorrelationId, detail.CorrelationId);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public async Task Query_PaginationInvalide_EstRefusee(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            new AuditQueryService(new FakeAuditRepository()).SearchAsync(
                new GetAuditQuery(null, null, null, null, null, null, null, null, null, null, page, pageSize)));
    }

    [Fact]
    public async Task Get_TraceAbsente_RetourneNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new AuditQueryService(new FakeAuditRepository()).GetAsync(Guid.NewGuid()));
    }

    private static AuditService CreateService(FakeAuditRepository repository, ICurrentUserContext user)
        => new(repository, new AuditDataSanitizer(), user, new FakeCorrelation(), new FakeClock());

    private static AuditEntry Entry()
        => new(
            ActionAudit.Validation,
            DomaineAudit.InventaireFonderie,
            "InventaireConteneur",
            "1",
            "2026-08|M3000|1",
            "Tests",
            "Validation");

    private static TraceAudit CreateTrace()
        => TraceAudit.Creer(
            NowUtc,
            ActionAudit.Validation,
            DomaineAudit.InventaireFonderie,
            "InventaireConteneur",
            "1",
            "2026-08|M3000|1",
            "user",
            "Utilisateur",
            null,
            "Admin",
            "Tests",
            "corr-audit-1",
            "Validation",
            "{\"statut\":\"Brouillon\"}",
            "{\"statut\":\"Valide\"}",
            null);

    private sealed record FakeUser(
        bool IsAuthenticated,
        string UserId,
        string DisplayName,
        string? Email,
        IReadOnlyCollection<string> Roles) : ICurrentUserContext;

    private sealed class FakeCorrelation : ICorrelationIdAccessor
    {
        public string CorrelationId => "corr-audit-1";
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow => NowUtc;
    }
}

internal sealed class FakeAuditRepository : IAuditRepository
{
    public List<TraceAudit> Items { get; } = [];
    public IReadOnlyList<TraceAudit> SearchItems { get; init; } = [];
    public AuditSearchCriteria? Criteria { get; private set; }
    public int SaveCount { get; private set; }

    public Task AddAsync(TraceAudit trace, CancellationToken cancellationToken = default)
    {
        Items.Add(trace);
        return Task.CompletedTask;
    }

    public Task<TraceAudit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(SearchItems.Concat(Items).FirstOrDefault(trace => trace.Id == id));

    public Task<AuditSearchResult> SearchAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        Criteria = criteria;
        return Task.FromResult(new AuditSearchResult(
            SearchItems,
            SearchItems.Count,
            SearchItems.Count == 0 ? null : SearchItems.Max(trace => trace.DateUtc)));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
