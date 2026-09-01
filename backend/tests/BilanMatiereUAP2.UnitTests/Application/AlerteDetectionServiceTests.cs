using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Alertes;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Application;

/// <summary>
/// Couvre la traduction « anomalie déjà produite par un bilan » -> « détection d'alerte ».
/// Le service de détection ne recalcule aucune formule métier : ces tests vérifient
/// uniquement le mappage, les unités et la conservation du signe.
/// </summary>
public sealed class AlerteDetectionServiceTests
{
    private const string Periode = "2026-07";

    // --- Usinage 2 : non-réception et sur-réception -------------------------

    [Fact]
    public async Task Detecter_QuantiteNonReceptionneePositive_ProduitUneAlerteEnPieces()
    {
        // EMR net = 1000 - 20 = 980 ; réceptions validées = 900 ; reste 80 pièces.
        var ligne = LigneUsinage2(quantiteCtrlEmr: 1000m, rebutEmr: 20m, receptionnee: 900m);

        var detections = await Detecter(usinage2: BilanUsinage2([ligne]));

        var detection = Assert.Single(
            detections,
            d => d.Code == CodesAlertes.U2QuantiteNonReceptionnee);
        Assert.Equal(TypeAlerte.AnomalieMetier, detection.Type);
        Assert.Equal(SourcesAlertes.BilanUsinage2, detection.Source);
        Assert.Equal("OF26070378", detection.NumeroOF);
        Assert.Equal("pieces", detection.DonneesAssociees["unite"]);
        Assert.Equal("80", detection.DonneesAssociees["quantite"]);
        Assert.DoesNotContain(detections, d => d.Code == CodesAlertes.U2ReceptionSuperieureEmr);
    }

    [Fact]
    public async Task Detecter_SurReception_ConserveLeSigneNegatifSansValeurAbsolue()
    {
        // EMR net = 980 ; réceptions validées = 1000 ; excédent de 20 pièces.
        var ligne = LigneUsinage2(quantiteCtrlEmr: 1000m, rebutEmr: 20m, receptionnee: 1000m);

        var detections = await Detecter(usinage2: BilanUsinage2([ligne]));

        var detection = Assert.Single(
            detections,
            d => d.Code == CodesAlertes.U2ReceptionSuperieureEmr);
        Assert.Equal("pieces", detection.DonneesAssociees["unite"]);
        // Le signe est une information métier : jamais de Math.Abs, jamais de clamp à 0.
        Assert.Equal("-20", detection.DonneesAssociees["ecartSigne"]);
        Assert.DoesNotContain(detections, d => d.Code == CodesAlertes.U2QuantiteNonReceptionnee);
    }

    [Fact]
    public async Task Detecter_ReceptionEquilibree_NeProduitAucuneAlerteDeReception()
    {
        // EMR net = 980 ; réceptions validées = 980 : situation OK, aucune alerte « OK » inutile.
        var ligne = LigneUsinage2(quantiteCtrlEmr: 1000m, rebutEmr: 20m, receptionnee: 980m);

        var detections = await Detecter(usinage2: BilanUsinage2([ligne]));

        Assert.DoesNotContain(detections, d => d.Code == CodesAlertes.U2QuantiteNonReceptionnee);
        Assert.DoesNotContain(detections, d => d.Code == CodesAlertes.U2ReceptionSuperieureEmr);
    }

    // --- UAP2 : données de référence manquantes -----------------------------

    [Fact]
    public async Task Detecter_QuantiteCtrlAbsente_ProduitUap2CtrlManquantSansValeurNulleRemplacee()
    {
        var ligne = LigneUap2([BilanUap2Service.AnomalieCtrlAbsent]);

        var detections = await Detecter(uap2: BilanUap2([ligne]));

        var detection = Assert.Single(detections, d => d.Code == CodesAlertes.Uap2CtrlManquant);
        Assert.Equal(TypeAlerte.DonneeManquante, detection.Type);
        Assert.Equal(SourcesAlertes.BilanUap2, detection.Source);
        Assert.Equal("OF06020384", detection.NumeroOF);
        Assert.Equal("14B1203-1", detection.CodeArticle);
        Assert.Equal("pieces", detection.DonneesAssociees["unite"]);
        // Une donnée absente reste absente : aucune valeur numérique de substitution.
        Assert.DoesNotContain("0", detection.DonneesAssociees.Values);
    }

    [Fact]
    public async Task Detecter_PoidsPhysiqueNonResolu_ProduitUap2PoidsPhysiqueManquant()
    {
        var ligne = LigneUap2([BilanUap2Service.AnomaliePoidsPhysique]);

        var detections = await Detecter(uap2: BilanUap2([ligne]));

        var detection = Assert.Single(detections, d => d.Code == CodesAlertes.Uap2PoidsPhysiqueManquant);
        Assert.Equal(TypeAlerte.DonneeManquante, detection.Type);
        // La sortie est en kg, l'écart reste en pièces : les deux unités ne se mélangent pas.
        Assert.Equal("kg", detection.DonneesAssociees["uniteSortie"]);
        Assert.Equal("pieces", detection.DonneesAssociees["uniteEcart"]);
    }

    // --- Fonderie : inventaires indisponibles -------------------------------

    [Fact]
    public async Task Detecter_StockDebutIndisponible_ProduitFonInventaireDebutManquantEnKg()
    {
        var ligne = LigneFonderie([BilanFonderieService.AnomalieStockDebut]);

        var detections = await Detecter(fonderie: BilanFonderie([ligne]));

        var detection = Assert.Single(detections, d => d.Code == CodesAlertes.FonInventaireDebutManquant);
        Assert.Equal(TypeAlerte.DonneeManquante, detection.Type);
        Assert.Equal(SourcesAlertes.BilanFonderie, detection.Source);
        Assert.Equal("M3000L0", detection.TypeMatiere);
        Assert.Equal("kg", detection.DonneesAssociees["unite"]);
    }

    [Fact]
    public async Task Detecter_StockFinIndisponible_ProduitFonInventaireFinManquantEnKg()
    {
        var ligne = LigneFonderie([BilanFonderieService.AnomalieStockFin]);

        var detections = await Detecter(fonderie: BilanFonderie([ligne]));

        var detection = Assert.Single(detections, d => d.Code == CodesAlertes.FonInventaireFinManquant);
        Assert.Equal("kg", detection.DonneesAssociees["unite"]);
    }

    [Fact]
    public async Task Detecter_BilanFonderieComplet_NeProduitAucuneAlerte()
    {
        var detections = await Detecter(fonderie: BilanFonderie([LigneFonderie([])]));

        Assert.Empty(detections);
    }

    // --- Imports X3 ---------------------------------------------------------

    [Fact]
    public async Task Detecter_ImportsReussisEtFichiersIdentiques_NeProduitAucuneAlerte()
    {
        // Un fichier déjà connu (duplicate) est une information, pas une anomalie.
        var imports = Imports(reussis: 5, fichiersIdentiques: 3);

        var detections = await Detecter(imports: imports);

        Assert.Empty(detections);
    }

    [Fact]
    public async Task Detecter_ImportEchoue_ProduitUneAnomalieTechniqueEnErreur()
    {
        var detections = await Detecter(imports: Imports(echoues: 2));

        var detection = Assert.Single(detections, d => d.Code == CodesAlertes.X3ImportEchec);
        Assert.Equal(TypeAlerte.AnomalieTechnique, detection.Type);
        Assert.Equal(SeveriteAlerte.Erreur, detection.Severite);
        Assert.Equal(SourcesAlertes.ImportX3, detection.Source);
        Assert.Equal("2", detection.DonneesAssociees["nombreImports"]);
    }

    [Fact]
    public async Task Detecter_ImportPartiel_ProduitUneAnomalieTechniqueDistincte()
    {
        var detections = await Detecter(imports: Imports(partiels: 1));

        var detection = Assert.Single(detections, d => d.Code == CodesAlertes.X3ImportPartiel);
        Assert.Equal(TypeAlerte.AnomalieTechnique, detection.Type);
        Assert.DoesNotContain(detections, d => d.Code == CodesAlertes.X3ImportEchec);
    }

    // --- Helpers ------------------------------------------------------------

    private static Task<IReadOnlyList<AlerteDetection>> Detecter(
        BilanFonderieResponse? fonderie = null,
        BilanUsinage2Response? usinage2 = null,
        BilanUap2Response? uap2 = null,
        DashboardImportsData? imports = null)
    {
        var service = new AlerteDetectionService(
            new FakeBilanFonderieService(fonderie ?? BilanFonderie([])),
            new FakeBilanUsinage2Service(usinage2 ?? BilanUsinage2([])),
            new FakeBilanUap2Service(uap2 ?? BilanUap2([])),
            new FakeDashboardReadRepository(imports ?? Imports()));
        return service.DetecterAsync(Periode);
    }

    private static DashboardImportsData Imports(
        int reussis = 0,
        int partiels = 0,
        int echoues = 0,
        int fichiersIdentiques = 0)
        => new(reussis, partiels, echoues, fichiersIdentiques, 0, 0, null);

    private static BilanFonderieResponse BilanFonderie(IReadOnlyList<BilanFonderieLigneResponse> lignes)
        => new(Periode, lignes, null, null, null, lignes.Count, 0, 0);

    private static BilanFonderieLigneResponse LigneFonderie(IReadOnlyList<string> anomalies)
        => new(
            Guid.NewGuid(),
            "M3000L0",
            Periode,
            null,
            null,
            null,
            null,
            0m,
            null,
            null,
            null,
            null,
            anomalies,
            anomalies.Count == 0 ? "Complet" : "Incomplet");

    private static BilanUsinage2Response BilanUsinage2(IReadOnlyList<BilanUsinage2LigneResponse> lignes)
        => new(Periode, lignes, 0m, lignes.Count, 0);

    private static BilanUsinage2LigneResponse LigneUsinage2(
        decimal quantiteCtrlEmr,
        decimal rebutEmr,
        decimal receptionnee)
    {
        var nombreDePieces = quantiteCtrlEmr - rebutEmr;
        return new BilanUsinage2LigneResponse(
            "OF26070378",
            "14B1203-1",
            null,
            "16",
            null,
            Periode,
            quantiteCtrlEmr,
            rebutEmr,
            nombreDePieces,
            receptionnee,
            nombreDePieces - receptionnee,
            0m,
            null,
            null,
            null,
            null,
            null,
            // Poids renseignés : ces tests ciblent la réception, pas le contrôle des poids.
            1m,
            1m,
            0m,
            0m,
            "Complet",
            "Complet",
            []);
    }

    private static BilanUap2Response BilanUap2(IReadOnlyList<BilanUap2LigneResponse> lignes)
        => new(Periode, lignes, 0m, 0m, null, 5m, lignes.Count, 0, 0, 0);

    private static BilanUap2LigneResponse LigneUap2(IReadOnlyList<string> anomalies)
        => new(
            "OF06020384",
            "14B1203-1",
            null,
            null,
            Periode,
            0m,
            0m,
            0m,
            0m,
            0m,
            null,
            null,
            0m,
            0m,
            0m,
            null,
            anomalies,
            "DonneesManquantes");

    private sealed class FakeBilanFonderieService(BilanFonderieResponse reponse) : IBilanFonderieService
    {
        public Task<BilanFonderieResponse> GetAsync(string periode, CancellationToken cancellationToken = default)
            => Task.FromResult(reponse);
    }

    private sealed class FakeBilanUsinage2Service(BilanUsinage2Response reponse) : IBilanUsinage2Service
    {
        public Task<BilanUsinage2Response> GetAsync(string periode, CancellationToken cancellationToken = default)
            => Task.FromResult(reponse);

        public Task<BilanUsinage2Response> QueryAsync(
            GetBilanUsinage2Query query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(reponse);

        public Task<byte[]> ExportCsvAsync(string periode, CancellationToken cancellationToken = default)
            => Task.FromResult(Array.Empty<byte>());
    }

    private sealed class FakeBilanUap2Service(BilanUap2Response reponse) : IBilanUap2Service
    {
        public Task<IReadOnlyList<string>> GetPeriodesDisponiblesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([Periode]);

        public Task<BilanUap2Response> GetAsync(string periode, CancellationToken cancellationToken = default)
            => Task.FromResult(reponse);
    }

    private sealed class FakeDashboardReadRepository(DashboardImportsData imports) : IDashboardReadRepository
    {
        public Task<DashboardOperationsData> GetOperationsAsync(
            DateOnly periode,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new DashboardOperationsData(0, 0, 0));

        public Task<DashboardImportsData> GetImportsAsync(
            DateOnly periode,
            CancellationToken cancellationToken = default)
            => Task.FromResult(imports);
    }
}
