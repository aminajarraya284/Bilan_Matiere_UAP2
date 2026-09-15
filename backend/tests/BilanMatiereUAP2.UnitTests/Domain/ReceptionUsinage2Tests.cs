using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class ReceptionUsinage2Tests
{
    private static readonly DateTime Now = new(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc);

    private static ReceptionUsinage2.ContexteSnapshot SnapshotComplet(
        string statutArticle = "Trouve",
        string statutTypeMatiere = "Trouve",
        string statutNomenclature = "Trouve",
        string statutPoidsPhysique = "Trouve",
        decimal? poidsTheoriqueUnitaireKg = 2.5m,
        decimal? poidsPhysiqueUnitaireKg = 2.5m,
        decimal? totalCtrlEmr = 100m)
        => new(
            "07CG201-1", "CORPS TEST", "En cours", "16", "M3010L0",
            2.5m, "KG", poidsTheoriqueUnitaireKg, poidsPhysiqueUnitaireKg, Guid.NewGuid(),
            10m, totalCtrlEmr, 5m, 0m, 0m, 0m,
            statutArticle, "Trouve", statutTypeMatiere, statutNomenclature, statutPoidsPhysique, "Trouve",
            []);

    [Fact]
    public void Creer_AvecContexteComplet_CreeUneReceptionBrouillon()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, "Fonderie", null,
            SnapshotComplet(), Guid.NewGuid(), "Testeur", Now);

        Assert.Equal(StatutReceptionUsinage2.Brouillon, reception.StatutReception);
        Assert.Equal("OF123", reception.NumeroOF);
        Assert.Equal("07CG201-1", reception.CodeArticle);
    }

    [Theory]
    [InlineData("ArticleIntrouvable", "Trouve", "Trouve")]
    [InlineData("Trouve", "MouvementAbsent", "Trouve")]
    [InlineData("Trouve", "Trouve", "NomenclatureIntrouvable")]
    public void Creer_SansArticleOuMatiereOuNomenclatureTrouve_ResteEnregistreAvecAnomalies(string statutArticle, string statutMatiere, string statutNomenclature)
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null,
            SnapshotComplet(statutArticle, statutMatiere, statutNomenclature), Guid.NewGuid(), "Testeur", Now);

        Assert.Equal(statutArticle, reception.StatutArticle);
        Assert.Equal(statutMatiere, reception.StatutTypeMatiere);
        Assert.Equal(statutNomenclature, reception.StatutNomenclature);
    }

    [Fact]
    public void Creer_NombrePiecesNegatifOuNul_Echoue()
    {
        Assert.Throws<DomainValidationException>(() => ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 0m, 250m, null, null,
            SnapshotComplet(), Guid.NewGuid(), "Testeur", Now));
    }

    [Fact]
    public void Modifier_SurBrouillon_MetAJourLesChampsSaisis()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null,
            SnapshotComplet(), Guid.NewGuid(), "Testeur", Now);

        reception.Modifier(150m, 300m, "Fonderie", "Note", Guid.NewGuid(), "Modificateur", Now);

        Assert.Equal(150m, reception.NombrePiecesReceptionnees);
        Assert.Equal(300m, reception.PoidsTotalReceptionneKg);
        Assert.Equal("Fonderie", reception.Origine);
    }

    [Fact]
    public void Modifier_SurReceptionValidee_Echoue()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null,
            SnapshotComplet(), Guid.NewGuid(), "Testeur", Now);
        reception.Valider(false, null, Guid.NewGuid(), "Validateur", Now);

        Assert.Throws<DomainValidationException>(() => reception.Modifier(150m, 300m, null, null, Guid.NewGuid(), "X", Now));
    }

    // Le poids physique unitaire est une donnee AUTOMATIQUE, issue du controle poids Fonderie.
    // Son absence est une anomalie visible, jamais un blocage de saisie : la validation reste
    // possible et la reception conserve un poids physique NUL (jamais un faux zero).
    [Fact]
    public void Valider_SansPoidsPhysique_ResteAutoriseeEtLaisseLAnomalieVisible()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null,
            SnapshotComplet(statutPoidsPhysique: "PoidsPhysiqueManquant", poidsPhysiqueUnitaireKg: null),
            Guid.NewGuid(), "Testeur", Now);

        reception.Valider(false, null, Guid.NewGuid(), "X", Now);

        Assert.Equal(StatutReceptionUsinage2.Valide, reception.StatutReception);
        Assert.Null(reception.PoidsPhysiqueUnitaireKg);
        Assert.Equal("PoidsPhysiqueManquant", reception.StatutPoidsPhysique);
    }

    [Fact]
    public void Valider_DeuxFois_EstRefuse()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null,
            SnapshotComplet(), Guid.NewGuid(), "Testeur", Now);
        reception.Valider(false, null, Guid.NewGuid(), "X", Now);

        Assert.Throws<DomainValidationException>(() => reception.Valider(false, null, Guid.NewGuid(), "X", Now));
    }

    [Fact]
    public void Valider_SansPoidsPhysiqueAvecDerogationEtJustification_Reussit()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null,
            SnapshotComplet(statutPoidsPhysique: "PoidsPhysiqueManquant", poidsPhysiqueUnitaireKg: null),
            Guid.NewGuid(), "Testeur", Now);

        reception.Valider(true, "Validation exceptionnelle SOPAL", Guid.NewGuid(), "Admin", Now);

        Assert.Equal(StatutReceptionUsinage2.Valide, reception.StatutReception);
        Assert.Equal("Validation exceptionnelle SOPAL", reception.DerogationJustification);
    }

    [Fact]
    public void Valider_AvecPoidsPhysique_NeRequiertPasDeDerogation()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null,
            SnapshotComplet(), Guid.NewGuid(), "Testeur", Now);

        reception.Valider(false, null, Guid.NewGuid(), "Validateur", Now);

        Assert.Equal(StatutReceptionUsinage2.Valide, reception.StatutReception);
        Assert.Null(reception.DerogationJustification);
    }

    [Fact]
    public void Annuler_DepuisBrouillonOuValide_FonctionneEtEstIdempotent()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null,
            SnapshotComplet(), Guid.NewGuid(), "Testeur", Now);

        reception.Annuler(Guid.NewGuid(), "X", Now);
        Assert.Equal(StatutReceptionUsinage2.Annule, reception.StatutReception);

        // Idempotent : un second appel ne doit pas lever d'exception.
        reception.Annuler(Guid.NewGuid(), "X", Now);
        Assert.Equal(StatutReceptionUsinage2.Annule, reception.StatutReception);
    }

    [Fact]
    public void RecalculerContexte_SurBrouillon_MetAJourLeSnapshotSansToucherAuxChampsSaisis()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, "Fonderie", "Note",
            SnapshotComplet(), Guid.NewGuid(), "Testeur", Now);

        var nouveauSnapshot = SnapshotComplet(totalCtrlEmr: 200m);
        reception.RecalculerContexte(nouveauSnapshot, Guid.NewGuid(), "X", Now);

        Assert.Equal(200m, reception.TotalCtrlEmr);
        Assert.Equal(100m, reception.NombrePiecesReceptionnees);
        Assert.Equal("Fonderie", reception.Origine);
    }

    [Fact]
    public void RecalculerContexte_SurReceptionValidee_Echoue()
    {
        var reception = ReceptionUsinage2.Creer(
            "OF123", new DateOnly(2026, 7, 26), 100m, 250m, null, null,
            SnapshotComplet(), Guid.NewGuid(), "Testeur", Now);
        reception.Valider(false, null, Guid.NewGuid(), "X", Now);

        Assert.Throws<DomainValidationException>(() => reception.RecalculerContexte(SnapshotComplet(), Guid.NewGuid(), "X", Now));
    }
}
