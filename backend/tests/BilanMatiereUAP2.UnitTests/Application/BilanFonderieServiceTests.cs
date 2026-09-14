using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Bilans;
using FluentValidation;
using Xunit;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class BilanFonderieServiceTests
{
    private static readonly Guid TypeId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private const string Code = "M3000L0";

    [Fact]
    public async Task Get_AvecToutesLesComposantes_AppliqueLaFormuleConfirmee()
    {
        var repository = new FakeRepository
        {
            // INPUT  = 100 + 40 = 140
            // OUTPUT = 90 + 12 + 25 = 127
            // ECART  = 13
            Stocks = { new BilanFonderieStock(TypeId, 100m, 90m) },
            Sorties = { new BilanFonderieSortieX3(Code, "KG", 40m, 4) },
            Dechets = { new BilanFonderieDechet(TypeId, 12m) },
            Receptions = { new BilanFonderieReception(Code, 25m, 3, 0) }
        };

        var response = await new BilanFonderieService(repository).GetAsync("2026-07");

        var ligne = Assert.Single(response.Lignes);
        Assert.Equal(140m, ligne.InputKg);
        Assert.Equal(127m, ligne.OutputKg);
        Assert.Equal(13m, ligne.EcartKg);
        Assert.Equal("Complet", ligne.Statut);
        Assert.Empty(ligne.Anomalies);
        Assert.Equal(13m, response.TotalEcartKg);
    }

    // RM-COM-003 : une composante absente ne devient jamais zero. Un stock de debut
    // inconnu rend l'INPUT inconnu, donc l'ecart inconnu.
    [Fact]
    public async Task Get_SansStockDebut_PropageNullEtNeRamenePasAZero()
    {
        var repository = new FakeRepository
        {
            Stocks = { new BilanFonderieStock(TypeId, null, 90m) },
            Sorties = { new BilanFonderieSortieX3(Code, "KG", 40m, 4) }
        };

        var response = await new BilanFonderieService(repository).GetAsync("2026-07");

        var ligne = Assert.Single(response.Lignes);
        Assert.Null(ligne.StockDebutKg);
        Assert.Null(ligne.InputKg);
        Assert.Null(ligne.EcartKg);
        Assert.Contains(BilanFonderieService.AnomalieStockDebut, ligne.Anomalies);
        Assert.Equal("Incomplet", ligne.Statut);
        // Un total partiel serait lu comme exhaustif.
        Assert.Null(response.TotalInputKg);
    }

    [Fact]
    public async Task Get_SansProjectionDInventaire_SignaleLeStockDeFinManquant()
    {
        var repository = new FakeRepository();

        var response = await new BilanFonderieService(repository).GetAsync("2026-07");

        var ligne = Assert.Single(response.Lignes);
        Assert.Null(ligne.StockFinKg);
        Assert.Null(ligne.OutputKg);
        Assert.Contains(BilanFonderieService.AnomalieStockFin, ligne.Anomalies);
    }

    // Les consommations X3 arrivent en unites mixtes. Additionner des BA ou des UN a
    // des KG produirait un total faux : elles sont exclues ET signalees.
    [Fact]
    public async Task Get_AvecUnitesMixtes_NeSommeQueLesKilogrammesEtSignaleLeReste()
    {
        var repository = new FakeRepository
        {
            Stocks = { new BilanFonderieStock(TypeId, 0m, 0m) },
            Sorties =
            {
                new BilanFonderieSortieX3(Code, "KG", 40m, 4),
                new BilanFonderieSortieX3(Code, "UN", 900m, 9),
                new BilanFonderieSortieX3(Code, "BA", 7m, 1)
            }
        };

        var response = await new BilanFonderieService(repository).GetAsync("2026-07");

        var ligne = Assert.Single(response.Lignes);
        Assert.Equal(40m, ligne.SortiesMagasinX3Kg);
        Assert.Contains(
            ligne.Anomalies,
            anomalie => anomalie.StartsWith(BilanFonderieService.AnomalieSortiesUnites, StringComparison.Ordinal)
                && anomalie.Contains("BA")
                && anomalie.Contains("UN"));
    }

    [Fact]
    public async Task Get_AvecReceptionsSansPoids_LesSignaleSansFausserLeTotal()
    {
        var repository = new FakeRepository
        {
            Stocks = { new BilanFonderieStock(TypeId, 0m, 0m) },
            Receptions = { new BilanFonderieReception(Code, 25m, 3, 2) },
            ReceptionsSansTypeMatiere = 5
        };

        var response = await new BilanFonderieService(repository).GetAsync("2026-07");

        var ligne = Assert.Single(response.Lignes);
        Assert.Equal(25m, ligne.ReceptionsUsinage2Kg);
        Assert.Contains(ligne.Anomalies, a => a == $"{BilanFonderieService.AnomalieReceptionsPoids}:2");
        Assert.Contains(ligne.Anomalies, a => a == $"{BilanFonderieService.AnomalieReceptionsTypeMatiere}:5");
    }

    // Le signe distingue un manquant d'un excedent : jamais de Math.Abs.
    [Fact]
    public async Task Get_QuandOutputDepasseInput_ConserveUnEcartNegatif()
    {
        var repository = new FakeRepository
        {
            Stocks = { new BilanFonderieStock(TypeId, 10m, 50m) },
            Dechets = { new BilanFonderieDechet(TypeId, 5m) }
        };

        var response = await new BilanFonderieService(repository).GetAsync("2026-07");

        var ligne = Assert.Single(response.Lignes);
        Assert.Equal(-45m, ligne.EcartKg);
    }

    [Fact]
    public async Task Get_AvecPeriodeInvalide_LeveUneValidationException()
    {
        var service = new BilanFonderieService(new FakeRepository());

        await Assert.ThrowsAsync<ValidationException>(() => service.GetAsync("07-2026"));
    }

    [Theory]
    [InlineData("2026-07-10", "2026-07-20", false, false)]
    [InlineData("2026-07-01", "2026-07-20", true, false)]
    [InlineData("2026-07-10", "2026-07-31", false, true)]
    [InlineData("2026-07-01", "2026-07-31", true, true)]
    public async Task Intervalle_NExtrapolePasLesStocksMensuels(string debut, string fin, bool stockDebut, bool stockFin)
    {
        var repository = new FakeRepository { Stocks = { new BilanFonderieStock(TypeId, 100m, 90m) } };
        var response = await new BilanFonderieService(repository).GetIntervalleAsync(debut, fin);
        var ligne = Assert.Single(response.Bilan.Lignes);
        Assert.Equal(stockDebut ? 100m : (decimal?)null, ligne.StockDebutKg);
        Assert.Equal(stockFin ? 90m : (decimal?)null, ligne.StockFinKg);
        Assert.Equal(stockDebut && stockFin ? 10m : (decimal?)null, ligne.EcartKg);
        Assert.Equal(DateOnly.Parse(debut), response.DateDebut);
        Assert.Equal(DateOnly.Parse(fin), response.DateFin);
    }

    [Fact]
    public async Task Intervalle_MultiMois_UtiliseLesEtatsAuxBornesSansSommerLesStocks()
    {
        var repository = new FakeRepository();
        repository.StocksParMois[new DateOnly(2026, 7, 1)] = [new(TypeId, 100m, 80m)];
        repository.StocksParMois[new DateOnly(2026, 8, 1)] = [new(TypeId, 80m, 60m)];
        var response = await new BilanFonderieService(repository).GetIntervalleAsync("2026-07-01", "2026-08-31");
        var ligne = Assert.Single(response.Bilan.Lignes);
        Assert.Equal(100m, ligne.StockDebutKg);
        Assert.Equal(60m, ligne.StockFinKg);
        Assert.Equal(40m, ligne.EcartKg);
    }

    private sealed class FakeRepository : IBilanFonderieRepository
    {
        public List<BilanFonderieStock> Stocks { get; } = [];
        public Dictionary<DateOnly, List<BilanFonderieStock>> StocksParMois { get; } = [];
        public List<BilanFonderieSortieX3> Sorties { get; } = [];
        public List<BilanFonderieDechet> Dechets { get; } = [];
        public List<BilanFonderieReception> Receptions { get; } = [];
        public int ReceptionsSansTypeMatiere { get; init; }

        public Task<IReadOnlyList<BilanFonderieTypeMatiere>> GetTypesMatiereActifsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BilanFonderieTypeMatiere>>([new BilanFonderieTypeMatiere(TypeId, Code)]);

        public Task<IReadOnlyList<BilanFonderieStock>> GetStocksAsync(DateOnly periode, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BilanFonderieStock>>(StocksParMois.GetValueOrDefault(periode) ?? Stocks);

        public Task<IReadOnlyList<BilanFonderieSortieX3>> GetSortiesMagasinX3Async(DateOnly dateDebut, DateOnly dateFin, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BilanFonderieSortieX3>>(Sorties);

        public Task<IReadOnlyList<BilanFonderieDechet>> GetDechetsAsync(DateOnly dateDebut, DateOnly dateFin, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BilanFonderieDechet>>(Dechets);

        public Task<IReadOnlyList<BilanFonderieReception>> GetReceptionsUsinage2Async(DateOnly dateDebut, DateOnly dateFin, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<BilanFonderieReception>>(Receptions);

        public Task<int> CompterReceptionsSansTypeMatiereAsync(DateOnly dateDebut, DateOnly dateFin, CancellationToken ct = default)
            => Task.FromResult(ReceptionsSansTypeMatiere);
    }
}
