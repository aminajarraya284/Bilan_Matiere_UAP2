using System.Text;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Application.Features.Rapports;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Contracts.Rapports;
using FluentValidation;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class RapportsServiceTests
{
    [Fact]
    public void Catalogue_Admin_ExposeLesHuitRapportsSupportes()
    {
        var service = CreerService("Admin");

        var catalogue = service.GetCatalogue();

        Assert.Equal(8, catalogue.Count);
        Assert.All(catalogue, item => Assert.Equal(new[] { "CSV" }, item.Formats));
    }

    [Fact]
    public void Catalogue_ResponsableUsinage2_RespecteLePerimetreMetier()
    {
        var service = CreerService("ResponsableUsinage2");

        var codes = service.GetCatalogue().Select(item => item.Code).ToArray();

        Assert.Contains(CodesRapports.BilanUsinage2, codes);
        Assert.Contains(CodesRapports.BilanUap2, codes);
        Assert.DoesNotContain(CodesRapports.BilanFonderie, codes);
        Assert.DoesNotContain(CodesRapports.InventaireFonderie, codes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("2026-13")]
    public async Task BilanFonderie_PeriodeInvalide_EchoueAvantLaLecture(string periode)
    {
        var fake = new FakeBilanFonderieService(CreerBilan());
        var service = CreerService("Admin", fake);

        await Assert.ThrowsAsync<ValidationException>(() => service.GetBilanFonderieAsync(periode));
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task ExportCsv_ReutiliseLaProjection_EmetLeBomEtNeutraliseUneFormule()
    {
        var fake = new FakeBilanFonderieService(CreerBilan("=FORMULE"));
        var service = CreerService("Admin", fake);

        var export = await service.ExportCsvAsync(
            CodesRapports.BilanFonderie,
            "2026-01",
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        var csv = Encoding.UTF8.GetString(export.Content);

        Assert.Equal(1, fake.Calls);
        Assert.True(export.Content.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        Assert.Contains("'=FORMULE", csv);
        Assert.Contains("Stock debut (kg)", csv);
        Assert.Equal("text/csv; charset=utf-8", export.ContentType);
    }

    private static RapportsService CreerService(string role, IBilanFonderieService? fonderie = null)
        => new(
            fonderie ?? null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new FakeCurrentUser(role),
            new FakeDateTimeProvider());

    private static BilanFonderieResponse CreerBilan(string anomalie = "DONNEE_MANQUANTE")
        => new(
            "2026-01",
            [new BilanFonderieLigneResponse(
                Guid.NewGuid(),
                "M3000",
                "2026-01",
                null,
                12m,
                null,
                5m,
                1m,
                2m,
                8m,
                null,
                null,
                [anomalie],
                "Incomplet")],
            null,
            8m,
            null,
            1,
            1,
            1);

    private sealed class FakeBilanFonderieService(BilanFonderieResponse response) : IBilanFonderieService
    {
        public int Calls { get; private set; }

        public Task<BilanFonderieResponse> GetAsync(string periode, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(response);
        }
    }

    private sealed class FakeCurrentUser(string role) : ICurrentUserService
    {
        public Guid? UserId => Guid.NewGuid();
        public string? UserName => "Test";
        public string? Role => role;
        public bool IsAuthenticated => true;
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc);
    }
}
