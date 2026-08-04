using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class InventaireFonderiePeriodeTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);

    // ── Création ────────────────────────────────────────────────────

    [Fact]
    public void Creer_AvecDonneesValides_InitialiseCorrectement()
    {
        var entity = InventaireFonderiePeriode.Creer(7, 2026, Now);

        Assert.Equal(7, entity.Mois);
        Assert.Equal(2026, entity.Annee);
        Assert.Equal(StatutInventairePeriode.Brouillon, entity.Statut);
        Assert.Equal(Now, entity.CreeLeUtc);
        Assert.Null(entity.ValideLeUtc);
        Assert.Null(entity.ValidePar);
        Assert.Null(entity.ClotureLeUtc);
        Assert.Null(entity.CloturePar);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Creer_MoisInvalide_LanceDomainValidationException(int mois)
    {
        Assert.Throws<DomainValidationException>(() =>
            InventaireFonderiePeriode.Creer(mois, 2026, Now));
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Creer_AnneeInvalide_LanceDomainValidationException(int annee)
    {
        Assert.Throws<DomainValidationException>(() =>
            InventaireFonderiePeriode.Creer(7, annee, Now));
    }

    // ── Démarrer ────────────────────────────────────────────────────

    [Fact]
    public void Demarrer_DepuisBrouillon_PasseEnCours()
    {
        var entity = InventaireFonderiePeriode.Creer(7, 2026, Now);

        entity.Demarrer();

        Assert.Equal(StatutInventairePeriode.EnCours, entity.Statut);
    }

    [Fact]
    public void Demarrer_DepuisCloture_LanceDomainValidationException()
    {
        var entity = InventaireFonderiePeriode.Creer(7, 2026, Now);
        entity.Cloturer("admin", Now.AddHours(1));

        Assert.Throws<DomainValidationException>(() => entity.Demarrer());
    }

    // ── Valider ─────────────────────────────────────────────────────

    [Fact]
    public void Valider_DepuisBrouillon_PasseEnValide()
    {
        var entity = InventaireFonderiePeriode.Creer(7, 2026, Now);

        entity.Valider("admin", Now.AddMinutes(10));

        Assert.Equal(StatutInventairePeriode.Valide, entity.Statut);
        Assert.Equal("admin", entity.ValidePar);
        Assert.Equal(Now.AddMinutes(10), entity.ValideLeUtc);
    }

    [Fact]
    public void Valider_DepuisCloture_LanceDomainValidationException()
    {
        var entity = InventaireFonderiePeriode.Creer(7, 2026, Now);
        entity.Cloturer("admin", Now.AddHours(1));

        Assert.Throws<DomainValidationException>(() =>
            entity.Valider("admin", Now.AddHours(2)));
    }

    [Fact]
    public void Valider_SansUtilisateur_LanceDomainValidationException()
    {
        var entity = InventaireFonderiePeriode.Creer(7, 2026, Now);

        Assert.Throws<DomainValidationException>(() =>
            entity.Valider("   ", Now.AddMinutes(1)));
    }

    // ── Clôturer ────────────────────────────────────────────────────

    [Fact]
    public void Cloturer_DepuisValide_PasseEnCloture()
    {
        var entity = InventaireFonderiePeriode.Creer(7, 2026, Now);
        entity.Valider("admin", Now.AddMinutes(5));

        entity.Cloturer("responsable", Now.AddHours(1));

        Assert.Equal(StatutInventairePeriode.Cloture, entity.Statut);
        Assert.Equal("responsable", entity.CloturePar);
        Assert.Equal(Now.AddHours(1), entity.ClotureLeUtc);
    }

    [Fact]
    public void Cloturer_SansUtilisateur_LanceDomainValidationException()
    {
        var entity = InventaireFonderiePeriode.Creer(7, 2026, Now);

        Assert.Throws<DomainValidationException>(() =>
            entity.Cloturer("  ", Now.AddMinutes(1)));
    }
}
