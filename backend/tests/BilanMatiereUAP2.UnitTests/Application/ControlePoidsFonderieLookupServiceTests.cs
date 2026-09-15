using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Services;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class ControlePoidsFonderieLookupServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly ReceptionDate = new(2026, 7, 24);

    [Fact]
    public async Task ResolvePoidsPhysiqueAsync_ReturnsPoidsPhysiqueManquant_WhenNoCandidates()
    {
        var repository = new FakeControlePoidsRepository();
        var types = new FakeTypeRepository(new[] { TypeMatiere.Creer("M3000L0", "Matière", Now) });
        var service = new ControlePoidsFonderieLookupService(repository, types);

        var result = await service.ResolvePoidsPhysiqueAsync("OF001", "ART001", "M3000L0", ReceptionDate, CancellationToken.None);

        Assert.Equal("PoidsPhysiqueManquant", result.Statut);
        Assert.Null(result.PoidsPhysiqueUnitaireKg);
        Assert.Empty(result.Candidats);
    }

    [Fact]
    public async Task ResolvePoidsPhysiqueAsync_ReturnsControleNonValide_WhenNoValidCandidates()
    {
        var type = TypeMatiere.Creer("M3000L0", "Matière", Now);
        var invalidControle = ControlePoidsFonderie.Creer(
            dateControle: new DateOnly(2026, 7, 23),
            numeroOF: "OF001",
            codeArticle: "ART001",
            typeMatiereId: type.Id,
            numeroLotOuSerie: null,
            poidsPhysiqueUnitaireKg: 10m,
            nombreEchantillons: 1,
            observation: "Brouillon",
            creeParUtilisateurId: Guid.NewGuid(),
            creeParNom: "Test",
            creeLeUtc: Now);

        var repository = new FakeControlePoidsRepository(new[] { invalidControle });
        var types = new FakeTypeRepository(new[] { type });
        var service = new ControlePoidsFonderieLookupService(repository, types);

        var result = await service.ResolvePoidsPhysiqueAsync("OF001", "ART001", "M3000L0", ReceptionDate, CancellationToken.None);

        Assert.Equal("ControleNonValide", result.Statut);
        Assert.DoesNotContain(result.Candidats, candidate => candidate.Statut == "Valide");
        Assert.Single(result.Candidats);
    }

    [Fact]
    public async Task ResolvePoidsPhysiqueAsync_ReturnsAverage_WhenMultipleValidCandidates()
    {
        var type = TypeMatiere.Creer("M3000L0", "Matière", Now);
        var first = ControlePoidsFonderie.Creer(
            dateControle: new DateOnly(2026, 7, 22),
            numeroOF: "OF001",
            codeArticle: "ART001",
            typeMatiereId: type.Id,
            numeroLotOuSerie: null,
            poidsPhysiqueUnitaireKg: 10m,
            nombreEchantillons: 1,
            observation: "First",
            creeParUtilisateurId: Guid.NewGuid(),
            creeParNom: "Test",
            creeLeUtc: Now);
        first.Valider(Guid.NewGuid(), "Validator", Now);

        var second = ControlePoidsFonderie.Creer(
            dateControle: new DateOnly(2026, 7, 23),
            numeroOF: "OF001",
            codeArticle: "ART001",
            typeMatiereId: type.Id,
            numeroLotOuSerie: null,
            poidsPhysiqueUnitaireKg: 11m,
            nombreEchantillons: 1,
            observation: "Second",
            creeParUtilisateurId: Guid.NewGuid(),
            creeParNom: "Test",
            creeLeUtc: Now);
        second.Valider(Guid.NewGuid(), "Validator", Now);

        var repository = new FakeControlePoidsRepository(new[] { first, second });
        var types = new FakeTypeRepository(new[] { type });
        var service = new ControlePoidsFonderieLookupService(repository, types);

        var result = await service.ResolvePoidsPhysiqueAsync("OF001", "ART001", "M3000L0", ReceptionDate, CancellationToken.None);

        Assert.Equal("Trouve", result.Statut);
        Assert.Equal(10.5m, result.PoidsPhysiqueUnitaireKg);
        Assert.Null(result.ControlePoidsFonderieId);
        Assert.Contains("Moyenne", result.Message!);
        Assert.Equal(2, result.Candidats.Count);
    }

    [Fact]
    public async Task ResolvePoidsPhysiqueAsync_ReturnsTrouve_WhenSingleValidCandidate()
    {
        var type = TypeMatiere.Creer("M3000L0", "Matière", Now);
        var controle = ControlePoidsFonderie.Creer(
            dateControle: new DateOnly(2026, 7, 23),
            numeroOF: "OF001",
            codeArticle: "ART001",
            typeMatiereId: type.Id,
            numeroLotOuSerie: null,
            poidsPhysiqueUnitaireKg: 10m,
            nombreEchantillons: 1,
            observation: "Unique",
            creeParUtilisateurId: Guid.NewGuid(),
            creeParNom: "Test",
            creeLeUtc: Now);
        controle.Valider(Guid.NewGuid(), "Validator", Now);

        var repository = new FakeControlePoidsRepository(new[] { controle });
        var types = new FakeTypeRepository(new[] { type });
        var service = new ControlePoidsFonderieLookupService(repository, types);

        var result = await service.ResolvePoidsPhysiqueAsync("OF001", "ART001", "M3000L0", ReceptionDate, CancellationToken.None);

        Assert.Equal("Trouve", result.Statut);
        Assert.Equal(10m, result.PoidsPhysiqueUnitaireKg);
        Assert.Equal(controle.Id, result.ControlePoidsFonderieId);
        Assert.Equal("ControlePoidsFonderie", result.Source);
        Assert.Single(result.Candidats);
    }

    private sealed class FakeControlePoidsRepository : IControlePoidsFonderieRepository
    {
        private readonly IReadOnlyList<ControlePoidsFonderie> _items;

        public FakeControlePoidsRepository(IReadOnlyList<ControlePoidsFonderie>? items = null)
        {
            _items = items ?? Array.Empty<ControlePoidsFonderie>();
        }

        public Task<ControlePoidsFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_items.SingleOrDefault(item => item.Id == id));

        public Task<PagedResult<ControlePoidsFonderie>> GetPagedAsync(ControlesPoidsFonderieFilter filter, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<ControlePoidsFonderie>(_items, _items.Count));

        public Task<IReadOnlyList<ControlePoidsFonderie>> FindCandidatesAsync(string codeArticle, Guid typeMatiereId, string? numeroOF, DateOnly dateReception, CancellationToken cancellationToken)
        {
            var normalizedArticle = codeArticle.Trim().ToUpperInvariant();
            var normalizedOF = string.IsNullOrWhiteSpace(numeroOF) ? null : numeroOF.Trim().ToUpperInvariant();
            var results = _items.Where(item =>
                item.CodeArticle == normalizedArticle &&
                item.TypeMatiereId == typeMatiereId &&
                item.DateControle <= dateReception &&
                item.Statut != StatutControlePoidsFonderie.Annule &&
                (normalizedOF == null || item.NumeroOF == normalizedOF))
                .OrderByDescending(item => item.DateControle)
                .ThenByDescending(item => item.CreeLeUtc)
                .ToArray();
            return Task.FromResult((IReadOnlyList<ControlePoidsFonderie>)results);
        }

        public Task AddAsync(ControlePoidsFonderie controle, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTypeRepository : ITypeMatiereRepository
    {
        private readonly IReadOnlyList<TypeMatiere> _types;

        public FakeTypeRepository(IReadOnlyList<TypeMatiere> types)
        {
            _types = types;
        }

        public Task<IReadOnlyList<TypeMatiere>> GetAllAsync(bool? actif, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<TypeMatiere>)_types.Where(type => actif is null || type.EstActif == actif).ToArray());

        public Task<TypeMatiere?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_types.SingleOrDefault(type => type.Id == id));

        public Task<TypeMatiere?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken) =>
            Task.FromResult(_types.SingleOrDefault(type => type.Code == normalizedCode));

        public Task AddAsync(TypeMatiere typeMatiere, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
