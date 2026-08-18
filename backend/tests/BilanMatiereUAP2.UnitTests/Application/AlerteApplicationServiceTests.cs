using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Alertes;
using BilanMatiereUAP2.Contracts.Alertes;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class AlerteApplicationServiceTests
{
    private static readonly DateTime NowUtc = new(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Search_Admin_TransmetPaginationEtFiltresNormalises()
    {
        var repository = new FakeRepository { SearchItems = [CreerAlerte()] };
        var service = CreerService(repository);

        var response = await service.SearchAsync(new GetAlertesQuery(
            " 2026-01 ", "ouverte", "avertissement", " Bilan Usinage 2 ", " OF-1 ", " ART-1 ", " réception ", 2, 25));

        Assert.Equal(1, response.Total);
        Assert.NotNull(repository.LastFilter);
        Assert.Equal(2, repository.LastFilter!.Page);
        Assert.Equal(25, repository.LastFilter.PageSize);
        Assert.Equal(StatutAlerte.Ouverte, repository.LastFilter.Statut);
        Assert.Equal(SeveriteAlerte.Avertissement, repository.LastFilter.Severite);
        Assert.Null(repository.LastFilter.SourcesAutorisees);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public async Task Search_PaginationInvalide_RetourneValidation(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            CreerService().SearchAsync(new GetAlertesQuery(null, null, null, null, null, null, null, page, pageSize)));
    }

    [Fact]
    public async Task Search_ResponsableFonderie_LimiteLeRepositoryASaSource()
    {
        var repository = new FakeRepository();
        var service = CreerService(repository, role: "ResponsableFonderie");

        await service.SearchAsync(new GetAlertesQuery(null, null, null, null, null, null, null));

        Assert.Equal([SourcesAlertes.BilanFonderie], repository.LastFilter!.SourcesAutorisees);
    }

    [Fact]
    public async Task Get_AlerteHorsPerimetre_RetourneForbidden()
    {
        var repository = new FakeRepository { SearchItems = [CreerAlerte(source: SourcesAlertes.BilanUsinage2)] };
        var service = CreerService(repository, role: "ResponsableFonderie");

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(repository.SearchItems[0].Id));
    }

    [Fact]
    public async Task Acquitter_TraceLUtilisateurEtSauvegarde()
    {
        var alerte = CreerAlerte();
        var repository = new FakeRepository { SearchItems = [alerte] };
        var service = CreerService(repository, userName: "Responsable U2");

        var response = await service.AcquitterAsync(alerte.Id, "Pris en charge");

        Assert.Equal("Acquittee", response.Statut);
        Assert.Equal("Responsable U2", response.AcquitteePar);
        Assert.Equal(1, repository.NombreSauvegardes);
    }

    [Fact]
    public async Task Ignorer_SansCommentaire_EstRefuse()
    {
        var alerte = CreerAlerte();
        var repository = new FakeRepository { SearchItems = [alerte] };

        await Assert.ThrowsAsync<BilanMatiereUAP2.Domain.Common.DomainValidationException>(
            () => CreerService(repository).IgnorerAsync(alerte.Id, " "));
    }

    [Fact]
    public async Task Resoudre_AlerteIgnoree_RetourneConflict()
    {
        var alerte = CreerAlerte();
        alerte.Ignorer("admin", NowUtc, "Justification");
        var repository = new FakeRepository { SearchItems = [alerte] };

        await Assert.ThrowsAsync<ConflictException>(() => CreerService(repository).ResoudreAsync(alerte.Id, null));
    }

    [Fact]
    public async Task Get_Inexistante_RetourneNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreerService().GetAsync(Guid.NewGuid()));
    }

    private static AlerteApplicationService CreerService(
        FakeRepository? repository = null,
        string role = "Admin",
        string userName = "Administrateur SOPAL")
        => new(
            repository ?? new FakeRepository(),
            new FakeGenerationService(),
            new FakeCurrentUser(role, userName),
            new FakeDateTimeProvider());

    private static AlerteMetier CreerAlerte(string source = "Bilan Usinage 2")
        => AlerteMetier.Creer(
            CodesAlertes.U2QuantiteNonReceptionnee,
            TypeAlerte.AnomalieMetier,
            SeveriteAlerte.Avertissement,
            "Quantité non réceptionnée",
            "La quantité source est positive.",
            source,
            "2026-01",
            "OF-1",
            "ART-1",
            "M3000L0",
            $"CLE|{Guid.NewGuid()}",
            true,
            "{\"unite\":\"pieces\"}",
            NowUtc.AddHours(-1));

    private sealed class FakeRepository : IAlerteRepository
    {
        public IReadOnlyList<AlerteMetier> SearchItems { get; init; } = [];
        public AlerteSearchFilter? LastFilter { get; private set; }
        public int NombreSauvegardes { get; private set; }

        public Task<AlerteMetier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(SearchItems.FirstOrDefault(alerte => alerte.Id == id));

        public Task<AlerteMetier?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<AlerteMetier>> GetActiveByPeriodAsync(string periode, CancellationToken cancellationToken = default)
            => Task.FromResult(SearchItems.Where(alerte => alerte.EstActive && alerte.Periode == periode).ToArray() as IReadOnlyList<AlerteMetier>);

        public Task<AlerteSearchResult> SearchAsync(AlerteSearchFilter filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return Task.FromResult(new AlerteSearchResult(
                SearchItems,
                SearchItems.Count,
                SearchItems.Count(alerte => alerte.Statut == StatutAlerte.Ouverte),
                SearchItems.Count(alerte => alerte.Statut == StatutAlerte.Acquittee),
                SearchItems.Count(alerte => alerte.Statut == StatutAlerte.Resolue),
                SearchItems.Count(alerte => alerte.Statut == StatutAlerte.Ignoree),
                SearchItems.Count == 0 ? null : SearchItems.Max(alerte => alerte.DerniereDetectionLeUtc)));
        }

        public Task AddRangeAsync(IEnumerable<AlerteMetier> alertes, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NombreSauvegardes++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGenerationService : IAlerteGenerationService
    {
        public Task<AlerteRecalculResponse> RecalculerAsync(string periode, CancellationToken cancellationToken = default)
            => Task.FromResult(new AlerteRecalculResponse(periode, NowUtc, 0, 0, 0, 0));
    }

    private sealed class FakeCurrentUser(string role, string userName) : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("de000000-0000-0000-0000-000000000001");
        public string? UserName => userName;
        public string? Role => role;
        public bool IsAuthenticated => true;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => NowUtc;
    }
}
