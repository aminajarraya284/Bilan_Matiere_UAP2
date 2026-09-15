using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class ManufacturingReferencesTests
{
    [Fact]
    public void Gamme_CleMetier_ContientAtelierPosteEtAlternative()
    {
        var key = RefGammeFabrication.GenererCleMetier(" art1 ", " alt1 ", " 5 ", " atelier ", " poste ");

        Assert.Equal("ART1|5|ATELIER|POSTE|ALT1", key);
    }

    [Fact]
    public void Gamme_EstValideLe_RespecteLaDateDeFin()
    {
        var gamme = RefGammeFabrication.Creer(
            "ART1", "Article", new DateOnly(2026, 7, 31), null, null, "5", "ATELIER", "POSTE",
            null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            "X3", Guid.NewGuid(), DateTime.UtcNow);

        Assert.True(gamme.EstValideLe(new DateOnly(2026, 7, 31)));
        Assert.False(gamme.EstValideLe(new DateOnly(2026, 8, 1)));
    }

    [Fact]
    public void Gamme_EstValideLe_TraiteLaDateSentinelleX3CommeToujoursValide()
    {
        // X3 exporte "sans limite de validite" via une date sentinelle tres ancienne (31/12/1599
        // observee sur 99% des lignes reelles GAMMEFAB) plutot qu'une valeur NULL.
        var gamme = RefGammeFabrication.Creer(
            "ART1", "Article", new DateOnly(1599, 12, 31), null, null, "5", "ATELIER", "POSTE",
            null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            "X3", Guid.NewGuid(), DateTime.UtcNow);

        Assert.True(gamme.EstValideLe(new DateOnly(2026, 7, 31)));
        Assert.True(gamme.EstValideLe(new DateOnly(2099, 1, 1)));
    }

    [Fact]
    public void Nomenclature_ConserveLesDeuxQuantites()
    {
        var nomenclature = RefNomenclature.Creer(
            "ART1", null, "ART1", "M3000L0", "1", "10", "Matiere", 2.5m, 12.5m, "KG", "MATIERE", "NORMAL", 0m,
            "X3", Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(2.5m, nomenclature.QuantiteLien);
        Assert.Equal(12.5m, nomenclature.QuantiteLienCumulee);
        Assert.NotEqual(nomenclature.QuantiteLien, nomenclature.QuantiteLienCumulee);
    }

    [Fact]
    public void RapportMachine_CleMetier_DistingueLesEquipes()
    {
        var first = X3RapportMachine.GenererCleMetier(new DateOnly(2026, 7, 24), "OF1", "ART1", "5", "P1", "A", "1");
        var second = X3RapportMachine.GenererCleMetier(new DateOnly(2026, 7, 24), "OF1", "ART1", "5", "P1", "B", "1");

        Assert.NotEqual(first, second);
    }
}
