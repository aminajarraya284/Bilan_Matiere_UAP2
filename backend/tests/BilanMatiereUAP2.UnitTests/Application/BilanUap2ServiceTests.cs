using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using Xunit;

namespace BilanMatiereUAP2.UnitTests.Application;

/// <summary>
/// Couvre RM-UAP2-001 et RM-UAP2-002 tels que confirmes par la note SOPAL :
/// CTRL est la quantite de reference (code poste exact), pas un quatrieme processus.
/// </summary>
public sealed class BilanUap2ServiceTests
{
    private const string Of = "OF18100563";
    private const string Article = "ART-1";

    [Fact]
    public async Task Get_Partie1_RetrancheLesRebutsProcessusDeLaQuantiteCtrl()
    {
        // CTRL = 1000 ; rebuts SCI 10 + EMR 20 + TRN 5 = 35 -> sortie MP = 965 pieces.
        var service = CreateService(new FakeSources
        {
            Rapports =
            {
                Rapport("CTRL", quantiteTotale: 1000m, quantiteRebut: 0m),
                Rapport("SCI01", quantiteTotale: 900m, quantiteRebut: 10m),
                Rapport("EMR02", quantiteTotale: 800m, quantiteRebut: 20m),
                Rapport("TRN03", quantiteTotale: 700m, quantiteRebut: 5m)
            },
            Receptions = { Reception(poidsPhysiqueUnitaireKg: 0.25m) }
        });

        var ligne = Assert.Single((await service.GetAsync("2026-07")).Lignes);

        Assert.Equal(1000m, ligne.QuantiteCtrlPieces);
        Assert.Equal(10m, ligne.RebutSciPieces);
        Assert.Equal(20m, ligne.RebutEmrPieces);
        Assert.Equal(5m, ligne.RebutTrnPieces);
        Assert.Equal(965m, ligne.QuantiteSortieMpPieces);
        Assert.Equal(241.25m, ligne.SortieMpKg);
    }

    // Le point qui distingue les deux lectures possibles de la note : les quantites
    // totales de SCI/EMR/TRN ne s'ajoutent PAS a la reference CTRL.
    [Fact]
    public async Task Get_NAjoutePasLesQuantitesDesProcessusALaReferenceCtrl()
    {
        var service = CreateService(new FakeSources
        {
            Rapports =
            {
                Rapport("CTRL", quantiteTotale: 100m, quantiteRebut: 0m),
                Rapport("SCI01", quantiteTotale: 999m, quantiteRebut: 0m)
            },
            Receptions = { Reception(0.5m) }
        });

        var ligne = Assert.Single((await service.GetAsync("2026-07")).Lignes);

        Assert.Equal(100m, ligne.QuantiteCtrlPieces);
        Assert.Equal(100m, ligne.QuantiteSortieMpPieces);
    }

    [Fact]
    public async Task Get_Partie2_CalculeLEcartEnPiecesEtSonTaux()
    {
        // Ecart = 1000 - (35 + 900 + 50) = 15 ; taux = 1,5 %.
        var service = CreateService(new FakeSources
        {
            Rapports =
            {
                Rapport("CTRL", 1000m, 0m),
                Rapport("SCI01", 0m, 10m),
                Rapport("EMR02", 0m, 20m),
                Rapport("TRN03", 0m, 5m)
            },
            Entrees = { Entree(900m) },
            Rebuts = { Rebut(50m) },
            Receptions = { Reception(0.25m) }
        });

        var ligne = Assert.Single((await service.GetAsync("2026-07")).Lignes);

        Assert.Equal(900m, ligne.EntreeOfPieces);
        Assert.Equal(50m, ligne.RebutUsinage2Pieces);
        Assert.Equal(15m, ligne.EcartUap2Pieces);
        Assert.Equal(1.5m, ligne.TauxEcartPourcent);
    }

    [Theory]
    [InlineData(15, "OK")]       // taux 1,5 % : a la limite, donc acceptable
    [InlineData(20, "AVerifier")] // taux 2,0 % : au-dela du seuil
    public async Task Get_AppliqueLeSeuilParametreALaValeurAbsolueDuTaux(int ecart, string statutAttendu)
    {
        var service = CreateService(new FakeSources
        {
            Rapports = { Rapport("CTRL", 1000m, 0m) },
            Entrees = { Entree(1000m - ecart) },
            Receptions = { Reception(0.25m) }
        });

        var ligne = Assert.Single((await service.GetAsync("2026-07")).Lignes);

        Assert.Equal(statutAttendu, ligne.Statut);
    }

    // Un ecart negatif depasse le seuil au meme titre qu'un ecart positif.
    [Fact]
    public async Task Get_AvecEcartNegatif_DeclencheAussiLAlerteEtConserveLeSigne()
    {
        var service = CreateService(new FakeSources
        {
            Rapports = { Rapport("CTRL", 1000m, 0m) },
            Entrees = { Entree(1100m) },
            Receptions = { Reception(0.25m) }
        });

        var ligne = Assert.Single((await service.GetAsync("2026-07")).Lignes);

        Assert.Equal(-100m, ligne.EcartUap2Pieces);
        Assert.Equal(-10m, ligne.TauxEcartPourcent);
        Assert.Contains(BilanUap2Service.AnomalieHorsSeuil, ligne.Anomalies);
    }

    // RM-COM-005 : sans poids unitaire identifie, la conversion est refusee.
    [Fact]
    public async Task Get_SansPoidsPhysique_RefuseLaConversionEtSignaleLaDonneeManquante()
    {
        var service = CreateService(new FakeSources
        {
            Rapports = { Rapport("CTRL", 1000m, 0m) },
            Receptions = { Reception(poidsPhysiqueUnitaireKg: null) }
        });

        var response = await service.GetAsync("2026-07");
        var ligne = Assert.Single(response.Lignes);

        Assert.Null(ligne.SortieMpKg);
        Assert.Equal(1000m, ligne.QuantiteSortieMpPieces);
        Assert.Contains(BilanUap2Service.AnomaliePoidsPhysique, ligne.Anomalies);
        Assert.Equal("DonneesManquantes", ligne.Statut);
        Assert.Null(response.TotalSortieMpKg);
    }

    [Fact]
    public async Task Get_SansLigneCtrl_SignaleLaReferenceAbsente()
    {
        var service = CreateService(new FakeSources
        {
            Rapports = { Rapport("SCI01", 900m, 10m) },
            Receptions = { Reception(0.25m) }
        });

        var ligne = Assert.Single((await service.GetAsync("2026-07")).Lignes);

        Assert.Equal(0m, ligne.QuantiteCtrlPieces);
        Assert.Null(ligne.TauxEcartPourcent);
        Assert.Contains(BilanUap2Service.AnomalieCtrlAbsent, ligne.Anomalies);
    }

    [Fact]
    public async Task Get_SansParametreDeSeuil_UtiliseLeRepliEtLeSignale()
    {
        var service = CreateService(
            new FakeSources
            {
                Rapports = { Rapport("CTRL", 1000m, 0m) },
                Receptions = { Reception(0.25m) }
            },
            seuilParametre: null);

        var response = await service.GetAsync("2026-07");

        Assert.Equal(BilanUap2Service.SeuilEcartParDefautPourcent, response.SeuilEcartPourcent);
        Assert.Contains(BilanUap2Service.AnomalieParametreSeuil, response.Lignes[0].Anomalies);
    }

    [Fact]
    public async Task Get_AvecPeriodeInvalide_LeveUneValidationException()
    {
        var service = CreateService(new FakeSources());

        await Assert.ThrowsAsync<ValidationException>(() => service.GetAsync("07-2026"));
    }

    // --- fabriques -----------------------------------------------------------

    private static BilanUap2Service CreateService(FakeSources sources, decimal? seuilParametre = 1.5m)
        => new(new FakeRepository(sources), new FakeParametres(seuilParametre));

    private static readonly DateTime Now = new(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);

    private static X3RapportMachine Rapport(string codePoste, decimal quantiteTotale, decimal quantiteRebut)
        => X3RapportMachine.Creer(
            new DateOnly(2026, 7, 15),
            codePoste,
            // La classification vient du prefixe du code poste, pas de ce champ.
            OrigineProcessus.Inconnue,
            "10",
            Article,
            "Piece test",
            Of,
            null,
            quantiteTotale,
            quantiteRebut,
            null,
            "YRJMCH",
            Guid.NewGuid(),
            Now);

    private static EntreeOfComposant Entree(decimal quantiteNetteSignee)
        => EntreeOfComposant.Creer(
            Guid.NewGuid(), new DateOnly(2026, 7, 20), Of, Article, "Piece test", "UN",
            quantiteNetteSignee, Now);

    private static RebutUsinage2 Rebut(decimal quantitePieces)
    {
        var rebut = RebutUsinage2.Creer(
            new DateOnly(2026, 7, 25), Of, Article, quantitePieces, null, null, Guid.NewGuid(), "T", Now);
        rebut.Valider(Guid.NewGuid(), "V", Now);
        return rebut;
    }

    private static ReceptionUsinage2 Reception(decimal? poidsPhysiqueUnitaireKg)
    {
        var reception = ReceptionUsinage2.Creer(
            Of,
            new DateOnly(2026, 7, 5),
            100m,
            260m,
            null,
            null,
            Snapshot(poidsPhysiqueUnitaireKg),
            Guid.NewGuid(),
            "T",
            Now);
        reception.Valider(false, null, Guid.NewGuid(), "V", Now);
        return reception;
    }

    private static ReceptionUsinage2.ContexteSnapshot Snapshot(decimal? poidsPhysiqueUnitaireKg)
        => new(
            Article,
            "Piece test",
            "En cours",
            "16",
            "M3000L0",
            2.5m,
            "KG",
            2.5m,
            poidsPhysiqueUnitaireKg,
            poidsPhysiqueUnitaireKg is null ? null : Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            null,
            "OK",
            "OK",
            "OK",
            "OK",
            poidsPhysiqueUnitaireKg is null ? "DonneesManquantes" : "OK",
            "OK",
            []);

    private sealed class FakeSources
    {
        public List<X3RapportMachine> Rapports { get; } = [];
        public List<EntreeOfComposant> Entrees { get; } = [];
        public List<RebutUsinage2> Rebuts { get; } = [];
        public List<ReceptionUsinage2> Receptions { get; } = [];
    }

    private sealed class FakeRepository(FakeSources sources) : IBilanUap2Repository
    {
        public Task<IReadOnlyList<string>> GetPeriodesDisponiblesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<X3RapportMachine>> GetRapportsMachineAsync(int annee, int mois, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<X3RapportMachine>>(sources.Rapports);

        public Task<IReadOnlyList<EntreeOfComposant>> GetEntreesOfAsync(int annee, int mois, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EntreeOfComposant>>(sources.Entrees);

        public Task<IReadOnlyList<RebutUsinage2>> GetRebutsValidesAsync(int annee, int mois, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RebutUsinage2>>(sources.Rebuts);

        public Task<IReadOnlyList<ReceptionUsinage2>> GetReceptionsValidesAsync(int annee, int mois, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ReceptionUsinage2>>(sources.Receptions);
    }

    private sealed class FakeParametres(decimal? seuil) : IParametreApplicationProvider
    {
        public Task<string?> GetValueAsync(string cle, CancellationToken ct = default)
            => Task.FromResult<string?>(null);

        public Task<decimal?> GetDecimalAsync(string cle, CancellationToken ct = default)
            => Task.FromResult(cle == ParametresApplicationCles.SeuilEcartUap2 ? seuil : null);

        public Task<IReadOnlyList<string>> GetCsvValuesAsync(string cle, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>([]);
    }
}
