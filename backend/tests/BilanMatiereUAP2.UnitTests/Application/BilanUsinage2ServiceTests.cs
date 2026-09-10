using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Application.Features.EntreeOf;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Application;

/// <summary>
/// Bilan Usinage 2 : toutes les quantites sont en PIECES.
/// Le controle des poids (kg, kg/piece) est verifie separement et ne doit jamais
/// intervenir dans le calcul en pieces.
/// </summary>
public sealed class BilanUsinage2ServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc);
    private const string Periode = "2026-07";

    [Fact]
    public async Task GetAsync_RetourneToutesLesLignes_QueryAsyncRestePagine()
    {
        var receptions = Enumerable.Range(1, 201)
            .Select(index => ReceptionValidee(10m, numeroOF: $"OF{index:D4}"))
            .ToArray();
        var service = CreateService(receptions, entreeOf: 10m, rapport: Rapport(10m, 0m));

        var complet = await service.GetAsync(Periode);
        var page = await service.QueryAsync(new GetBilanUsinage2Query(Periode, Page: 5));

        Assert.Equal(201, complet.Lignes.Count);
        Assert.Equal(201, complet.Total);
        Assert.Equal(2010m, complet.QuantiteTotaleReceptionneePieces);
        Assert.Equal("OF0201", Assert.Single(page.Lignes).NumeroOF);
        Assert.Equal(complet.Total, page.Total);
        Assert.Equal(complet.TotalBilanUsinage2Pieces, page.TotalBilanUsinage2Pieces);
        Assert.Equal(50, page.PageSize);
    }

    // Valeur du parametre applicatif CodeAtelierUap2 utilisee par les fakes.
    // Elle n'est pas codee en dur dans le service, qui la lit via IParametreApplicationProvider.
    private const string CodeAtelierUap2 = "16";

    // 1. CTRL EMR = 100, Rebut EMR = 5 -> Nombre de pieces = 95.
    [Fact]
    public async Task NombreDePieces_EstCtrlEmrMoinsRebutEmr()
    {
        var service = CreateService([ReceptionValidee(90m)], entreeOf: 100m, rapport: Rapport(100m, 5m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(100m, ligne.QuantiteCtrlEmrPieces);
        Assert.Equal(5m, ligne.RebutEmrPieces);
        Assert.Equal(95m, ligne.NombreDePieces);
    }

    // 2, 3, 4. Quantite non receptionnee et statut associe, sans Math.Abs ni remise a zero.
    [Theory]
    [InlineData(90, 5, "AVerifier")]
    [InlineData(95, 0, "OK")]
    [InlineData(100, -5, "Anomalie")]
    public async Task QuantiteNonReceptionnee_ConserveLeSigneEtPiloteLeStatut(
        decimal receptionnee,
        decimal nonReceptionneeAttendue,
        string statutAttendu)
    {
        var service = CreateService([ReceptionValidee(receptionnee)], entreeOf: 100m, rapport: Rapport(100m, 5m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(receptionnee, ligne.QuantiteReceptionneePieces);
        Assert.Equal(nonReceptionneeAttendue, ligne.QuantiteNonReceptionneePieces);
        Assert.Equal(statutAttendu, ligne.StatutReception);
    }

    // 5, 6, 7. Seules les receptions VALIDES sont additionnees.
    [Fact]
    public async Task QuantiteReceptionnee_ExclutBrouillonsEtAnnulees()
    {
        var valide30 = ReceptionValidee(30m);
        var valide40 = ReceptionValidee(40m);
        var annulee20 = ReceptionValidee(20m);
        annulee20.Annuler(Guid.NewGuid(), "A", Now);
        var brouillon25 = Reception(25m);

        var service = CreateService(
            [valide30, valide40, annulee20, brouillon25],
            entreeOf: 100m,
            rapport: Rapport(100m, 5m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(70m, ligne.QuantiteReceptionneePieces);
        Assert.Equal(25m, ligne.QuantiteNonReceptionneePieces);
    }

    // 8. Exemple obligatoire : 1000 - 20 - 970 = 10 pieces.
    [Fact]
    public async Task BilanUsinage2_EstReceptionneMoinsRebutU2MoinsEntreeOf()
    {
        var service = CreateService(
            [ReceptionValidee(1000m)],
            entreeOf: 970m,
            rapport: Rapport(1000m, 0m),
            rebuts: [RebutValide(20m)]);

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(1000m, ligne.QuantiteReceptionneePieces);
        Assert.Equal(20m, ligne.RebutUsinage2Pieces);
        Assert.Equal(970m, ligne.EntreeOfPieces);
        Assert.Equal(10m, ligne.BilanUsinage2Pieces);
    }

    // 9. Quantites MVT signees : -980 + 980 = 0, jamais de valeur absolue.
    [Fact]
    public async Task EntreeOf_ConserveLaQuantiteSignee()
    {
        var service = CreateService([ReceptionValidee(100m)], entreeOf: 0m, rapport: Rapport(100m, 0m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(0m, ligne.EntreeOfPieces);
        Assert.Equal(100m, ligne.BilanUsinage2Pieces); // 100 - 0 - 0
    }

    // 10. Les receptions hors periode sont exclues.
    [Fact]
    public async Task Receptions_HorsPeriode_SontExclues()
    {
        var dansPeriode = ReceptionValidee(40m);
        var horsPeriode = ReceptionValidee(60m, date: new DateOnly(2026, 6, 15));

        var service = CreateService([dansPeriode, horsPeriode], entreeOf: 0m, rapport: Rapport(100m, 0m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(40m, ligne.QuantiteReceptionneePieces);
    }

    // 10 bis. Les rebuts U2 hors periode sont exclus.
    [Fact]
    public async Task RebutsU2_HorsPeriode_SontExclus()
    {
        var service = CreateService(
            [ReceptionValidee(100m)],
            entreeOf: 0m,
            rapport: Rapport(100m, 0m),
            rebuts: [RebutValide(20m), RebutValide(50m, date: new DateOnly(2026, 6, 10))]);

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(20m, ligne.RebutUsinage2Pieces);
    }

    // 10 ter. Un rebut U2 non valide (brouillon) n'alimente pas le bilan.
    [Fact]
    public async Task RebutsU2_NonValides_SontExclus()
    {
        var service = CreateService(
            [ReceptionValidee(100m)],
            entreeOf: 0m,
            rapport: Rapport(100m, 0m),
            rebuts: [RebutBrouillon(20m)]);

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(0m, ligne.RebutUsinage2Pieces);
        Assert.Equal(100m, ligne.BilanUsinage2Pieces);
    }

    // 11, 12. La cle de regroupement est le NUMERO D'OF SEUL.
    // Un OF portant plusieurs articles reste UNE ligne, avec l'anomalie correspondante :
    // le bilan ne scinde pas un OF en plusieurs lignes et ne fait disparaitre aucune quantite.
    [Fact]
    public async Task Regroupement_UneLigneParOf_MemeAvecPlusieursArticles()
    {
        var service = CreateService(
            [
                ReceptionValidee(10m, numeroOF: "OF1", codeArticle: "ART1"),
                ReceptionValidee(20m, numeroOF: "OF1", codeArticle: "ART2"),
                ReceptionValidee(30m, numeroOF: "OF2", codeArticle: "ART1")
            ],
            entreeOf: 0m,
            rapport: Rapport(0m, 0m, trouve: false));

        var lignes = (await service.GetAsync(Periode, CancellationToken.None)).Lignes;

        Assert.Equal(2, lignes.Count);
        var of1 = lignes.Single(ligne => ligne.NumeroOF == "OF1");
        Assert.Equal(30m, of1.QuantiteReceptionneePieces); // 10 + 20, aucune quantite perdue
        Assert.Contains("PLUSIEURS_ARTICLES_POUR_OF", of1.Anomalies);
        Assert.Equal(30m, lignes.Single(ligne => ligne.NumeroOF == "OF2").QuantiteReceptionneePieces);
    }

    // 13. SCI et TRN n'entrent pas dans le Nombre de pieces Usinage 2.
    [Fact]
    public async Task NombreDePieces_IgnoreSciEtTrn()
    {
        // CTRL : SCI=500, EMR=100, TRN=300 ; rebuts : SCI=50, EMR=5, TRN=30.
        var rapport = new RapportMachineAggregateResponse(
            "OF1", "ART1",
            TotalCtrlSci: 500m, TotalCtrlEmr: 100m, TotalCtrlTrn: 300m,
            TotalRebutSci: 50m, TotalRebutEmr: 5m, TotalRebutTrn: 30m,
            NombreLignes: 3, NombreLignesEmr: 1, NombreLignesNonMappees: 0,
            PremiereDate: null, DerniereDate: null, Trouve: true);

        var service = CreateService([ReceptionValidee(90m)], entreeOf: 0m, rapport: rapport);

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(100m, ligne.QuantiteCtrlEmrPieces);
        Assert.Equal(5m, ligne.RebutEmrPieces);
        Assert.Equal(95m, ligne.NombreDePieces);
    }

    // 14. Nombre de pieces = 0 ou absent -> taux non calculables (pas de division par zero).
    [Fact]
    public async Task Taux_SontNuls_QuandLeNombreDePiecesEstNul()
    {
        // CTRL EMR = Rebut EMR : le denominateur des taux vaut zero.
        // La reception reste positive, le domaine interdisant une reception nulle.
        var service = CreateService([ReceptionValidee(5m)], entreeOf: 0m, rapport: Rapport(10m, 10m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(0m, ligne.NombreDePieces);
        Assert.Null(ligne.TauxReceptionPourcent);
        Assert.Null(ligne.TauxNonReceptionPourcent);
    }

    [Fact]
    public async Task Taux_SontCalculesEnPourcentage()
    {
        var service = CreateService([ReceptionValidee(76m)], entreeOf: 0m, rapport: Rapport(100m, 5m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(95m, ligne.NombreDePieces);
        Assert.Equal(80m, ligne.TauxReceptionPourcent);       // 76 / 95
        Assert.Equal(20m, ligne.TauxNonReceptionPourcent);    // 19 / 95
    }

    // 15, 16. Le poids physique unitaire vient du Controle poids Fonderie,
    //         il n'est JAMAIS deduit de Poids total / Nombre de pieces.
    [Fact]
    public async Task PoidsPhysiqueUnitaire_VientDuControlePoidsEtNonDuTotalDivise()
    {
        // 90 pieces receptionnees pour un poids total de 90 x 2.6 = 234 kg.
        // Le poids physique unitaire du snapshot vaut 2.6 : une division donnerait la
        // meme valeur, on choisit donc un poids total volontairement incoherent.
        var reception = Reception(90m, poidsTotalKg: 999m);
        reception.Valider(false, null, Guid.NewGuid(), "V", Now);

        var service = CreateService([reception], entreeOf: 0m, rapport: Rapport(100m, 5m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(2.6m, ligne.PoidsPhysiqueUnitaireKg);                 // valeur du controle poids
        Assert.NotEqual(999m / 90m, ligne.PoidsPhysiqueUnitaireKg!.Value); // jamais total / pieces
        Assert.Equal(999m, ligne.PoidsReceptionneTotalKg);                 // le total reste en kg
    }

    [Fact]
    public async Task ControleDesPoids_EstExposeSeparementDesPieces()
    {
        var service = CreateService([ReceptionValidee(90m)], entreeOf: 0m, rapport: Rapport(100m, 5m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(2.6m, ligne.PoidsPhysiqueUnitaireKg);
        Assert.Equal(2.5m, ligne.PoidsTheoriqueUnitaireKg);
        Assert.Equal(0.1m, ligne.EcartPoidsUnitaireKg);
        Assert.Equal(4m, ligne.TauxEcartPoidsPourcent);
        Assert.Equal("Trouve", ligne.StatutPoids);
    }

    // 17. Une reception annulee reste dans l'historique mais n'entre dans aucun total.
    [Fact]
    public async Task ReceptionAnnulee_NAlimenteAucunTotal()
    {
        var annulee = ReceptionValidee(50m);
        annulee.Annuler(Guid.NewGuid(), "A", Now);

        var service = CreateService([annulee], entreeOf: 0m, rapport: Rapport(100m, 5m));

        var bilan = await service.GetAsync(Periode, CancellationToken.None);

        // Aucune reception validee : la cle ne subsiste que si une ligne EMR existe.
        Assert.Equal(0m, bilan.QuantiteTotaleReceptionneePieces);
    }

    // 18. La periode du bilan pilote le rattachement des donnees.
    [Fact]
    public async Task Periode_EstReporteeSurChaqueLigne()
    {
        var service = CreateService([ReceptionValidee(10m)], entreeOf: 0m, rapport: Rapport(100m, 5m));

        var bilan = await service.GetAsync(Periode, CancellationToken.None);

        Assert.Equal(Periode, bilan.Periode);
        Assert.All(bilan.Lignes, ligne => Assert.Equal(Periode, ligne.Periode));
    }

    [Fact]
    public async Task RapportEmrAbsent_NeFabriqueAucuneQuantiteEmr()
    {
        var service = CreateService([ReceptionValidee(90m)], entreeOf: 100m, rapport: Rapport(0m, 0m, trouve: false));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Null(ligne.QuantiteCtrlEmrPieces);
        Assert.Null(ligne.RebutEmrPieces);
        Assert.Null(ligne.NombreDePieces);
        Assert.Null(ligne.QuantiteNonReceptionneePieces);
        Assert.Equal("DonneesManquantes", ligne.StatutReception);
        // L'anomalie RAPPORT_MACHINE_EMR_INTROUVABLE appartient a l'ALERTE de non-reception :
        // le Bilan U2 n'integre pas l'EMR, il se contente de ne rien fabriquer.
        Assert.DoesNotContain("RAPPORT_MACHINE_EMR_INTROUVABLE", ligne.Anomalies);
    }

    [Fact]
    public async Task EntreeOfIntrouvable_RendLeBilanNonCalculable()
    {
        var service = CreateService(
            [ReceptionValidee(90m)],
            entreeOf: null,
            rapport: Rapport(100m, 5m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Null(ligne.EntreeOfPieces);
        Assert.Null(ligne.BilanUsinage2Pieces);
    }

    [Fact]
    public async Task TypeMatiere_VientDeLaSortieMpEtNonDeLaReception()
    {
        var service = CreateService(
            [ReceptionValidee(90m)],
            entreeOf: 0m,
            rapport: Rapport(100m, 5m),
            sortiesMp: [new SortieMpMaterialLink("OF1", "ART1", "M3000L0")]);

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal("M3000L0", ligne.TypeMatiere);
        Assert.NotEqual("M3010L0", ligne.TypeMatiere);
    }

    [Fact]
    public async Task TypeMatiere_SortieMpAmbigue_NeChoisitPasArbitrairement()
    {
        var service = CreateService(
            [ReceptionValidee(90m)],
            entreeOf: 0m,
            rapport: Rapport(100m, 5m),
            sortiesMp:
            [
                new SortieMpMaterialLink("OF1", "ART1", "M3000L0"),
                new SortieMpMaterialLink("OF1", "ART1", "M3010L0")
            ]);

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Null(ligne.TypeMatiere);
        Assert.Contains("TYPE_MATIERE_SORTIE_MP_AMBIGUE", ligne.Anomalies);
    }

    [Fact]
    public async Task TypeMatiere_SortieMpAbsente_EstSignalee()
    {
        var service = CreateService(
            [ReceptionValidee(90m)],
            entreeOf: 0m,
            rapport: Rapport(100m, 5m),
            sortiesMp: []);

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Null(ligne.TypeMatiere);
        Assert.Contains("TYPE_MATIERE_SORTIE_MP_INTROUVABLE", ligne.Anomalies);
    }

    [Fact]
    public async Task TypeMatiere_SortieMp_EstRattacheeAOfEtArticle()
    {
        var service = CreateService(
            [
                ReceptionValidee(90m, numeroOF: "OF1", codeArticle: "ART1"),
                ReceptionValidee(80m, numeroOF: "OF2", codeArticle: "ART1")
            ],
            entreeOf: 0m,
            rapport: Rapport(100m, 5m),
            sortiesMp:
            [
                new SortieMpMaterialLink("OF1", "ART1", "M3000L0"),
                new SortieMpMaterialLink("OF2", "ART1", "M3060L0")
            ]);

        var lignes = (await service.GetAsync(Periode, CancellationToken.None)).Lignes
            .ToDictionary(ligne => ligne.NumeroOF);

        Assert.Equal("M3000L0", lignes["OF1"].TypeMatiere);
        Assert.Equal("M3060L0", lignes["OF2"].TypeMatiere);
    }

    // ======================================================================
    // PRIORITÉ 1 — Règle de population du Bilan Usinage 2
    // Seules les Réceptions U2 validées initialisent l'univers des OF.
    // Les Rebuts, Entrées OF et Sorties MP ne créent pas de nouvelles lignes.
    // ======================================================================

    // P1-A : Une Entrée OF seule (sans réception) ne crée pas de ligne Bilan U2.
    [Fact]
    public async Task EntreeOf_SansReceptionCorrespondante_NeCreeAucuneLigneBilan()
    {
        var entreeOrpheline = EntreeOf("OF_ORPHELIN", "ART_X", 50m);
        var service = CreateServiceWithEntreesRaw(
            receptions: [],
            entreesRaw: [entreeOrpheline],
            rapport: Rapport(0m, 0m, trouve: false));

        var result = await service.GetAsync(Periode, CancellationToken.None);
        Assert.Empty(result.Lignes);
    }

    // P1-B : Un Rebut U2 seul (sans réception) ne crée pas de ligne Bilan U2.
    [Fact]
    public async Task RebutU2_SansReceptionCorrespondante_NeCreeAucuneLigneBilan()
    {
        var service = CreateService(
            receptions: [],
            entreeOf: null,
            rapport: Rapport(0m, 0m, trouve: false),
            rebuts: [RebutValide(30m)]);

        var result = await service.GetAsync(Periode, CancellationToken.None);
        Assert.Empty(result.Lignes);
    }

    // P1-C : Une Sortie MP seule (sans réception) ne crée pas de ligne Bilan U2.
    [Fact]
    public async Task SortieMP_SansReceptionCorrespondante_NeCreeAucuneLigneBilan()
    {
        var service = CreateService(
            receptions: [],
            entreeOf: null,
            rapport: Rapport(0m, 0m, trouve: false),
            sortiesMp: [new SortieMpMaterialLink("OF_ORPHELIN", "ART_X", "M3010L0")]);

        var result = await service.GetAsync(Periode, CancellationToken.None);
        Assert.Empty(result.Lignes);
    }

    // P1-D : Entrée OF + Rebut U2 pour le même OF, mais sans réception → aucune ligne.
    [Fact]
    public async Task EntreeOfEtRebutU2_SansReception_NeCreeAucuneLigne()
    {
        var entreeOrpheline = EntreeOf("OF1", "ART1", 50m);
        var service = CreateServiceWithEntreesRaw(
            receptions: [],
            entreesRaw: [entreeOrpheline],
            rapport: Rapport(0m, 0m, trouve: false),
            rebuts: [RebutValide(20m)]);

        var result = await service.GetAsync(Periode, CancellationToken.None);
        Assert.Empty(result.Lignes);
    }

    // P1-E : OF1 a une réception ; OF_ORPHELIN a seulement une Entrée OF.
    //        Seule la ligne OF1 doit apparaître dans le bilan.
    [Fact]
    public async Task EntreeOf_OfSansReception_NApparaïtPasDansLeBilan()
    {
        var service = CreateServiceWithEntreesRaw(
            receptions: [ReceptionValidee(100m, numeroOF: "OF1")],
            entreesRaw:
            [
                EntreeOf("OF1", "ART1", 10m),          // enrichit OF1 existant
                EntreeOf("OF_ORPHELIN", "ART_X", 50m)  // OF sans réception → ignoré
            ],
            rapport: Rapport(100m, 5m));

        var lignes = (await service.GetAsync(Periode, CancellationToken.None)).Lignes;

        Assert.Single(lignes);
        Assert.Equal("OF1", lignes[0].NumeroOF, ignoreCase: true);
        // L'entrée de OF1 est bien prise en compte.
        Assert.Equal(10m, lignes[0].EntreeOfPieces);
    }

    // P1-F (régression) : Réception + Rebut → la ligne existe et les deux quantités sont correctes.
    [Fact]
    public async Task P1Regression_ReceptionEtRebut_LigneExisteAvecBonnesQuantites()
    {
        var service = CreateService(
            [ReceptionValidee(100m)],
            entreeOf: null,
            rapport: Rapport(100m, 5m),
            rebuts: [RebutValide(15m)]);

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(100m, ligne.QuantiteReceptionneePieces);
        Assert.Equal(15m, ligne.RebutUsinage2Pieces);
    }

    // P1-G (régression) : Réception + Entrée OF → la ligne existe et l'entrée alimente EntreeOfPieces.
    [Fact]
    public async Task P1Regression_ReceptionEtEntreeOf_LigneExisteEtEntreeEstPriseEnCompte()
    {
        var service = CreateServiceWithEntreesRaw(
            receptions: [ReceptionValidee(100m, numeroOF: "OF1")],
            entreesRaw: [EntreeOf("OF1", "ART1", 40m)],
            rapport: Rapport(100m, 5m));

        var ligne = Assert.Single((await service.GetAsync(Periode, CancellationToken.None)).Lignes);

        Assert.Equal(100m, ligne.QuantiteReceptionneePieces);
        Assert.Equal(40m, ligne.EntreeOfPieces);
    }

    // ------------------------------------------------------------------------
    /// <summary>
    /// Le service lit desormais directement les repositories (Receptions, Rebuts, Entrees OF,
    /// YRJMCH, Sorties MP) puis resout l'eligibilite atelier via les gammes et le parametre
    /// applicatif CodeAtelierUap2. Les fakes reproduisent ce contrat.
    /// </summary>
    private static BilanUsinage2Service CreateService(
        IReadOnlyList<ReceptionUsinage2> receptions,
        decimal? entreeOf,
        RapportMachineAggregateResponse rapport,
        IReadOnlyList<RebutUsinage2>? rebuts = null,
        IReadOnlyList<SortieMpMaterialLink>? sortiesMp = null)
    {
        // Un couple (OF, article) par OF present dans les receptions ou les rebuts :
        // les mouvements Entree OF et les lignes YRJMCH sont rattaches a ces memes cles,
        // sans introduire d'article supplementaire qui fausserait la detection d'ambiguite.
        var cles = receptions
            .Select(reception => (Of: reception.NumeroOF, Article: reception.CodeArticle ?? "ART1"))
            .Concat((rebuts ?? []).Select(rebut => (Of: rebut.NumeroOF, Article: rebut.CodeArticle ?? "ART1")))
            .GroupBy(cle => cle.Of, StringComparer.OrdinalIgnoreCase)
            .Select(groupe => (Of: groupe.Key, groupe.First().Article))
            .ToArray();
        var articles = cles
            .Select(cle => cle.Article)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .DefaultIfEmpty("ART1")
            .ToArray();

        return new BilanUsinage2Service(
            new FakeReceptionRepository(receptions),
            new FakeRebutRepository(rebuts ?? []),
            new FakeEntreeOfRepository(entreeOf, cles),
            new FakeReportMachineRepository(rapport, cles),
            new FakeSortieMpMaterialRepository(sortiesMp),
            new FakeReferencesRepository(articles),
            new FakeParametres());
    }

    /// <summary>
    /// Variante de CreateService acceptant des EntreeOfComposant bruts (independants
    /// des receptions). Indispensable pour les tests P1 mettant en scene des OF
    /// qui ont des entrees OF mais AUCUNE reception (cas d'OF orphelins).
    /// </summary>
    private static BilanUsinage2Service CreateServiceWithEntreesRaw(
        IReadOnlyList<ReceptionUsinage2> receptions,
        IReadOnlyList<EntreeOfComposant>? entreesRaw,
        RapportMachineAggregateResponse rapport,
        IReadOnlyList<RebutUsinage2>? rebuts = null,
        IReadOnlyList<SortieMpMaterialLink>? sortiesMp = null)
    {
        var cles = receptions
            .Select(reception => (Of: reception.NumeroOF, Article: reception.CodeArticle ?? "ART1"))
            .Concat((rebuts ?? []).Select(rebut => (Of: rebut.NumeroOF, Article: rebut.CodeArticle ?? "ART1")))
            .GroupBy(cle => cle.Of, StringComparer.OrdinalIgnoreCase)
            .Select(groupe => (Of: groupe.Key, groupe.First().Article))
            .ToArray();
        var articles = cles
            .Select(cle => cle.Article)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .DefaultIfEmpty("ART1")
            .ToArray();

        return new BilanUsinage2Service(
            new FakeReceptionRepository(receptions),
            new FakeRebutRepository(rebuts ?? []),
            new FakeEntreeOfRepositoryRaw(entreesRaw ?? []),
            new FakeReportMachineRepository(rapport, cles),
            new FakeSortieMpMaterialRepository(sortiesMp),
            new FakeReferencesRepository(articles),
            new FakeParametres());
    }

    /// <summary>Construit un EntreeOfComposant avec les valeurs minimales pour les tests P1.</summary>
    private static EntreeOfComposant EntreeOf(string numeroOF, string codeArticle, decimal quantite)
        => EntreeOfComposant.Creer(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 6),
            numeroOF,
            codeArticle,
            "Article test",
            "UN",
            quantite,
            new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc));

    private static ReceptionUsinage2 ReceptionValidee(
        decimal pieces,
        DateOnly? date = null,
        string numeroOF = "OF1",
        string codeArticle = "ART1")
    {
        var reception = Reception(pieces, date: date, numeroOF: numeroOF, codeArticle: codeArticle);
        reception.Valider(false, null, Guid.NewGuid(), "V", Now);
        return reception;
    }

    private static ReceptionUsinage2 Reception(
        decimal pieces,
        DateOnly? date = null,
        string numeroOF = "OF1",
        string codeArticle = "ART1",
        decimal? poidsTotalKg = null)
        => ReceptionUsinage2.Creer(
            numeroOF,
            date ?? new DateOnly(2026, 7, 5),
            pieces,
            poidsTotalKg ?? pieces * 2.6m,
            null,
            null,
            Snapshot(codeArticle),
            Guid.NewGuid(),
            "T",
            Now);

    private static RebutUsinage2 RebutValide(decimal pieces, DateOnly? date = null)
    {
        var rebut = RebutUsinage2.Creer(
            date ?? new DateOnly(2026, 7, 10), "OF1", "ART1", pieces, null, null, Guid.NewGuid(), "T", Now);
        rebut.Valider(Guid.NewGuid(), "V", Now);
        return rebut;
    }

    private static RebutUsinage2 RebutBrouillon(decimal pieces)
        => RebutUsinage2.Creer(new DateOnly(2026, 7, 10), "OF1", "ART1", pieces, null, null, Guid.NewGuid(), "T", Now);

    private static ReceptionUsinage2.ContexteSnapshot Snapshot(string codeArticle = "ART1")
        => new(
            codeArticle,
            "Article test",
            "En cours",
            "16",
            "M3010L0",
            2.5m,
            "KG",
            2.5m,
            2.6m,
            Guid.NewGuid(),
            10m,
            100m,
            5m,
            0m,
            5m,
            0m,
            "Trouve",
            "Trouve",
            "Trouve",
            "Trouve",
            "Trouve",
            "Trouve",
            [],
            "SortieMp",
            // Sans cette eligibilite, le service exclut la reception du Bilan U2
            // et leve RECEPTION_U2_HORS_ATELIER_EXCLUE.
            EligibleAtelier16: true);

    private static RapportMachineAggregateResponse Rapport(decimal ctrlEmr, decimal rebutEmr, bool trouve = true)
        => new("OF1", "ART1", 0m, ctrlEmr, 0m, 0m, rebutEmr, 0m, trouve ? 1 : 0, trouve ? 1 : 0, 0, null, null, trouve);

    private sealed class FakeReceptionRepository(IReadOnlyList<ReceptionUsinage2> receptions) : IReceptionUsinage2Repository
    {
        public Task<ReceptionUsinage2?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<ReceptionUsinage2?>(null);

        public Task<PagedResult<ReceptionUsinage2>> GetPagedAsync(ReceptionsUsinage2Filter filter, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<ReceptionUsinage2>([], 0));

        public Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByOfAndArticleAsync(
            string numeroOF, string codeArticle, int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)receptions
                .Where(r => r.StatutReception == StatutReceptionUsinage2.Valide
                    && r.NumeroOF == numeroOF && r.CodeArticle == codeArticle
                    && r.DateReception.Year == annee && r.DateReception.Month == mois)
                .ToArray());

        public Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)receptions
                .Where(r => r.StatutReception == StatutReceptionUsinage2.Valide && r.DateReception == date)
                .ToArray());

        public Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)receptions
                .Where(r => r.StatutReception == StatutReceptionUsinage2.Valide
                    && r.DateReception.Year == annee && r.DateReception.Month == mois)
                .ToArray());

        // Meme perimetre que GetValidesByPeriodeAsync : le filtrage d'eligibilite atelier
        // est applique par le service, pas par le fake.
        public Task<IReadOnlyList<ReceptionUsinage2>> GetToutesValidesByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)receptions
                .Where(r => r.StatutReception == StatutReceptionUsinage2.Valide
                    && r.DateReception.Year == annee && r.DateReception.Month == mois)
                .ToArray());

        public Task<IReadOnlyList<ReceptionUsinage2>> GetConcernedByControlePoidsAsync(
            string codeArticle, string codeTypeMatiere, string? numeroOF, DateOnly dateControle, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)[]);

        public Task AddAsync(ReceptionUsinage2 reception, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeRebutRepository(IReadOnlyList<RebutUsinage2> rebuts) : IRebutUsinage2Repository
    {
        public Task AddAsync(RebutUsinage2 rebut, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<RebutUsinage2?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<RebutUsinage2?>(null);

        public Task<(IReadOnlyList<RebutUsinage2> Items, int Total)> QueryAsync(
            int? annee, int? mois, string? numeroOF, string? codeArticle, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(((IReadOnlyList<RebutUsinage2>)rebuts, rebuts.Count));

        public Task<IReadOnlyList<RebutUsinage2>> GetValidesByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<RebutUsinage2>)rebuts
                .Where(r => r.Statut == StatutRebutUsinage2.Valide && r.Annee == annee && r.Mois == mois)
                .ToArray());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Mouvements Entree OF a l'unite « UN » (pieces). Une quantite nulle signifie
    /// « aucun mouvement » : le service doit alors rendre le bilan non calculable.
    /// La quantite est reportee SIGNEE, sans Math.Abs.
    /// </summary>
    private sealed class FakeEntreeOfRepository(
        decimal? quantite,
        IReadOnlyList<(string Of, string Article)> cles) : IEntreeOfRepository
    {
        private IReadOnlyList<EntreeOfComposant> Rows(int annee, int mois) => quantite is null
            ? []
            : cles.Select(cle => EntreeOfComposant.Creer(
                Guid.NewGuid(),
                new DateOnly(annee, mois, 6),
                cle.Of,
                cle.Article,
                "Article test",
                "UN",
                quantite.Value,
                new DateTime(annee, mois, 6, 0, 0, 0, DateTimeKind.Utc))).ToArray();

        public Task<IReadOnlyList<EntreeOfComposant>> GetByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult(Rows(annee, mois));

        public Task<IReadOnlyList<EntreeOfComposant>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult(Rows(date.Year, date.Month).Where(row => row.DateImputation == date).ToArray()
                as IReadOnlyList<EntreeOfComposant>);

        public Task<IReadOnlyList<EntreeOfComposant>> GetByOfAndPeriodeAsync(string numeroOF, int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult(Rows(annee, mois)
                .Where(row => string.Equals(row.NumeroOF, numeroOF, StringComparison.OrdinalIgnoreCase))
                .ToArray() as IReadOnlyList<EntreeOfComposant>);

        public Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, CancellationToken cancellationToken = default)
            => Task.FromResult<ImportBatch?>(null);
        public Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkBatchAsEchecAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<string, EntreeOfComposant>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyDictionary<string, EntreeOfComposant>)new Dictionary<string, EntreeOfComposant>());
        public Task AddRangeAsync(IEnumerable<EntreeOfComposant> items, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Variante de FakeEntreeOfRepository qui retourne directement des entrees brutes
    /// passees par les tests P1, sans les generer a partir de cles issues des receptions.
    /// Permet de mettre en scene des OFs « orphelins » (entrees sans reception).
    /// </summary>
    private sealed class FakeEntreeOfRepositoryRaw(IReadOnlyList<EntreeOfComposant> entrees) : IEntreeOfRepository
    {
        public Task<IReadOnlyList<EntreeOfComposant>> GetByPeriodeAsync(
            int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<EntreeOfComposant>)entrees
                .Where(e => e.DateImputation.Year == annee && e.DateImputation.Month == mois)
                .ToArray());

        public Task<IReadOnlyList<EntreeOfComposant>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<EntreeOfComposant>)entrees
                .Where(e => e.DateImputation == date).ToArray());

        public Task<IReadOnlyList<EntreeOfComposant>> GetByOfAndPeriodeAsync(
            string numeroOF, int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<EntreeOfComposant>)entrees
                .Where(e => string.Equals(e.NumeroOF, numeroOF, StringComparison.OrdinalIgnoreCase)
                    && e.DateImputation.Year == annee && e.DateImputation.Month == mois)
                .ToArray());

        public Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, CancellationToken cancellationToken = default)
            => Task.FromResult<ImportBatch?>(null);
        public Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkBatchAsEchecAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<string, EntreeOfComposant>> GetByCleMetiersAsync(
            IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyDictionary<string, EntreeOfComposant>)new Dictionary<string, EntreeOfComposant>());
        public Task AddRangeAsync(IEnumerable<EntreeOfComposant> items, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>Gammes actives : chaque article de test appartient a l'atelier UAP2.</summary>
    private sealed class FakeReferencesRepository(IReadOnlyList<string> articlesEligibles) : IOfContextReferencesRepository
    {
        private RefGammeFabrication Gamme(string article) => RefGammeFabrication.Creer(
            article, null, null, null, null, "10", CodeAtelierUap2, "POSTE", null, "M3010",
            null, null, null, null, null, null, null, null, null, null, null, null,
            "TEST", Guid.NewGuid(), Now);

        public Task<IReadOnlyList<RefGammeFabrication>> GetGammesActivesByArticleAsync(string codeArticle, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<RefGammeFabrication>)(
                articlesEligibles.Contains(codeArticle, StringComparer.OrdinalIgnoreCase) ? [Gamme(codeArticle)] : []));

        public Task<IReadOnlyList<RefGammeFabrication>> GetGammesActivesByArticlesAsync(IReadOnlyCollection<string> codesArticle, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<RefGammeFabrication>)codesArticle
                .Where(code => articlesEligibles.Contains(code, StringComparer.OrdinalIgnoreCase))
                .Select(Gamme)
                .ToArray());

        public Task<IReadOnlyList<RefNomenclature>> GetNomenclaturesByArticleAndComponentAsync(
            string codeArticleParent, string codeComposant, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<RefNomenclature>)[]);
    }

    /// <summary>Parametres applicatifs : seul CodeAtelierUap2 est utilise par le Bilan U2.</summary>
    private sealed class FakeParametres : IParametreApplicationProvider
    {
        public Task<string?> GetValueAsync(string cle, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(cle == ParametresApplicationCles.CodeAtelierUap2 ? CodeAtelierUap2 : null);

        public Task<decimal?> GetDecimalAsync(string cle, CancellationToken cancellationToken = default)
            => Task.FromResult<decimal?>(null);

        public Task<IReadOnlyList<string>> GetCsvValuesAsync(string cle, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<string>)[]);
    }

    private sealed class FakeSortieMpMaterialRepository(IReadOnlyList<SortieMpMaterialLink>? links)
        : ISortieMpMaterialRepository
    {
        public Task<IReadOnlyList<SortieMpMaterialLink>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SortieMpMaterialLink>)[]);

        // Univers du Bilan U2 : les liens configures par le test, sans filtrage sur des cles connues.
        public Task<IReadOnlyList<SortieMpMaterialLink>> GetAllByPeriodeAsync(
            int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SortieMpMaterialLink>)(links ?? []));

        public Task<IReadOnlyList<SortieMpMaterialLink>> GetByPeriodeAsync(
            int annee,
            int mois,
            IReadOnlyCollection<SortieMpMaterialKey> cles,
            CancellationToken cancellationToken = default)
        {
            var result = links ?? cles
                .Select(cle => new SortieMpMaterialLink(cle.NumeroOF, cle.CodeArticle, "M3010L0"))
                .ToArray();
            return Task.FromResult((IReadOnlyList<SortieMpMaterialLink>)result
                .Where(link => cles.Any(cle =>
                    string.Equals(cle.NumeroOF, link.NumeroOF, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(cle.CodeArticle, link.CodeArticle, StringComparison.OrdinalIgnoreCase)))
                .ToArray());
        }
    }

    /// <summary>
    /// Lignes YRJMCH brutes de la periode. Le service classe lui-meme SCI / EMR / TRN
    /// d'apres le prefixe du poste : le fake emet donc les trois familles, afin que les
    /// tests verifient reellement que SCI et TRN sont ignores par le Bilan U2.
    /// Rapport « non trouve » = aucune ligne, jamais une quantite nulle fabriquee.
    /// </summary>
    private sealed class FakeReportMachineRepository(
        RapportMachineAggregateResponse rapport,
        IReadOnlyList<(string Of, string Article)> cles) : IReportMachineRepository
    {
        private IReadOnlyList<X3RapportMachine> Rows(int annee, int mois)
        {
            if (!rapport.Trouve) return [];

            var lignes = new List<X3RapportMachine>();
            foreach (var (of, article) in cles)
            {
                void Ajouter(string poste, decimal total, decimal rebut) => lignes.Add(X3RapportMachine.Creer(
                    new DateOnly(annee, mois, 1),
                    poste,
                    OrigineProcessus.Inconnue, // la classification vient du prefixe du poste
                    "10",
                    article,
                    "Article test",
                    of,
                    null,
                    total,
                    rebut,
                    null,
                    "X3",
                    Guid.NewGuid(),
                    new DateTime(annee, mois, 1, 0, 0, 0, DateTimeKind.Utc)));

                Ajouter("EMR01", rapport.TotalCtrlEmr, rapport.TotalRebutEmr);
                if (rapport.TotalCtrlSci != 0m || rapport.TotalRebutSci != 0m)
                {
                    Ajouter("SCI01", rapport.TotalCtrlSci, rapport.TotalRebutSci);
                }

                if (rapport.TotalCtrlTrn != 0m || rapport.TotalRebutTrn != 0m)
                {
                    Ajouter("TRN01", rapport.TotalCtrlTrn, rapport.TotalRebutTrn);
                }
            }

            return lignes;
        }

        public Task<IReadOnlyList<X3RapportMachine>> GetByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult(Rows(annee, mois));

        public Task<IReadOnlyList<X3RapportMachine>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);

        public Task<IReadOnlyList<X3RapportMachine>> GetByOfAndArticleAsync(string numeroOF, string codeArticle, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);

        public Task<IReadOnlyList<X3RapportMachine>> GetByOfArticleAndPeriodeAsync(
            string numeroOF, string codeArticle, int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);

        public Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, string profilImport, CancellationToken cancellationToken = default)
            => Task.FromResult<ImportBatch?>(null);

        public Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAnomaliesAsync(IEnumerable<ImportReferenceAnomaly> anomalies, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRowsAsync(IEnumerable<X3RapportMachine> rows, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<X3RapportMachine>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);
        public Task<PagedResult<X3RapportMachine>> QueryAsync(string? numeroOF = null, string? codeArticle = null, string? codePoste = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<X3RapportMachine>([], 0));
        public Task<IReadOnlyList<X3RapportMachine>> GetAllActiveTrackedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);
        public Task<IReadOnlyList<PosteNonMappeSummary>> GetPostesNonMappesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<PosteNonMappeSummary>)[]);
        public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MarkBatchAsFailedAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

}
