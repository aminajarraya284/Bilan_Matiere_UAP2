using System.Reflection;
using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;

namespace BilanMatiereUAP2.UnitTests.Domain;

public sealed class AlerteMetierTests
{
    private static readonly DateTime DetectionUtc = new(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Creer_NormaliseLesIdentifiantsEtInitialiseUneAlerteOuverte()
    {
        var alerte = Creer(code: "  u2_quantite_non_receptionnee  ");

        Assert.Equal("U2_QUANTITE_NON_RECEPTIONNEE", alerte.Code);
        Assert.Equal("2026-01", alerte.Periode);
        Assert.Equal("OF-001", alerte.NumeroOF);
        Assert.Equal(StatutAlerte.Ouverte, alerte.Statut);
        Assert.Equal(DetectionUtc, alerte.DetecteeLeUtc);
        Assert.Equal(DetectionUtc, alerte.DerniereDetectionLeUtc);
        Assert.True(alerte.EstActive);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("titre")]
    [InlineData("description")]
    [InlineData("source")]
    [InlineData("cle")]
    public void Creer_RefuseLesChampsObligatoiresVides(string champ)
    {
        Assert.Throws<DomainValidationException>(() => AlerteMetier.Creer(
            champ == "code" ? " " : "CODE",
            TypeAlerte.AnomalieMetier,
            SeveriteAlerte.Avertissement,
            champ == "titre" ? " " : "Titre",
            champ == "description" ? " " : "Description",
            champ == "source" ? " " : "Bilan Usinage 2",
            "2026-01",
            "OF-001",
            "ART-001",
            null,
            champ == "cle" ? " " : "CLE",
            true,
            null,
            DetectionUtc));
    }

    [Fact]
    public void Creer_RefuseUneDateNonUtc()
    {
        Assert.Throws<DomainValidationException>(() => Creer(
            date: new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Local)));
    }

    [Fact]
    public void Acquitter_EstIdempotentEtConserveLaPremiereTrace()
    {
        var alerte = Creer();
        var date = DetectionUtc.AddHours(1);

        alerte.Acquitter("operateur", date, "Pris en charge");
        alerte.Acquitter("autre", date.AddHours(1), "Ne doit pas remplacer");

        Assert.Equal(StatutAlerte.Acquittee, alerte.Statut);
        Assert.Equal("operateur", alerte.AcquitteePar);
        Assert.Equal(date, alerte.AcquitteeLeUtc);
        Assert.Equal("Pris en charge", alerte.CommentaireTraitement);
    }

    [Fact]
    public void Resoudre_EstIdempotentEtRendLAlerteInactive()
    {
        var alerte = Creer();
        var date = DetectionUtc.AddHours(1);

        alerte.Resoudre("responsable", date, "Source corrigée");
        alerte.Resoudre("autre", date.AddHours(1));

        Assert.Equal(StatutAlerte.Resolue, alerte.Statut);
        Assert.False(alerte.EstActive);
        Assert.Equal("responsable", alerte.ResoluePar);
        Assert.Equal(date, alerte.ResolueLeUtc);
    }

    [Fact]
    public void Ignorer_ExigeUneJustificationEtInterditEnsuiteLaResolution()
    {
        var alerte = Creer();

        Assert.Throws<DomainValidationException>(() => alerte.Ignorer("responsable", DetectionUtc.AddMinutes(1), " "));

        alerte.Ignorer("responsable", DetectionUtc.AddMinutes(2), "Cas validé hors périmètre");

        Assert.Equal(StatutAlerte.Ignoree, alerte.Statut);
        Assert.False(alerte.EstActive);
        Assert.Throws<DomainValidationException>(() => alerte.Resoudre("responsable", DetectionUtc.AddMinutes(3)));
    }

    [Fact]
    public void MettreAJourDetection_ConserveLaPremiereDateEtActualiseLaDerniere()
    {
        var alerte = Creer();
        var nouvelleDetection = DetectionUtc.AddDays(1);

        alerte.MettreAJourDetection(
            SeveriteAlerte.Erreur,
            "Titre actualisé",
            "Description actualisée",
            true,
            "{\"unite\":\"pieces\"}",
            nouvelleDetection);

        Assert.Equal(DetectionUtc, alerte.DetecteeLeUtc);
        Assert.Equal(nouvelleDetection, alerte.DerniereDetectionLeUtc);
        Assert.Equal(SeveriteAlerte.Erreur, alerte.Severite);
    }

    [Fact]
    public void ResoudreAutomatiquement_NAgitQueSurUneConditionEligible()
    {
        var automatique = Creer(resolutionAutomatique: true);
        var humaine = Creer(resolutionAutomatique: false);

        automatique.ResoudreAutomatiquement(DetectionUtc.AddHours(1));
        humaine.ResoudreAutomatiquement(DetectionUtc.AddHours(1));

        Assert.Equal(StatutAlerte.Resolue, automatique.Statut);
        Assert.Equal("SYSTEME", automatique.ResoluePar);
        Assert.Equal(StatutAlerte.Ouverte, humaine.Statut);
    }

    [Fact]
    public void Entite_NExposeAucunSetterPublic()
    {
        var settersPublics = typeof(AlerteMetier)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.SetMethod?.IsPublic == true)
            .ToArray();

        Assert.Empty(settersPublics);
    }

    [Fact]
    public void Deduplication_NormaliseEtProtegeLeSeparateur()
    {
        var cle = AlerteDeduplication.Construire(
            " u2_code ",
            "2026-01",
            " of|1 ",
            null,
            " m3000l0 ",
            " bilan usinage 2 ");

        Assert.Equal("U2_CODE|2026-01|OF%7C1|<NULL>|M3000L0|BILAN USINAGE 2", cle);
    }

    private static AlerteMetier Creer(
        string code = "U2_QUANTITE_NON_RECEPTIONNEE",
        bool resolutionAutomatique = true,
        DateTime? date = null)
        => AlerteMetier.Creer(
            code,
            TypeAlerte.AnomalieMetier,
            SeveriteAlerte.Avertissement,
            "Quantité non réceptionnée",
            "Le bilan source conserve une quantité positive non réceptionnée.",
            "Bilan Usinage 2",
            "2026-01",
            "of-001",
            "art-001",
            "m3000l0",
            "U2_QUANTITE_NON_RECEPTIONNEE|2026-01|OF-001|ART-001|M3000L0|BILAN USINAGE 2",
            resolutionAutomatique,
            null,
            date ?? DetectionUtc);
}
