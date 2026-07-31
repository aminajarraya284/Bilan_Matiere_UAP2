using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class RapportMachineTests
{
    [Fact]
    public void Creer_ConserveLOrigineProcessusFourniParLAppelant()
    {
        var emr = X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "EMR32", OrigineProcessus.Emr, "5", "ART1", null, "OF1", "E1", 100m, 2m, "1", "X3", Guid.NewGuid(), DateTime.UtcNow);
        var inconnu = X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "NOY08", OrigineProcessus.Inconnue, "5", "ART1", null, "OF1", "E1", 100m, 2m, "1", "X3", Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(OrigineProcessus.Emr, emr.OrigineProcessus);
        Assert.Equal(OrigineProcessus.Inconnue, inconnu.OrigineProcessus);
    }

    [Fact]
    public void RecalculerOrigineProcessus_MetAJourLOrigineExistante()
    {
        var rapport = X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "NOY08", OrigineProcessus.Inconnue, "5", "ART1", null, "OF1", "E1", 100m, 2m, "1", "X3", Guid.NewGuid(), DateTime.UtcNow);

        rapport.RecalculerOrigineProcessus(OrigineProcessus.Sci);

        Assert.Equal(OrigineProcessus.Sci, rapport.OrigineProcessus);
    }

    [Fact]
    public void CleMetier_DistingueLesEquipes()
    {
        var first = X3RapportMachine.GenererCleMetier(new DateOnly(2026, 7, 24), "OF1", "ART1", "5", "P1", "A", "1");
        var second = X3RapportMachine.GenererCleMetier(new DateOnly(2026, 7, 24), "OF1", "ART1", "5", "P1", "B", "1");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void QuantiteTotale_EtQuantiteRebut_SontConservees()
    {
        var rapport = X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "SCI03", OrigineProcessus.Sci, "30", "ART1", "DES", "OF1", "E1", 339m, 7m, "1", "X3", Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(339m, rapport.QuantiteTotale);
        Assert.Equal(7m, rapport.QuantiteRebut);
        Assert.Equal(OrigineProcessus.Sci, rapport.OrigineProcessus);
    }
}
