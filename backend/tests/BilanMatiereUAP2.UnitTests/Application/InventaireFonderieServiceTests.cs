using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.InventairesFonderie;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class InventaireFonderieServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly DateTest = new(2026, 7, 22);

    private readonly FakeInventaireFonderieRepository _repository = new();
    private readonly FakeTypeMatiereRepository _typeMatiereRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUserService _currentUser = new();
    private readonly FakeDateTimeProvider _dateTimeProvider = new(Now);

    private readonly CreateInventaireConteneurValidator _createConteneurValidator = new();
    private readonly UpdateInventaireConteneurValidator _updateConteneurValidator = new();
    private readonly CreateInventaireCategorieValidator _createCategorieValidator = new();
    private readonly UpdateInventaireCategorieValidator _updateCategorieValidator = new();
    private readonly CreateInventaireFourValidator _createFourValidator = new();
    private readonly UpdateInventaireFourValidator _updateFourValidator = new();
    private readonly CreateInventaireFonderieRequestValidator _createLegacyValidator = new();
    private readonly UpdateInventaireFonderieRequestValidator _updateLegacyValidator = new();

    private readonly InventaireFonderieService _service;

    public InventaireFonderieServiceTests()
    {
        _typeMatiereRepository.Items.Add(TypeMatiere.Creer("ALU", "Aluminium Standard", Now));

        _service = new InventaireFonderieService(
            _repository,
            _typeMatiereRepository,
            _unitOfWork,
            _currentUser,
            _dateTimeProvider,
            _createConteneurValidator,
            _updateConteneurValidator,
            _createCategorieValidator,
            _updateCategorieValidator,
            _createFourValidator,
            _updateFourValidator,
            _createLegacyValidator,
            _updateLegacyValidator);
    }

    // ── Partie 1 : Conteneurs ────────────────────────────────────────

    [Fact]
    public async Task CreateConteneurAsync_AvecDonneesValides_CreeEtRetourneResponse()
    {
        var request = new CreateInventaireConteneurRequest(DateTest, "CNT-001", "OF-100", "ALU", 150.5m, "Observation conteneur");

        var response = await _service.CreateConteneurAsync(request, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("CNT-001", response.NumeroConteneur);
        Assert.Equal("OF-100", response.NumeroOF);
        Assert.Equal("ALU", response.CodeTypeMatiere);
        Assert.Equal(150.5m, response.PoidsReelKg);
        Assert.Equal("Observation conteneur", response.Observation);
        Assert.Equal("Brouillon", response.Statut);
        Assert.Single(_repository.Conteneurs);
    }

    [Fact]
    public async Task ValidateConteneurAsync_ConteneurExistant_BrouillonAValide()
    {
        var conteneur = InventaireConteneurOf.Creer(DateTest, "CNT-001", "OF-100", "ALU", 150m, null, Now);
        _repository.Conteneurs.Add(conteneur);

        var response = await _service.ValidateConteneurAsync(conteneur.Id, CancellationToken.None);

        Assert.Equal("Valide", response.Statut);
        Assert.Equal("test.user", response.ValidePar);
    }

    [Fact]
    public async Task DeleteConteneurAsync_Brouillon_SupprimePhysiquement()
    {
        var conteneur = InventaireConteneurOf.Creer(DateTest, "CNT-001", "OF-100", "ALU", 150m, null, Now);
        _repository.Conteneurs.Add(conteneur);

        await _service.DeleteConteneurAsync(conteneur.Id, CancellationToken.None);

        Assert.Empty(_repository.Conteneurs);
    }

    [Fact]
    public async Task DeleteConteneurAsync_Valide_LanceConflictException()
    {
        var conteneur = InventaireConteneurOf.Creer(DateTest, "CNT-001", "OF-100", "ALU", 150m, null, Now);
        conteneur.Valider("admin", Now);
        _repository.Conteneurs.Add(conteneur);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeleteConteneurAsync(conteneur.Id, CancellationToken.None));
    }

    // ── Partie 2 : Catégories ────────────────────────────────────────

    [Fact]
    public async Task CreateCategorieAsync_AvecDonneesValides_CreeEtRetourneResponse()
    {
        var request = new CreateInventaireCategorieRequest(DateTest, CategorieInventaireFonderie.Rebut, "ALU", 250m, "Com dechets");

        var response = await _service.CreateCategorieAsync(request, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Rebut", response.Categorie);
        Assert.Equal("ALU", response.CodeTypeMatiere);
        Assert.Equal(250m, response.PoidsReelKg);
        Assert.True(response.InclusBilanFonderie);
        Assert.True(response.VisibleRapportInventaire);
        Assert.Single(_repository.Categories);
    }

    // ── Partie 3 : Fours ────────────────────────────────────────────

    [Fact]
    public async Task CreateFourAsync_FourExcluDuBilan()
    {
        var request = new CreateInventaireFourRequest(DateTest, "F01", "ALU", 500m, "Charge four");

        var response = await _service.CreateFourAsync(request, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("F01", response.CodeFour);
        Assert.Equal("ALU", response.CodeTypeMatiere);
        Assert.Equal(500m, response.QuantiteKg);
        Assert.False(response.InclusBilanFonderie);
        Assert.True(response.VisibleRapportInventaire);
        Assert.Single(_repository.Fours);
    }

    // ── Synthèse mensuelle ──────────────────────────────────────────

    [Fact]
    public async Task GetSyntheseAsync_CalculeLesTotauxCorrectement()
    {
        _repository.SyntheseRows = new List<InventaireSyntheseRow>
        {
            new("ALU", 100m, 50m, 20m),
            new("ZINC", 200m, 100m, 40m)
        };

        var result = await _service.GetSyntheseAsync(7, 2026, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(7, result.Mois);
        Assert.Equal(2026, result.Annee);
        Assert.Equal(2, result.Lignes.Count);

        // ALU : Rapport = 100+50+20 = 170, Bilan = 100+50 = 150
        var alu = result.Lignes.First(l => l.CodeTypeMatiere == "ALU");
        Assert.Equal(100m, alu.Partie1Kg);
        Assert.Equal(50m, alu.Partie2Kg);
        Assert.Equal(20m, alu.Partie3FourKg);
        Assert.Equal(170m, alu.TotalRapportInventaireKg);
        Assert.Equal(150m, alu.TotalBilanMatiereKg);

        // Totaux généraux
        Assert.Equal(300m, result.TotalGeneral.Partie1Kg);
        Assert.Equal(150m, result.TotalGeneral.Partie2Kg);
        Assert.Equal(60m, result.TotalGeneral.Partie3FourKg);
        Assert.Equal(510m, result.TotalGeneral.TotalRapportInventaireKg); // 300 + 150 + 60
        Assert.Equal(450m, result.TotalGeneral.TotalBilanMatiereKg);      // 300 + 150
    }

    // ── Fake Implementations ────────────────────────────────────────

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
        public string? UserName => "test.user";
        public string? Role => "Admin";
        public bool IsAuthenticated => true;
    }

    private sealed class FakeDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeTypeMatiereRepository : ITypeMatiereRepository
    {
        public List<TypeMatiere> Items { get; } = new();

        public Task<TypeMatiere?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<TypeMatiere?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<TypeMatiere>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TypeMatiere>>(Items);

        public Task<IReadOnlyList<TypeMatiere>> GetAllAsync(bool? actifUniquement = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TypeMatiere>>(actifUniquement == true ? Items.Where(x => x.EstActif).ToList() : Items);

        public Task<IReadOnlyList<TypeMatiere>> GetActifsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TypeMatiere>>(Items.Where(x => x.EstActif).ToList());

        public Task AddAsync(TypeMatiere typeMatiere, CancellationToken cancellationToken = default)
        {
            Items.Add(typeMatiere);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeInventaireFonderieRepository : IInventaireFonderieRepository
    {
        public List<InventaireConteneurOf> Conteneurs { get; } = new();
        public List<InventaireCategorie> Categories { get; } = new();
        public List<InventaireFour> Fours { get; } = new();
        public List<InventaireFonderie> LegacyItems { get; } = new();
        public List<InventaireSyntheseRow> SyntheseRows { get; set; } = new();

        public Task<PagedResult<InventaireConteneurOf>> SearchConteneursAsync(InventaireConteneurCriteria criteria, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<InventaireConteneurOf>(Conteneurs, Conteneurs.Count));

        public Task<InventaireConteneurOf?> GetConteneurByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Conteneurs.FirstOrDefault(x => x.Id == id));

        public Task<bool> ConteneurExisteAsync(DateOnly dateInventaire, string numeroConteneur, string numeroOF, string codeTypeMatiere, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AddConteneurAsync(InventaireConteneurOf entity, CancellationToken cancellationToken = default)
        {
            Conteneurs.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveConteneurAsync(InventaireConteneurOf entity, CancellationToken cancellationToken = default)
        {
            Conteneurs.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<PagedResult<InventaireCategorie>> SearchCategoriesAsync(InventaireCategorieCriteria criteria, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<InventaireCategorie>(Categories, Categories.Count));

        public Task<InventaireCategorie?> GetCategorieByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Categories.FirstOrDefault(x => x.Id == id));

        public Task AddCategorieAsync(InventaireCategorie entity, CancellationToken cancellationToken = default)
        {
            Categories.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveCategorieAsync(InventaireCategorie entity, CancellationToken cancellationToken = default)
        {
            Categories.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<PagedResult<InventaireFour>> SearchFoursAsync(InventaireFourCriteria criteria, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<InventaireFour>(Fours, Fours.Count));

        public Task<InventaireFour?> GetFourByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Fours.FirstOrDefault(x => x.Id == id));

        public Task AddFourAsync(InventaireFour entity, CancellationToken cancellationToken = default)
        {
            Fours.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveFourAsync(InventaireFour entity, CancellationToken cancellationToken = default)
        {
            Fours.Remove(entity);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<InventaireSyntheseRow>> GetSyntheseAsync(int mois, int annee, string? codeTypeMatiere, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<InventaireSyntheseRow>>(SyntheseRows);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        // Legacy methods
        public Task<PagedResult<InventaireFonderie>> GetPagedAsync(InventairesFonderieFilter filter, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<InventaireFonderie>(LegacyItems, LegacyItems.Count));

        public Task<InventaireFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(LegacyItems.FirstOrDefault(x => x.Id == id));

        public Task<bool> ExistsActiveAsync(DateOnly periode, Guid typeMatiereId, Guid? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AddAsync(InventaireFonderie entity, CancellationToken cancellationToken = default)
        {
            LegacyItems.Add(entity);
            return Task.CompletedTask;
        }
    }
}
