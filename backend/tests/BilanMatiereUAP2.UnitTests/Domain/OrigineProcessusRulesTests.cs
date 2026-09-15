using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class OrigineProcessusRulesTests
{
    [Theory]
    [InlineData("SCI03", OrigineProcessus.Sci)]
    [InlineData("sci03", OrigineProcessus.Sci)]
    [InlineData(" SCI03 ", OrigineProcessus.Sci)]
    [InlineData("EMR39", OrigineProcessus.Emr)]
    [InlineData("TRN07", OrigineProcessus.Trn)]
    [InlineData("CTRL", OrigineProcessus.Ctrl)]
    [InlineData(" ctrl ", OrigineProcessus.Ctrl)]
    [InlineData("ABC01", OrigineProcessus.Inconnue)]
    [InlineData("", OrigineProcessus.Inconnue)]
    [InlineData(null, OrigineProcessus.Inconnue)]
    public void DeterminerOrigineProcessus_ClasseSelonLesTroisPremiersCaracteres(
        string? poste,
        OrigineProcessus attendu)
    {
        Assert.Equal(attendu, OrigineProcessusRules.DeterminerOrigineProcessus(poste));
    }

    [Theory]
    [InlineData("NOY08")]
    [InlineData("GRNFND")]
    [InlineData("COQ10")]
    [InlineData("CBP01")]
    public void EstPrefixeDetecteNonValide_DetecteLesPostesHorsAgregat(string poste)
    {
        Assert.True(OrigineProcessusRules.EstPrefixeDetecteNonValide(poste));
        Assert.Equal(OrigineProcessus.Inconnue, OrigineProcessusRules.DeterminerOrigineProcessus(poste));
    }

    [Fact]
    public void EstPrefixeDetecteNonValide_NeClassePasLesProcessusConfirmes()
    {
        Assert.False(OrigineProcessusRules.EstPrefixeDetecteNonValide("SCI03"));
    }
}
