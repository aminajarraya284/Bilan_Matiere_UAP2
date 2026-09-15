using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class InventaireConteneurOfTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);

    // ── Création ────────────────────────────────────────────────────

    [Fact]
    public void Creer_AvecDonneesValides_InitialiseCorrectement()
    {
        var entity = CreerConteneur();

        Assert.Equal(StatutInventaire.Brouillon, entity.Statut);
        Assert.Equal("CNT-001", entity.NumeroConteneur);
        Assert.Equal("OF-100", entity.NumeroOF);
        Assert.Equal("ALU", entity.CodeTypeMatiere);
        Assert.Equal(150.5m, entity.PoidsReelKg);
        Assert.Equal("Test obs", entity.Observation);
        Assert.Equal(Now, entity.CreeLeUtc);
        Assert.Null(entity.ModifieLeUtc);
        Assert.Null(entity.ValideLeUtc);
        Assert.Null(entity.ValidePar);
        Assert.Null(entity.AnnuleLeUtc);
        Assert.Null(entity.AnnulePar);
    }

    [Fact]
    public void Creer_NormaliseLesChampsTexteEnMajuscules()
    {
        var entity = CreerConteneur(numeroConteneur: " cnt-001 ", numeroOF: " of-100 ", codeTypeMatiere: " alu ");

        Assert.Equal("CNT-001", entity.NumeroConteneur);
        Assert.Equal("OF-100", entity.NumeroOF);
        Assert.Equal("ALU", entity.CodeTypeMatiere);
    }

    [Fact]
    public void Creer_ObservationVide_RetourneNull()
    {
        var entity = CreerConteneur(observation: "   ");

        Assert.Null(entity.Observation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Creer_SansNumeroConteneur_LanceDomainValidationException(string? value)
    {
        var ex = Assert.Throws<DomainValidationException>(() =>
            CreerConteneur(numeroConteneur: value!));
        Assert.Contains("conteneur", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Creer_SansNumeroOF_LanceDomainValidationException(string? value)
    {
        var ex = Assert.Throws<DomainValidationException>(() =>
            CreerConteneur(numeroOF: value!));
        Assert.Contains("OF", ex.Message);
    }

    [Fact]
    public void Creer_PoidsNegatif_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreerConteneur(poidsReelKg: -1m));
    }

    [Fact]
    public void Creer_PoidsZero_EstAutorise()
    {
        var entity = CreerConteneur(poidsReelKg: 0m);
        Assert.Equal(0m, entity.PoidsReelKg);
    }

    [Fact]
    public void Creer_DateInventaireDefault_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            InventaireConteneurOf.Creer(default, "CNT-001", "OF-100", "ALU", 150m, null, Now));
    }

    [Fact]
    public void Creer_CodeTypeMatiereVide_LanceDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreerConteneur(codeTypeMatiere: ""));
    }

    [Fact]
    public void Creer_ObservationTropLongue_LanceDomainValidationException()
    {
        var obs = new string('A', 501);
        Assert.Throws<DomainValidationException>(() =>
            CreerConteneur(observation: obs));
    }

    // ── Modification ────────────────────────────────────────────────

    [Fact]
    public void Modifier_Brouillon_MetAJourLesDonnees()
    {
        var entity = CreerConteneur();
        var newDate = new DateOnly(2026, 8, 1);

        entity.Modifier(newDate, "CNT-002", "OF-200", "ZINC", 200m, "Nouvelle obs", Now.AddHours(1));

        Assert.Equal("CNT-002", entity.NumeroConteneur);
        Assert.Equal("OF-200", entity.NumeroOF);
        Assert.Equal("ZINC", entity.CodeTypeMatiere);
        Assert.Equal(200m, entity.PoidsReelKg);
        Assert.Equal("Nouvelle obs", entity.Observation);
        Assert.Equal(Now.AddHours(1), entity.ModifieLeUtc);
    }

    [Fact]
    public void Modifier_Valide_LanceDomainValidationException()
    {
        var entity = CreerConteneur();
        entity.Valider("admin", Now.AddMinutes(1));

        Assert.Throws<DomainValidationException>(() =>
            entity.Modifier(new DateOnly(2026, 8, 1), "X", "Y", "Z", 10m, null, Now.AddHours(1)));
    }

    [Fact]
    public void Modifier_Annule_LanceDomainValidationException()
    {
        var entity = CreerConteneur();
        entity.Annuler("admin", Now.AddMinutes(1));

        Assert.Throws<DomainValidationException>(() =>
            entity.Modifier(new DateOnly(2026, 8, 1), "X", "Y", "Z", 10m, null, Now.AddHours(1)));
    }

    // ── Validation ──────────────────────────────────────────────────

    [Fact]
    public void Valider_FaitPasserDeBrouillonAValide()
    {
        var entity = CreerConteneur();

        entity.Valider("admin", Now.AddMinutes(5));

        Assert.Equal(StatutInventaire.Valide, entity.Statut);
        Assert.Equal("admin", entity.ValidePar);
        Assert.Equal(Now.AddMinutes(5), entity.ValideLeUtc);
        Assert.Equal(Now.AddMinutes(5), entity.ModifieLeUtc);
    }

    [Fact]
    public void Valider_SansUtilisateur_LanceDomainValidationException()
    {
        var entity = CreerConteneur();

        Assert.Throws<DomainValidationException>(() =>
            entity.Valider("", Now.AddMinutes(1)));
    }

    [Fact]
    public void Valider_DejaValide_LanceDomainValidationException()
    {
        var entity = CreerConteneur();
        entity.Valider("admin", Now.AddMinutes(1));

        Assert.Throws<DomainValidationException>(() =>
            entity.Valider("admin", Now.AddMinutes(2)));
    }

    // ── Annulation ──────────────────────────────────────────────────

    [Fact]
    public void Annuler_DepuisBrouillon_PasseEnAnnule()
    {
        var entity = CreerConteneur();

        entity.Annuler("admin", Now.AddMinutes(5));

        Assert.Equal(StatutInventaire.Annule, entity.Statut);
        Assert.Equal("admin", entity.AnnulePar);
        Assert.Equal(Now.AddMinutes(5), entity.AnnuleLeUtc);
    }

    [Fact]
    public void Annuler_DepuisValide_PasseEnAnnule()
    {
        var entity = CreerConteneur();
        entity.Valider("admin", Now.AddMinutes(1));

        entity.Annuler("responsable", Now.AddMinutes(10));

        Assert.Equal(StatutInventaire.Annule, entity.Statut);
        Assert.Equal("responsable", entity.AnnulePar);
    }

    [Fact]
    public void Annuler_DejaAnnule_Idempotent()
    {
        var entity = CreerConteneur();
        entity.Annuler("admin", Now.AddMinutes(1));

        // Pas d'exception : opération idempotente
        entity.Annuler("admin", Now.AddMinutes(5));

        Assert.Equal(StatutInventaire.Annule, entity.Statut);
        // Le premier timestamp est conservé
        Assert.Equal(Now.AddMinutes(1), entity.AnnuleLeUtc);
    }

    [Fact]
    public void Annuler_SansUtilisateur_LanceDomainValidationException()
    {
        var entity = CreerConteneur();

        Assert.Throws<DomainValidationException>(() =>
            entity.Annuler("", Now.AddMinutes(1)));
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static InventaireConteneurOf CreerConteneur(
        DateOnly? dateInventaire = null,
        string numeroConteneur = "CNT-001",
        string? numeroOF = "OF-100",
        string codeTypeMatiere = "ALU",
        decimal poidsReelKg = 150.5m,
        string? observation = "Test obs")
    {
        return InventaireConteneurOf.Creer(
            dateInventaire.HasValue ? dateInventaire.Value : new DateOnly(2026, 7, 22),
            numeroConteneur,
            numeroOF!,
            codeTypeMatiere,
            poidsReelKg,
            observation,
            Now);
    }
}
