using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class TypeMatiereTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Creer_AvecDonneesValides_CreeUnTypeActif()
    {
        var entity = TypeMatiere.Creer("M3000L0", "Matière M3000L0", Now);

        Assert.Equal("M3000L0", entity.Code);
        Assert.Equal("Matière M3000L0", entity.Libelle);
        Assert.True(entity.EstActif);
    }

    [Fact]
    public void Creer_NormaliseLeCode()
    {
        var entity = TypeMatiere.Creer("  m3010l0  ", "Matière", Now);

        Assert.Equal("M3010L0", entity.Code);
    }

    [Fact]
    public void Creer_RefuseUnCodeVide()
    {
        Assert.Throws<DomainValidationException>(() => TypeMatiere.Creer("  ", "Matière", Now));
    }

    [Fact]
    public void Desactiver_DesactiveLeType()
    {
        var entity = TypeMatiere.Creer("M3000L0", "Matière", Now);

        entity.Desactiver(Now.AddMinutes(1));

        Assert.False(entity.EstActif);
        Assert.Equal(Now.AddMinutes(1), entity.DateModificationUtc);
    }

    [Fact]
    public void Activer_ActiveUnTypeDesactive()
    {
        var entity = TypeMatiere.Creer("M3000L0", "Matière", Now);
        entity.Desactiver(Now.AddMinutes(1));

        entity.Activer(Now.AddMinutes(2));

        Assert.True(entity.EstActif);
    }
}
