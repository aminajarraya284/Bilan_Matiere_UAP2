using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Alertes;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class AlerteGenerationServiceTests
{
    private static readonly DateTime PremierCalculUtc = new(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("2026-13")]
    public async Task Recalculer_PeriodeInvalide_RefuseAvantLaDetection(string periode)
    {
        var detection = new FakeDetectionService();
        var service = CreerService(detection: detection);

        await Assert.ThrowsAsync<ValidationException>(() => service.RecalculerAsync(periode));

        Assert.Equal(0, detection.NombreAppels);
    }

    [Fact]
    public async Task Recalculer_AucuneAnomalie_NeCreeRien()
    {
        var repository = new FakeAlerteRepository();

        var resultat = await CreerService(repository).RecalculerAsync("2026-01");

        Assert.Equal(0, resultat.NombreDetections);
        Assert.Equal(0, resultat.NombreCreees);
        Assert.Empty(repository.Alertes);
        Assert.Equal(1, repository.NombreSauvegardes);
    }

    [Fact]
    public async Task Recalculer_DeuxFoisLaMemeDetection_NeDupliquePasEtConserveLaPremiereDate()
    {
        var repository = new FakeAlerteRepository();
        var horloge = new FakeDateTimeProvider { UtcNow = PremierCalculUtc };
        var detection = new FakeDetectionService { Detections = [Detection()] };
        var service = CreerService(repository, detection, horloge);

        var premier = await service.RecalculerAsync("2026-01");
        horloge.UtcNow = PremierCalculUtc.AddHours(2);
        var second = await service.RecalculerAsync("2026-01");

        var alerte = Assert.Single(repository.Alertes);
        Assert.Equal(1, premier.NombreCreees);
        Assert.Equal(0, second.NombreCreees);
        Assert.Equal(1, second.NombreMisesAJour);
        Assert.Equal(PremierCalculUtc, alerte.DetecteeLeUtc);
        Assert.Equal(PremierCalculUtc.AddHours(2), alerte.DerniereDetectionLeUtc);
    }

    [Fact]
    public async Task Recalculer_DetectionDupliqueeDansLeMemeFlux_NeCreeQuUneAlerte()
    {
        var repository = new FakeAlerteRepository();
        var detection = new FakeDetectionService { Detections = [Detection(), Detection()] };

        var resultat = await CreerService(repository, detection).RecalculerAsync("2026-01");

        Assert.Equal(1, resultat.NombreDetections);
        Assert.Single(repository.Alertes);
    }

    [Fact]
    public async Task Recalculer_DisparitionDuneConditionSure_ResoutAutomatiquement()
    {
        var repository = new FakeAlerteRepository();
        var detection = new FakeDetectionService { Detections = [Detection(resolutionAutomatique: true)] };
        var service = CreerService(repository, detection);
        await service.RecalculerAsync("2026-01");
        detection.Detections = [];

        var resultat = await service.RecalculerAsync("2026-01");

        var alerte = Assert.Single(repository.Alertes);
        Assert.Equal(1, resultat.NombreResoluesAutomatiquement);
        Assert.Equal(StatutAlerte.Resolue, alerte.Statut);
        Assert.False(alerte.EstActive);
    }

    [Fact]
    public async Task Recalculer_DisparitionDuneConditionHumaine_ConserveLAlerteActive()
    {
        var repository = new FakeAlerteRepository();
        var detection = new FakeDetectionService { Detections = [Detection(resolutionAutomatique: false)] };
        var service = CreerService(repository, detection);
        await service.RecalculerAsync("2026-01");
        detection.Detections = [];

        var resultat = await service.RecalculerAsync("2026-01");

        Assert.Equal(0, resultat.NombreResoluesAutomatiquement);
        Assert.True(Assert.Single(repository.Alertes).EstActive);
    }

    [Fact]
    public async Task Recalculer_DeuxOfDistincts_ProduitDeuxClesDistinctesSansMelangerLesPieces()
    {
        var repository = new FakeAlerteRepository();
        var detection = new FakeDetectionService
        {
            Detections =
            [
                Detection(numeroOf: "OF-1", donnees: new Dictionary<string, string?> { ["quantitePieces"] = "10", ["unite"] = "pieces" }),
                Detection(numeroOf: "OF-2", donnees: new Dictionary<string, string?> { ["quantitePieces"] = "20", ["unite"] = "pieces" })
            ]
        };

        await CreerService(repository, detection).RecalculerAsync("2026-01");

        Assert.Equal(2, repository.Alertes.Count);
        Assert.All(repository.Alertes, alerte => Assert.Contains("\"unite\":\"pieces\"", alerte.DonneesAssocieesJson));
        Assert.Equal(2, repository.Alertes.Select(alerte => alerte.CleDeduplication).Distinct().Count());
    }

    [Fact]
    public async Task Recalculer_ExceptionTechniqueDeDetection_EstPropageeSansSauvegarde()
    {
        var repository = new FakeAlerteRepository();
        var detection = new FakeDetectionService { Exception = new InvalidOperationException("SQL indisponible") };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreerService(repository, detection).RecalculerAsync("2026-01"));

        Assert.Equal("SQL indisponible", exception.Message);
        Assert.Equal(0, repository.NombreSauvegardes);
    }

    private static AlerteGenerationService CreerService(
        FakeAlerteRepository? repository = null,
        FakeDetectionService? detection = null,
        FakeDateTimeProvider? horloge = null)
        => new(
            detection ?? new FakeDetectionService(),
            repository ?? new FakeAlerteRepository(),
            horloge ?? new FakeDateTimeProvider { UtcNow = PremierCalculUtc });

    private static AlerteDetection Detection(
        string numeroOf = "OF-1",
        bool resolutionAutomatique = true,
        IReadOnlyDictionary<string, string?>? donnees = null)
        => new(
            CodesAlertes.U2QuantiteNonReceptionnee,
            TypeAlerte.AnomalieMetier,
            SeveriteAlerte.Avertissement,
            "Quantité non réceptionnée",
            "La quantité non réceptionnée issue du bilan est positive.",
            SourcesAlertes.BilanUsinage2,
            "2026-01",
            numeroOf,
            "ART-1",
            "M3000L0",
            resolutionAutomatique,
            donnees ?? new Dictionary<string, string?> { ["unite"] = "pieces" });

    private sealed class FakeDetectionService : IAlerteDetectionService
    {
        public IReadOnlyList<AlerteDetection> Detections { get; set; } = [];
        public Exception? Exception { get; init; }
        public int NombreAppels { get; private set; }

        public Task<IReadOnlyList<AlerteDetection>> DetecterAsync(
            string periode,
            CancellationToken cancellationToken = default)
        {
            NombreAppels++;
            return Exception is null
                ? Task.FromResult(Detections)
                : Task.FromException<IReadOnlyList<AlerteDetection>>(Exception);
        }
    }

    private sealed class FakeAlerteRepository : IAlerteRepository
    {
        public List<AlerteMetier> Alertes { get; } = [];
        public int NombreSauvegardes { get; private set; }

        public Task<AlerteMetier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Alertes.FirstOrDefault(alerte => alerte.Id == id));

        public Task<AlerteMetier?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<AlerteMetier>> GetActiveByPeriodAsync(
            string periode,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AlerteMetier>>(
                Alertes.Where(alerte => alerte.EstActive && alerte.Periode == periode).ToArray());

        public Task<AlerteSearchResult> SearchAsync(
            AlerteSearchFilter filter,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AddRangeAsync(IEnumerable<AlerteMetier> alertes, CancellationToken cancellationToken = default)
        {
            Alertes.AddRange(alertes);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NombreSauvegardes++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; }
    }
}
