using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class InventaireCategorieTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);

    // ── Création ────────────────────────────────────────────────────

    [Fact]
    public void Creer_AvecDonneesValides_InitialiseCorrectement()
    {
        var entity = CreerCategorie();

        Assert.Equal(StatutInventaire.Brouillon, entity.Statut);
        Assert.Equal(CategorieInventaireFonderie.Rebut, entity.Categorie);
        Assert.Equal("ALU", entity.CodeTypeMatiere);
        Assert.Equal(250m, entity.PoidsReelKg);
        Assert.Equal("Test commentaire", entity.Commentaire);
        Assert.Equal(Now, entity.CreeLeUtc);
    }

    [Fact]
    public void Creer_InvariantsBilanEtRapport()
    {
        var entity = CreerCategorie();

        // Partie 2 : toujours incluse dans bilan ET rapport
        Assert.True(entity.InclusBilanFonderie);
        Assert.True(entity.VisibleRapportInventaire);
    }

    [Fact]
    public void Creer_NormaliseLeCodeTypeMatiere()
    {
        var entity = CreerCategorie(codeTypeMatiere: " alu ");

        Assert.Equal("ALU", entity.CodeTypeMatiere);
    }

    [Fact]
    public void Creer_CommentaireVide_RetourneNull()
    {
        var entity = CreerCategorie(commentaire: "   ");

        Assert.Null(entity.Commentaire);
    }

    [Fact]
    public void Creer_CategorieInvalide_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreerCategorie(categorie: (CategorieInventaireFonderie)99));
    }

    [Fact]
    public void Creer_DateInventaireDefault_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            InventaireCategorie.Creer(default, CategorieInventaireFonderie.Rebut, "ALU", 250m, null, Now));
    }

    [Fact]
    public void Creer_PoidsNegatif_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreerCategorie(poidsReelKg: -1m));
    }

    [Fact]
    public void Creer_CodeTypeMatiereVide_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreerCategorie(codeTypeMatiere: ""));
    }

    [Fact]
    public void Creer_CommentaireTropLong_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreerCategorie(commentaire: new string('X', 501)));
    }

    // ── Modification ────────────────────────────────────────────────

    [Fact]
    public void Modifier_Brouillon_MetAJourLesDonnees()
    {
        var entity = CreerCategorie();

        entity.Modifier(
            new DateOnly(2026, 8, 1),
            CategorieInventaireFonderie.MpLingot,
            "ZINC",
            300m,
            "Nouveau commentaire",
            Now.AddHours(1));

        Assert.Equal(CategorieInventaireFonderie.MpLingot, entity.Categorie);
        Assert.Equal("ZINC", entity.CodeTypeMatiere);
        Assert.Equal(300m, entity.PoidsReelKg);
        Assert.Equal("Nouveau commentaire", entity.Commentaire);
        Assert.Equal(Now.AddHours(1), entity.ModifieLeUtc);
    }

    [Fact]
    public void Modifier_Valide_LanceDomainValidationException()
    {
        var entity = CreerCategorie();
        entity.Valider("admin", Now.AddMinutes(1));

        Assert.Throws<DomainValidationException>(() =>
            entity.Modifier(new DateOnly(2026, 8, 1), CategorieInventaireFonderie.Rebut, "X", 10m, null, Now.AddHours(1)));
    }

    // ── Validation ──────────────────────────────────────────────────

    [Fact]
    public void Valider_FaitPasserDeBrouillonAValide()
    {
        var entity = CreerCategorie();

        entity.Valider("admin", Now.AddMinutes(5));

        Assert.Equal(StatutInventaire.Valide, entity.Statut);
        Assert.Equal("admin", entity.ValidePar);
        Assert.Equal(Now.AddMinutes(5), entity.ValideLeUtc);
    }

    [Fact]
    public void Valider_SansUtilisateur_LanceDomainValidationException()
    {
        var entity = CreerCategorie();

        Assert.Throws<DomainValidationException>(() =>
            entity.Valider("   ", Now.AddMinutes(1)));
    }

    // ── Annulation ──────────────────────────────────────────────────

    [Fact]
    public void Annuler_DepuisBrouillon_PasseEnAnnule()
    {
        var entity = CreerCategorie();

        entity.Annuler("admin", Now.AddMinutes(5));

        Assert.Equal(StatutInventaire.Annule, entity.Statut);
        Assert.Equal("admin", entity.AnnulePar);
    }

    [Fact]
    public void Annuler_DepuisValide_PasseEnAnnule()
    {
        var entity = CreerCategorie();
        entity.Valider("admin", Now.AddMinutes(1));

        entity.Annuler("responsable", Now.AddMinutes(10));

        Assert.Equal(StatutInventaire.Annule, entity.Statut);
    }

    [Fact]
    public void Annuler_DejaAnnule_Idempotent()
    {
        var entity = CreerCategorie();
        entity.Annuler("admin", Now.AddMinutes(1));

        entity.Annuler("admin", Now.AddMinutes(5));

        Assert.Equal(Now.AddMinutes(1), entity.AnnuleLeUtc);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static InventaireCategorie CreerCategorie(
        DateOnly? dateInventaire = null,
        CategorieInventaireFonderie categorie = CategorieInventaireFonderie.Rebut,
        string codeTypeMatiere = "ALU",
        decimal poidsReelKg = 250m,
        string? commentaire = "Test commentaire")
    {
        return InventaireCategorie.Creer(
            dateInventaire.HasValue ? dateInventaire.Value : new DateOnly(2026, 7, 22),
            categorie,
            codeTypeMatiere,
            poidsReelKg,
            commentaire,
            Now);
    }
}
