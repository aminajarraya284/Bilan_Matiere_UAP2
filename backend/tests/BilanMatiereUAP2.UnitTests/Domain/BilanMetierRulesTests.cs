using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class BilanMetierRulesTests
{
    [Theory]
    [InlineData(" of123 ", "OF123")]
    [InlineData(null, "")]
    public void NormaliserCle_TrimEtMetEnMajuscules(string? value, string expected)
    {
        Assert.Equal(expected, BilanMetierRules.NormaliserCle(value));
    }

    [Fact]
    public void ClassifierPoste_DelegueLaReglePureConfirmee()
    {
        Assert.Equal(OrigineProcessus.Emr, BilanMetierRules.ClassifierPoste(" emr32 "));
    }

    [Fact]
    public void ToQuantiteMetier_ConvertitLeSigneSortieMpEnUnSeulPoint()
    {
        Assert.Equal(980m, BilanMetierRules.ToQuantiteMetier(-980m));
        Assert.Equal(-980m, BilanMetierRules.ToQuantiteMetier(980m));
    }

    [Theory]
    [MemberData(nameof(TauxCases))]
    public void CalculerTaux_RetourneNullSiDenominateurNul(decimal numerateur, decimal denominateur, decimal? expected)
    {
        Assert.Equal(expected, BilanMetierRules.CalculerTaux(numerateur, denominateur));
    }

    [Fact]
    public void CalculerBilanUsinage2_AppliqueLaFormuleEnPieces()
    {
        Assert.Equal(10m, BilanMetierRules.CalculerBilanUsinage2(900m, 20m, 870m));
    }

    public static TheoryData<decimal, decimal, decimal?> TauxCases()
        => new()
        {
            { 10m, 100m, 10m },
            { -10m, 100m, -10m },
            { 10m, 0m, null }
        };
}
