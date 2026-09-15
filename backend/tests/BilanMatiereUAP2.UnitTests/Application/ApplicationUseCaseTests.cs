using System.Reflection;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.DechetsFonderie;
using BilanMatiereUAP2.Application.Features.TypesMatiere;
using BilanMatiereUAP2.Contracts.DechetsFonderie;
using BilanMatiereUAP2.Contracts.TypesMatiere;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class ApplicationUseCaseTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateTypeMatiere_Doublon_RetourneUnConflit()
    {
        var types = new FakeTypeRepository();
        types.Items.Add(TypeMatiere.Creer("M3000L0", "Matière", Now));
        var service = CreateTypeService(types);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            new CreateTypeMatiereCommand(new CreateTypeMatiereRequest(" m3000l0 ", "Doublon")),
            default));
    }

    [Fact]
    public async Task CreateDechet_TypeMatiereInexistant_Echoue()
    {
        var service = CreateDechetService(new FakeTypeRepository(), new FakeDechetRepository());
        var request = Request(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(new(request), default));
    }

    [Fact]
    public async Task CreateDechet_TypeMatiereInactif_RetourneUnConflit()
    {
        var types = new FakeTypeRepository();
        var type = TypeMatiere.Creer("M3000L0", "Matière", Now);
        type.Desactiver(Now);
        types.Items.Add(type);
        var service = CreateDechetService(types, new FakeDechetRepository());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new(Request(type.Id)), default));
    }

    [Fact]
    public async Task GetDechets_FiltreParPeriodeMatiereEtType()
    {
        var fixture = CreateWasteFixture();

        var response = await fixture.Service.GetAllAsync(
            new GetDechetsFonderieQuery("2026-06", fixture.M3000.Id, "Bavure"),
            default);

        var item = Assert.Single(response.Items);
        Assert.Equal(fixture.M3000.Id, item.TypeMatiereId);
        Assert.Equal("Bavure", item.TypeDechet);
        Assert.Equal(new DateOnly(2026, 6, 15), item.DateDeclaration);
    }

    [Fact]
    public async Task GetResume_CalculeLesTotauxParMatiere()
    {
        var fixture = CreateWasteFixture();

        var response = await fixture.Service.GetResumeAsync(new("2026-06"), default);

        Assert.Equal(35m, response.TotalGeneralKg);
        var m3000 = Assert.Single(response.TotauxParTypeMatiere, item => item.TypeMatiereId == fixture.M3000.Id);
        Assert.Equal(10m, m3000.TotalBavureKg);
        Assert.Equal(20m, m3000.TotalPoudreKg);
        var m3010 = Assert.Single(response.TotauxParTypeMatiere, item => item.TypeMatiereCode == "M3010L0");
        Assert.Equal(5m, m3010.TotalBavureKg);
        Assert.Equal(0m, m3010.TotalPoudreKg);
    }

    [Fact]
    public async Task GetResume_PeriodeSansDonnees_RetourneUneListeVide()
    {
        var fixture = CreateWasteFixture();

        var response = await fixture.Service.GetResumeAsync(new("2030-01"), default);

        Assert.Empty(response.TotauxParTypeMatiere);
        Assert.Equal(0m, response.TotalGeneralKg);
    }

    [Fact]
    public async Task DeleteDechet_ExclutLaLigneDuResume()
    {
        var fixture = CreateWasteFixture();
        var target = fixture.Dechets.Items.First();

        await fixture.Service.DeleteAsync(new(target.Id), default);
        var response = await fixture.Service.GetResumeAsync(new("2026-06"), default);

        Assert.Equal(25m, response.TotalGeneralKg);
    }

    private static TypeMatiereService CreateTypeService(FakeTypeRepository repository)
    {
        return new TypeMatiereService(
            repository,
            new FakeUnitOfWork(),
            new FakeDateTimeProvider(),
            new CreateTypeMatiereRequestValidator(),
            new UpdateTypeMatiereRequestValidator());
    }

    private static DechetFonderieService CreateDechetService(
        FakeTypeRepository types,
        FakeDechetRepository dechets)
    {
        return new DechetFonderieService(
            dechets,
            types,
            new FakeUnitOfWork(),
            new FakeCurrentUserService(),
            new FakeDateTimeProvider(),
            new CreateDechetFonderieRequestValidator(),
            new UpdateDechetFonderieRequestValidator());
    }

    private static CreateDechetFonderieRequest Request(Guid typeMatiereId)
    {
        return new CreateDechetFonderieRequest(
            new DateOnly(2026, 6, 30),
            typeMatiereId,
            "Bavure",
            125.5m,
            "Bavures triées");
    }

    private static WasteFixture CreateWasteFixture()
    {
        var types = new FakeTypeRepository();
        var m3000 = TypeMatiere.Creer("M3000L0", "Matière M3000L0", Now);
        var m3010 = TypeMatiere.Creer("M3010L0", "Matière M3010L0", Now);
        types.Items.AddRange([m3000, m3010]);
        var dechets = new FakeDechetRepository();
        dechets.Items.Add(CreateWaste(m3000, new DateOnly(2026, 6, 15), TypeDechet.Bavure, 10m));
        dechets.Items.Add(CreateWaste(m3000, new DateOnly(2026, 6, 16), TypeDechet.Poudre, 20m));
        dechets.Items.Add(CreateWaste(m3010, new DateOnly(2026, 6, 17), TypeDechet.Bavure, 5m));
        dechets.Items.Add(CreateWaste(m3000, new DateOnly(2026, 7, 1), TypeDechet.Bavure, 99m));
        return new WasteFixture(CreateDechetService(types, dechets), dechets, m3000);
    }

    private static DechetFonderie CreateWaste(TypeMatiere type, DateOnly date, TypeDechet kind, decimal weight)
    {
        var entity = DechetFonderie.Creer(date, type.Id, kind, weight, null, null, "Test", Now);
        // Depuis l'introduction du cycle de vie, Creer produit un BROUILLON, exclu des totaux.
        // Le resume ne porte que sur les dechets VALIDES : on valide donc la donnee de test.
        entity.Valider(null, "Test", Now);
        typeof(DechetFonderie)
            .GetProperty(nameof(DechetFonderie.TypeMatiere), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(entity, type);
        return entity;
    }

    private sealed record WasteFixture(
        DechetFonderieService Service,
        FakeDechetRepository Dechets,
        TypeMatiere M3000);

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => Now;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("20000000-0000-0000-0000-000000000001");
        public string UserName => "Responsable Fonderie";
        public string Role => "ResponsableFonderie";
        public bool IsAuthenticated => true;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeTypeRepository : ITypeMatiereRepository
    {
        public List<TypeMatiere> Items { get; } = [];

        public Task<IReadOnlyList<TypeMatiere>> GetAllAsync(bool? actif, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TypeMatiere>>(Items.Where(item => actif is null || item.EstActif == actif).ToArray());

        public Task<TypeMatiere?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));

        public Task<TypeMatiere?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Code == normalizedCode));

        public Task AddAsync(TypeMatiere typeMatiere, CancellationToken cancellationToken)
        {
            Items.Add(typeMatiere);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDechetRepository : IDechetFonderieRepository
    {
        public List<DechetFonderie> Items { get; } = [];

        public Task<DechetFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id && !item.EstSupprime));

        public Task<PagedResult<DechetFonderie>> GetPagedAsync(
            DechetsFonderieFilter filter,
            CancellationToken cancellationToken)
        {
            var query = Items.Where(item =>
                !item.EstSupprime &&
                item.DateDeclaration >= filter.DateDebutIncluse &&
                item.DateDeclaration < filter.DateFinExclusive &&
                (filter.TypeMatiereId is null || item.TypeMatiereId == filter.TypeMatiereId) &&
                (filter.TypeDechet is null || item.TypeDechet == filter.TypeDechet));
            var total = query.Count();
            var page = query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToArray();
            return Task.FromResult(new PagedResult<DechetFonderie>(page, total));
        }

        public Task<IReadOnlyList<ResumeDechetsFonderieData>> GetResumeAsync(
            DateOnly dateDebutIncluse,
            DateOnly dateFinExclusive,
            CancellationToken cancellationToken)
        {
            // Meme filtre que DechetFonderieRepository : seuls les dechets VALIDES comptent.
            // Un dechet annule reste dans l'historique mais sort des totaux.
            var result = Items
                .Where(item => !item.EstSupprime
                    && item.Statut == StatutDechet.Valide
                    && item.DateDeclaration >= dateDebutIncluse
                    && item.DateDeclaration < dateFinExclusive)
                .GroupBy(item => new { item.TypeMatiereId, item.TypeMatiere.Code })
                .Select(group => new ResumeDechetsFonderieData(
                    group.Key.TypeMatiereId,
                    group.Key.Code,
                    group.Where(item => item.TypeDechet == TypeDechet.Bavure).Sum(item => item.PoidsKg),
                    group.Where(item => item.TypeDechet == TypeDechet.Poudre).Sum(item => item.PoidsKg)))
                .ToArray();
            return Task.FromResult<IReadOnlyList<ResumeDechetsFonderieData>>(result);
        }

        public Task AddAsync(DechetFonderie dechet, CancellationToken cancellationToken)
        {
            Items.Add(dechet);
            return Task.CompletedTask;
        }
    }
}
