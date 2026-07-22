using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class DechetFonderieTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TypeMatiereId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public void Creer_AvecDonneesValides_InitialiseLeDechet()
    {
        var entity = Create();

        Assert.Equal(StatutDechet.Valide, entity.Statut);
        Assert.Equal(125.5m, entity.PoidsKg);
        Assert.False(entity.EstSupprime);
    }

    [Fact]
    public void Creer_CalculeLaSemaineIso()
    {
        var entity = Create(new DateOnly(2026, 6, 30));

        Assert.Equal(27, entity.SemaineIso);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Creer_RefuseUnPoidsNonPositif(decimal poids)
    {
        Assert.Throws<DomainValidationException>(() => DechetFonderie.Creer(
            new DateOnly(2026, 6, 30),
            TypeMatiereId,
            TypeDechet.Bavure,
            poids,
            null,
            null,
            null,
            Now));
    }

    [Fact]
    public void Modifier_MetAJourLesValeursEtLaSemaine()
    {
        var entity = Create();

        entity.Modifier(
            new DateOnly(2026, 7, 6),
            TypeMatiereId,
            TypeDechet.Poudre,
            50.25m,
            "Poudre récupérée",
            Now.AddMinutes(1));

        Assert.Equal(28, entity.SemaineIso);
        Assert.Equal(TypeDechet.Poudre, entity.TypeDechet);
        Assert.Equal(50.25m, entity.PoidsKg);
    }

    [Fact]
    public void SupprimerLogiquement_MasqueEtAnnuleLeDechet()
    {
        var entity = Create();

        entity.SupprimerLogiquement(Now.AddMinutes(1));

        Assert.True(entity.EstSupprime);
        Assert.Equal(StatutDechet.Annule, entity.Statut);
        Assert.Equal(Now.AddMinutes(1), entity.DateSuppressionUtc);
    }

    private static DechetFonderie Create(DateOnly? date = null)
    {
        return DechetFonderie.Creer(
            date ?? new DateOnly(2026, 6, 30),
            TypeMatiereId,
            TypeDechet.Bavure,
            125.5m,
            "Bavures triées",
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            "Responsable Fonderie",
            Now);
    }
}
