using System.Reflection;
using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class TraceAuditTests
{
    private static readonly DateTime DateUtc = new(2026, 8, 18, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Creer_AvecDonneesValides_ConserveLEvenement()
    {
        var trace = Creer();

        Assert.NotEqual(Guid.Empty, trace.Id);
        Assert.Equal(DateUtc, trace.DateUtc);
        Assert.Equal(ActionAudit.Validation, trace.Action);
        Assert.Equal(DomaineAudit.InventaireFonderie, trace.Domaine);
        Assert.Equal("Utilisateur SOPAL", trace.UtilisateurNom);
        Assert.Equal("corr-123", trace.CorrelationId);
    }

    [Fact]
    public void Creer_DateNonUtc_EstRefusee()
    {
        Assert.Throws<DomainValidationException>(() => Creer(dateUtc: DateTime.SpecifyKind(DateUtc, DateTimeKind.Local)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Creer_ActionOuDomaineInvalide_EstRefuse(int action, int domaine)
    {
        Assert.Throws<DomainValidationException>(() => Creer(
            action: (ActionAudit)action,
            domaine: (DomaineAudit)domaine));
    }

    [Theory]
    [InlineData("", "42", "user", "corr")]
    [InlineData("Entite", "", "user", "corr")]
    [InlineData("Entite", "42", "", "corr")]
    [InlineData("Entite", "42", "user", "")]
    public void Creer_ChampsObligatoiresVides_SontRefuses(
        string entite,
        string entiteId,
        string utilisateurId,
        string correlationId)
    {
        Assert.Throws<DomainValidationException>(() => Creer(
            entite: entite,
            entiteId: entiteId,
            utilisateurId: utilisateurId,
            correlationId: correlationId));
    }

    [Fact]
    public void Creer_NullEtZeroRestentDistincts()
    {
        var trace = Creer(anciennesValeursJson: "{\"poidsKg\":null}", nouvellesValeursJson: "{\"poidsKg\":0}");

        Assert.Contains(":null", trace.AnciennesValeursJson);
        Assert.Contains(":0", trace.NouvellesValeursJson);
    }

    [Fact]
    public void Entite_EstImmutableDepuisLExterieur()
    {
        var settersPublics = typeof(TraceAudit)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.SetMethod?.IsPublic == true)
            .ToArray();
        var methodesInterdites = typeof(TraceAudit)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name is "Modifier" or "Supprimer" or "Annuler")
            .ToArray();

        Assert.Empty(settersPublics);
        Assert.Empty(methodesInterdites);
    }

    private static TraceAudit Creer(
        DateTime? dateUtc = null,
        ActionAudit action = ActionAudit.Validation,
        DomaineAudit domaine = DomaineAudit.InventaireFonderie,
        string entite = "InventaireConteneur",
        string entiteId = "42",
        string utilisateurId = "user-1",
        string correlationId = "corr-123",
        string? anciennesValeursJson = null,
        string? nouvellesValeursJson = null)
        => TraceAudit.Creer(
            dateUtc ?? DateUtc,
            action,
            domaine,
            entite,
            entiteId,
            "2026-08|M3000L0|42",
            utilisateurId,
            "Utilisateur SOPAL",
            "user@sopal.local",
            "Admin",
            "Tests",
            correlationId,
            "Validation inventaire",
            anciennesValeursJson,
            nouvellesValeursJson,
            null);
}
