using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class InventaireFourTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);

    // ── Création ────────────────────────────────────────────────────

    [Fact]
    public void Creer_AvecDonneesValides_InitialiseCorrectement()
    {
        var entity = CreerFour();

        Assert.Equal(StatutInventaire.Brouillon, entity.Statut);
        Assert.Equal("F01", entity.CodeFour);
        Assert.Equal("ALU", entity.CodeTypeMatiere);
        Assert.Equal(500m, entity.QuantiteKg);
        Assert.Equal("Obs four", entity.Observation);
        Assert.Equal(Now, entity.CreeLeUtc);
        Assert.Equal(7, entity.Mois);
        Assert.Equal(2026, entity.Annee);
    }

    [Fact]
    public void Creer_InvariantsBilanEtRapport()
    {
        var entity = CreerFour();

        // Partie 3 : EXCLUE du bilan matière, INCLUSE dans le rapport
        Assert.False(entity.InclusBilanFonderie);
        Assert.True(entity.VisibleRapportInventaire);
    }

    [Fact]
    public void Creer_NormaliseLesChampsTexte()
    {
        var entity = CreerFour(codeFour: " f01 ", codeTypeMatiere: " alu ");

        Assert.Equal("F01", entity.CodeFour);
        Assert.Equal("ALU", entity.CodeTypeMatiere);
    }

    [Fact]
    public void Creer_CodeFourVide_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreerFour(codeFour: ""));
    }

    [Fact]
    public void Creer_QuantiteNegative_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreerFour(quantiteKg: -0.1m));
    }

    [Fact]
    public void Creer_DateInventaireDefault_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            InventaireFour.Creer(default, "F01", "ALU", 500m, null, Now));
    }

    [Fact]
    public void Creer_ObservationTropLongue_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreerFour(observation: new string('Z', 501)));
    }

    // ── Modification ────────────────────────────────────────────────

    [Fact]
    public void Modifier_Brouillon_MetAJourLesDonnees()
    {
        var entity = CreerFour();
        var newDate = new DateOnly(2026, 8, 15);

        entity.Modifier(newDate, "F02", "ZINC", 750m, "Modifié", Now.AddHours(1));

        Assert.Equal("F02", entity.CodeFour);
        Assert.Equal("ZINC", entity.CodeTypeMatiere);
        Assert.Equal(750m, entity.QuantiteKg);
        Assert.Equal(8, entity.Mois);
        Assert.Equal(2026, entity.Annee);
        Assert.Equal(Now.AddHours(1), entity.ModifieLeUtc);
    }

    [Fact]
    public void Modifier_Valide_LanceDomainValidationException()
    {
        var entity = CreerFour();
        entity.Valider("admin", Now.AddMinutes(1));

        Assert.Throws<DomainValidationException>(() =>
            entity.Modifier(new DateOnly(2026, 8, 1), "X", "Y", 10m, null, Now.AddHours(1)));
    }

    // ── Validation ──────────────────────────────────────────────────

    [Fact]
    public void Valider_FaitPasserDeBrouillonAValide()
    {
        var entity = CreerFour();

        entity.Valider("admin", Now.AddMinutes(5));

        Assert.Equal(StatutInventaire.Valide, entity.Statut);
        Assert.Equal("admin", entity.ValidePar);
    }

    [Fact]
    public void Valider_SansUtilisateur_LanceDomainValidationException()
    {
        var entity = CreerFour();

        Assert.Throws<DomainValidationException>(() =>
            entity.Valider("", Now.AddMinutes(1)));
    }

    // ── Annulation ──────────────────────────────────────────────────

    [Fact]
    public void Annuler_DepuisBrouillon_PasseEnAnnule()
    {
        var entity = CreerFour();

        entity.Annuler("admin", Now.AddMinutes(5));

        Assert.Equal(StatutInventaire.Annule, entity.Statut);
        Assert.Equal("admin", entity.AnnulePar);
    }

    [Fact]
    public void Annuler_DepuisValide_PasseEnAnnule()
    {
        var entity = CreerFour();
        entity.Valider("admin", Now.AddMinutes(1));

        entity.Annuler("responsable", Now.AddMinutes(10));

        Assert.Equal(StatutInventaire.Annule, entity.Statut);
    }

    [Fact]
    public void Annuler_DejaAnnule_Idempotent()
    {
        var entity = CreerFour();
        entity.Annuler("admin", Now.AddMinutes(1));

        entity.Annuler("admin", Now.AddMinutes(5));

        Assert.Equal(Now.AddMinutes(1), entity.AnnuleLeUtc);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static InventaireFour CreerFour(
        DateOnly? dateInventaire = null,
        string codeFour = "F01",
        string codeTypeMatiere = "ALU",
        decimal quantiteKg = 500m,
        string? observation = "Obs four")
    {
        return InventaireFour.Creer(
            dateInventaire.HasValue ? dateInventaire.Value : new DateOnly(2026, 7, 22),
            codeFour,
            codeTypeMatiere,
            quantiteKg,
            observation,
            Now);
    }
}
