using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.InventairesFonderie;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Domain.Entities;
using Xunit;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class InventaireComptageServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly July = new(2026, 7, 1);
    private static readonly Guid TypeId = Guid.Parse("30000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task GetPeriode_SansProjectionPrecedente_RetourneNullEtAnomalie()
    {
        var service = CreateService(out _);

        var response = await service.GetPeriodeAsync("2026-07");

        var total = Assert.Single(response.TotauxParTypeMatiere!);
        Assert.Null(total.StockDebutKg);
        Assert.Equal(0m, total.StockFinKg);
        Assert.Contains("STOCK_DEBUT_INTROUVABLE", total.Anomalies);
    }

    [Fact]
    public async Task ValidationsEtAnnulation_RecalculentUneProjectionUnique()
    {
        var service = CreateService(out var repository);
        var conteneur = InventaireConteneur.Creer(
            July, "C-001", null, null, null, TypeId, 10m, null, null, "test", Now);
        repository.Conteneurs.Add(conteneur);

        await service.ValiderConteneurAsync(conteneur.Id);
        Assert.Equal(10m, Assert.Single(repository.Projections).StockFinKg);

        var categorie = InventaireCategorieComptage.Creer(
            July, "REBUT", TypeId, 3m, null, null, "test", Now);
        repository.Categories.Add(categorie);
        await service.ValiderCategorieAsync(categorie.Id);
        Assert.Equal(13m, Assert.Single(repository.Projections).StockFinKg);

        var four = InventaireFourComptage.Creer(
            July, "FOUR-01", TypeId, 4m, null, null, "test", Now);
        repository.Fours.Add(four);
        await service.ValiderFourAsync(four.Id);
        var projection = Assert.Single(repository.Projections);
        Assert.Equal(13m, projection.StockFinKg);

        await service.AnnulerConteneurAsync(conteneur.Id);
        Assert.Equal(3m, Assert.Single(repository.Projections).StockFinKg);
        Assert.Equal("Annule", conteneur.Statut.ToString());
    }

    [Fact]
    public async Task GetPeriode_ReprendLeStockFinDuMoisPrecedent()
    {
        var service = CreateService(out var repository);
        repository.Projections.Add(InventaireFonderie.Creer(
            new DateOnly(2026, 6, 1), TypeId, null, 27m, null, null, "test", Now));

        var response = await service.GetPeriodeAsync("2026-07");

        var total = Assert.Single(response.TotauxParTypeMatiere!);
        Assert.Equal(27m, total.StockDebutKg);
        Assert.Empty(total.Anomalies);
    }

    [Fact]
    public async Task EchecProjection_DemandeRollbackEtNeCreePasDeProjection()
    {
        var service = CreateService(out var repository);
        repository.FailWhenAddingProjection = true;
        var conteneur = InventaireConteneur.Creer(
            July, "C-ROLLBACK", null, null, null, TypeId, 10m, null, null, "test", Now);
        repository.Conteneurs.Add(conteneur);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ValiderConteneurAsync(conteneur.Id));

        Assert.Equal(1, repository.RollbackCount);
        Assert.Empty(repository.Projections);
    }

    private static InventaireComptageService CreateService(out FakeInventaireComptageRepository repository)
    {
        repository = new FakeInventaireComptageRepository();
        var types = new FakeTypeMatiereRepository();
        types.Items.Add(TypeMatiere.Creer("M3000L0", "Matiere test", Now, TypeId));
        return new InventaireComptageService(
            repository,
            types,
            new FakeCurrentUserService(),
            new FakeDateTimeProvider(Now));
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => TypeId;
        public string? UserName => "test.user";
        public string? Role => "Admin";
        public bool IsAuthenticated => true;
    }

    private sealed class FakeDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class FakeTypeMatiereRepository : ITypeMatiereRepository
    {
        public List<TypeMatiere> Items { get; } = [];

        public Task<IReadOnlyList<TypeMatiere>> GetAllAsync(bool? actif, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TypeMatiere>>(
                actif == true ? Items.Where(item => item.EstActif).ToArray() : Items.ToArray());

        public Task<TypeMatiere?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id));

        public Task<TypeMatiere?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(item => item.Code == normalizedCode));

        public Task AddAsync(TypeMatiere typeMatiere, CancellationToken cancellationToken)
        {
            Items.Add(typeMatiere);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInventaireComptageRepository : IInventaireComptageRepository
    {
        public List<InventaireConteneur> Conteneurs { get; } = [];
        public List<InventaireCategorieComptage> Categories { get; } = [];
        public List<InventaireFourComptage> Fours { get; } = [];
        public List<InventaireFonderie> Projections { get; } = [];
        public bool FailWhenAddingProjection { get; set; }
        public int RollbackCount { get; private set; }

        public Task<IReadOnlyList<InventaireConteneur>> GetConteneursAsync(DateOnly periode, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InventaireConteneur>>(Conteneurs.Where(item => item.Periode == periode).ToArray());

        public Task<IReadOnlyList<InventaireCategorieComptage>> GetCategoriesAsync(DateOnly periode, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InventaireCategorieComptage>>(Categories.Where(item => item.Periode == periode).ToArray());

        public Task<IReadOnlyList<InventaireFourComptage>> GetFoursAsync(DateOnly periode, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InventaireFourComptage>>(Fours.Where(item => item.Periode == periode).ToArray());

        public Task<InventaireConteneur?> GetConteneurAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Conteneurs.FirstOrDefault(item => item.Id == id));

        public Task<InventaireCategorieComptage?> GetCategorieAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Categories.FirstOrDefault(item => item.Id == id));

        public Task<InventaireFourComptage?> GetFourAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Fours.FirstOrDefault(item => item.Id == id));

        public Task AddAsync(InventaireConteneur ligne, CancellationToken cancellationToken = default)
        {
            Conteneurs.Add(ligne);
            return Task.CompletedTask;
        }

        public Task AddAsync(InventaireCategorieComptage ligne, CancellationToken cancellationToken = default)
        {
            Categories.Add(ligne);
            return Task.CompletedTask;
        }

        public Task AddAsync(InventaireFourComptage ligne, CancellationToken cancellationToken = default)
        {
            Fours.Add(ligne);
            return Task.CompletedTask;
        }

        public Task<InventaireFonderie?> GetProjectionAsync(DateOnly periode, Guid typeMatiereId, CancellationToken cancellationToken = default)
            => Task.FromResult(Projections.SingleOrDefault(item => item.Periode == periode && item.TypeMatiereId == typeMatiereId));

        public Task AddProjectionAsync(InventaireFonderie projection, CancellationToken cancellationToken = default)
        {
            if (FailWhenAddingProjection)
            {
                throw new InvalidOperationException("Projection test failure.");
            }

            Projections.Add(projection);
            return Task.CompletedTask;
        }

        public Task<IInventaireComptageTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IInventaireComptageTransaction>(new FakeTransaction(this));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private sealed class FakeTransaction(FakeInventaireComptageRepository repository) : IInventaireComptageTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                repository.RollbackCount++;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
