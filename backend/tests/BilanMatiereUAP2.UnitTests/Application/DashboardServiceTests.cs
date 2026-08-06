using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Application.Features.Dashboard;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Contracts.Dashboard;
using FluentValidation;
using Xunit;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class DashboardServiceTests
{
    private static readonly DateTime CalculUtc = new(2026, 1, 31, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Get_TroisBilansDisponibles_ConserveValeursEtUnitesSansLesAdditionner()
    {
        var service = CreerService();

        var response = await service.GetAsync("2026-01");

        Assert.Equal(10m, response.Fonderie.Ecart);
        Assert.Equal("kg", response.Fonderie.Unite);
        Assert.Equal(20m, response.Usinage2.Ecart);
        Assert.Equal("pieces", response.Usinage2.Unite);
        Assert.Equal(30m, response.Uap2.Ecart);
        Assert.Equal("pieces", response.Uap2.Unite);
        Assert.Equal(DashboardStatuts.Ok, response.StatutGlobal);
        Assert.Equal(CalculUtc, response.CalculeLeUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("01-2026")]
    [InlineData("2026-13")]
    public async Task Get_PeriodeInvalide_RefuseAvantToutAccesAuxSources(string periode)
    {
        var fonderie = new FakeFonderieService(BilanFonderieComplet());
        var service = CreerService(fonderie: fonderie);

        await Assert.ThrowsAsync<ValidationException>(() => service.GetAsync(periode));

        Assert.Equal(0, fonderie.NombreAppels);
    }

    [Fact]
    public async Task Get_FonderiePartielle_PropageNullEtAjouteUnAvertissement()
    {
        var fonderie = BilanFonderieComplet() with
        {
            TotalEcartKg = null,
            NombreLignesDonneesManquantes = 1,
            NombreAnomalies = 1
        };
        var service = CreerService(fonderie: new FakeFonderieService(fonderie));

        var response = await service.GetAsync("2026-01");

        Assert.Null(response.Fonderie.Ecart);
        Assert.Equal(DashboardStatuts.Partiel, response.Fonderie.Statut);
        Assert.Equal(DashboardStatuts.Partiel, response.StatutGlobal);
        Assert.Contains(response.Avertissements, warning => warning.Code == "FONDERIE_STATUT_PARTIEL");
    }

    [Fact]
    public async Task Get_U2Partiel_NeTransformePasEcartAbsentEnZero()
    {
        var u2 = BilanUsinage2Complet() with
        {
            TotalBilanUsinage2Pieces = null,
            NombreLignesDonneesManquantes = 1
        };
        var service = CreerService(usinage2: new FakeUsinage2Service(u2));

        var response = await service.GetAsync("2026-01");

        Assert.Null(response.Usinage2.Ecart);
        Assert.Equal(DashboardStatuts.Partiel, response.Usinage2.Statut);
    }

    [Fact]
    public async Task Get_Uap2Partiel_ConserveEcartEnPiecesEtSignaleLesDonneesManquantes()
    {
        var uap2 = BilanUap2Complet() with { NombreLignesDonneesManquantes = 1 };
        var service = CreerService(uap2: new FakeUap2Service(uap2));

        var response = await service.GetAsync("2026-01");

        Assert.Equal(30m, response.Uap2.Ecart);
        Assert.Equal("pieces", response.Uap2.Unite);
        Assert.Equal(DashboardStatuts.Partiel, response.Uap2.Statut);
    }

    [Fact]
    public async Task Get_AucuneLigne_RetourneDesEcartsNonDisponibles()
    {
        var service = CreerService(
            fonderie: new FakeFonderieService(new BilanFonderieResponse("2026-01", [], null, null, null, 0, 0, 0)),
            usinage2: new FakeUsinage2Service(new BilanUsinage2Response("2026-01", [], 0, 0, 0)),
            uap2: new FakeUap2Service(new BilanUap2Response("2026-01", [], 0, 0, null, 1.5m, 0, 0, 0, 0)));

        var response = await service.GetAsync("2026-01");

        Assert.Null(response.Fonderie.Ecart);
        Assert.Null(response.Usinage2.Ecart);
        Assert.Null(response.Uap2.Ecart);
        Assert.Equal(DashboardStatuts.NonDisponible, response.StatutGlobal);
    }

    [Fact]
    public async Task Get_OperationsEtImports_MappeLesCompteursReels()
    {
        var repository = new FakeDashboardRepository
        {
            Operations = new DashboardOperationsData(3, 5, 7),
            Imports = new DashboardImportsData(11, 2, 0, 4, 0, 6, CalculUtc.AddHours(-1))
        };
        var u2 = BilanUsinage2Complet() with
        {
            NombreOfNonReceptionnes = 8,
            NombreReferencesManquantes = 2
        };
        var service = CreerService(repository, usinage2: new FakeUsinage2Service(u2));

        var response = await service.GetAsync("2026-01");

        Assert.Equal(3, response.Operations.InventairesBrouillon);
        Assert.Equal(5, response.Operations.InventairesValides);
        Assert.Equal(7, response.Operations.ReceptionsValidees);
        Assert.Equal(8, response.Operations.ReceptionsNonReceptionnees);
        Assert.Equal(2, response.Operations.OfReferencesManquantes);
        Assert.Null(response.Operations.ControlesPoidsAmbigus);
        Assert.Equal(11, response.Imports.Reussis);
        Assert.Equal(2, response.Imports.Partiels);
        Assert.Equal(4, response.Imports.FichiersIdentiques);
        Assert.Equal(CalculUtc.AddHours(-1), response.Imports.DernierImportUtc);
    }

    [Fact]
    public async Task Get_ImportEchoue_PrioriseAnomalieSurPartielEtAVerifier()
    {
        var repository = new FakeDashboardRepository
        {
            Imports = new DashboardImportsData(0, 0, 1, 0, 0, 1, CalculUtc)
        };
        var fonderie = BilanFonderieComplet() with
        {
            TotalEcartKg = null,
            NombreLignesDonneesManquantes = 1
        };
        var u2 = BilanUsinage2Complet() with { NombreLignesAVerifier = 1 };
        var service = CreerService(
            repository,
            new FakeFonderieService(fonderie),
            new FakeUsinage2Service(u2));

        var response = await service.GetAsync("2026-01");

        Assert.Equal(DashboardStatuts.Anomalie, response.StatutGlobal);
        Assert.Contains(response.Avertissements, warning => warning.Code == "IMPORTS_ECHOUES");
    }

    [Fact]
    public async Task Get_Partiel_PrioriseDonneeManquanteSurSimpleVerification()
    {
        var fonderie = BilanFonderieComplet() with
        {
            TotalEcartKg = null,
            NombreLignesDonneesManquantes = 1
        };
        var u2 = BilanUsinage2Complet() with { NombreLignesAVerifier = 1 };
        var response = await CreerService(
                fonderie: new FakeFonderieService(fonderie),
                usinage2: new FakeUsinage2Service(u2))
            .GetAsync("2026-01");

        Assert.Equal(DashboardStatuts.Partiel, response.StatutGlobal);
    }

    [Fact]
    public async Task Get_LignesAVerifier_ProduitLeStatutGlobalAVerifier()
    {
        var u2 = BilanUsinage2Complet() with
        {
            NombreLignesOk = 0,
            NombreLignesAVerifier = 1
        };

        var response = await CreerService(usinage2: new FakeUsinage2Service(u2))
            .GetAsync("2026-01");

        Assert.Equal(DashboardStatuts.AVerifier, response.Usinage2.Statut);
        Assert.Equal(1, response.Usinage2.NombreAVerifier);
        Assert.Equal(DashboardStatuts.AVerifier, response.StatutGlobal);
    }

    [Fact]
    public async Task Get_ErreurMetierDUnBilan_EstPropagee()
    {
        var expected = new ValidationException("Bilan Fonderie invalide");
        var service = CreerService(fonderie: new ThrowingFonderieService(expected));

        var actual = await Assert.ThrowsAsync<ValidationException>(() => service.GetAsync("2026-01"));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task Get_ErreurTechniqueRepository_EstPropagee()
    {
        var repository = new FakeDashboardRepository { Exception = new InvalidOperationException("SQL indisponible") };
        var service = CreerService(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync("2026-01"));

        Assert.Equal("SQL indisponible", exception.Message);
    }

    private static DashboardService CreerService(
        FakeDashboardRepository? repository = null,
        IBilanFonderieService? fonderie = null,
        FakeUsinage2Service? usinage2 = null,
        FakeUap2Service? uap2 = null)
        => new(
            fonderie ?? new FakeFonderieService(BilanFonderieComplet()),
            usinage2 ?? new FakeUsinage2Service(BilanUsinage2Complet()),
            uap2 ?? new FakeUap2Service(BilanUap2Complet()),
            repository ?? new FakeDashboardRepository(),
            new FakeDateTimeProvider());

    private static BilanFonderieResponse BilanFonderieComplet()
    {
        var ligne = new BilanFonderieLigneResponse(
            Guid.NewGuid(), "M3000L0", "2026-01",
            100, 20, 120, 90, 5, 15, 110, 10, 8.33m, [], "Complet");
        return new BilanFonderieResponse("2026-01", [ligne], 120, 110, 10, 1, 0, 0);
    }

    private static BilanUsinage2Response BilanUsinage2Complet()
        => new(
            "2026-01",
            [],
            100,
            1,
            0,
            TotalBilanUsinage2Pieces: 20,
            Total: 1,
            NombreLignesOk: 1,
            NombreOf: 1);

    private static BilanUap2Response BilanUap2Complet()
    {
        var ligne = new BilanUap2LigneResponse(
            "OF-1", "ART-1", "Article", "M3000L0", "2026-01",
            100, 5, 5, 5, 85, 1, 85, 40, 15, 30, 30, [], "OK");
        return new BilanUap2Response("2026-01", [ligne], 100, 30, 85, 1.5m, 1, 0, 0, 0, 1, 1, 50);
    }

    private sealed class FakeFonderieService(BilanFonderieResponse response) : IBilanFonderieService
    {
        public int NombreAppels { get; private set; }

        public Task<BilanFonderieResponse> GetAsync(string periode, CancellationToken cancellationToken = default)
        {
            NombreAppels++;
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingFonderieService(Exception exception) : IBilanFonderieService
    {
        public Task<BilanFonderieResponse> GetAsync(
            string periode,
            CancellationToken cancellationToken = default)
            => Task.FromException<BilanFonderieResponse>(exception);
    }

    private sealed class FakeUsinage2Service(BilanUsinage2Response response) : IBilanUsinage2Service
    {
        public Task<BilanUsinage2Response> GetAsync(string periode, CancellationToken cancellationToken = default)
            => Task.FromResult(response);

        public Task<BilanUsinage2Response> QueryAsync(
            GetBilanUsinage2Query query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(response);

        public Task<byte[]> ExportCsvAsync(string periode, CancellationToken cancellationToken = default)
            => Task.FromResult(Array.Empty<byte>());
    }

    private sealed class FakeUap2Service(BilanUap2Response response) : IBilanUap2Service
    {
        public Task<BilanUap2Response> GetAsync(string periode, CancellationToken cancellationToken = default)
            => Task.FromResult(response);
    }

    private sealed class FakeDashboardRepository : IDashboardReadRepository
    {
        public DashboardOperationsData Operations { get; init; } = new(0, 0, 0);
        public DashboardImportsData Imports { get; init; } = new(0, 0, 0, 0, 0, 0, null);
        public Exception? Exception { get; init; }

        public Task<DashboardOperationsData> GetOperationsAsync(
            DateOnly periode,
            CancellationToken cancellationToken = default)
        {
            if (Exception is not null) throw Exception;
            return Task.FromResult(Operations);
        }

        public Task<DashboardImportsData> GetImportsAsync(
            DateOnly periode,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Imports);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => CalculUtc;
    }
}
