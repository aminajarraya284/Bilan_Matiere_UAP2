using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Services;

namespace BilanMatiereUAP2.UnitTests.Domain;

/// <summary>
/// Regle arbitree : le comptage physique (parties 1 et 2) constitue le STOCK FIN
/// de la periode. Les fours sont traces mais EXCLUS du stock. Le stock debut d'une
/// periode est le stock fin de la periode precedente.
/// </summary>
public sealed class InventaireComptageRulesTests
{
    private static readonly DateTime Now = new(2026, 7, 30, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid M3010 = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid M3000 = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly DateOnly Juillet = new(2026, 7, 1);

    private static InventaireConteneur Conteneur(decimal kg, Guid? matiere = null, bool valide = true)
    {
        var ligne = InventaireConteneur.Creer(
            Juillet, "C-001", "OF26070378", "07BL201-2", "CORPS", matiere ?? M3010, kg, null, null, "T", Now);
        if (valide) ligne.Valider(null, "V", Now);
        return ligne;
    }

    private static InventaireCategorieComptage Categorie(decimal kg, Guid? matiere = null, bool valide = true)
    {
        var ligne = InventaireCategorieComptage.Creer(
            Juillet, "REBUT", matiere ?? M3010, kg, null, null, "T", Now);
        if (valide) ligne.Valider(null, "V", Now);
        return ligne;
    }

    private static InventaireFourComptage Four(decimal kg, Guid? matiere = null, bool valide = true)
    {
        var ligne = InventaireFourComptage.Creer(
            Juillet, "Four CBP 01", matiere ?? M3010, kg, null, null, "T", Now);
        if (valide) ligne.Valider(null, "V", Now);
        return ligne;
    }

    [Fact]
    public void StockFin_EstLaSommeDesConteneursEtDesCategories()
    {
        var totaux = InventaireComptageRules.CalculerTotaux(
            [Conteneur(120m), Conteneur(80m)],
            [Categorie(50m)],
            []);

        var total = Assert.Single(totaux);
        Assert.Equal(200m, total.TotalConteneursKg);
        Assert.Equal(50m, total.TotalCategoriesKg);
        Assert.Equal(250m, total.StockFinKg);
    }

    // Exigence explicite : l'inventaire du four reste exclu du bilan.
    [Fact]
    public void LesFours_SontExclusDuStockFinMaisPresentsDansLeRapport()
    {
        var totaux = InventaireComptageRules.CalculerTotaux(
            [Conteneur(100m)],
            [Categorie(40m)],
            [Four(999m)]);

        var total = Assert.Single(totaux);
        Assert.Equal(999m, total.TotalFoursKg);
        Assert.Equal(140m, total.StockFinKg);        // le four n'y entre pas
        Assert.Equal(1139m, total.TotalRapportKg);   // mais il reste visible au rapport
    }

    [Theory]
    [InlineData(false)]
    public void LesLignesNonValidees_NAlimententPasLeStock(bool valide)
    {
        var totaux = InventaireComptageRules.CalculerTotaux(
            [Conteneur(100m, valide: valide)],
            [Categorie(40m, valide: valide)],
            []);

        Assert.Empty(totaux);
    }

    [Fact]
    public void UneLigneAnnulee_SortDuStockMaisResteDansLHistorique()
    {
        var conteneur = Conteneur(100m);
        conteneur.Annuler(null, "A", Now);

        var totaux = InventaireComptageRules.CalculerTotaux([conteneur], [], []);

        Assert.Empty(totaux);
        Assert.False(conteneur.EstRetenuePourLeStock);
    }

    [Fact]
    public void LesTotaux_SontSeparesParTypeMatiere()
    {
        var totaux = InventaireComptageRules.CalculerTotaux(
            [Conteneur(100m, M3010), Conteneur(70m, M3000)],
            [Categorie(30m, M3000)],
            []);

        Assert.Equal(2, totaux.Count);
        Assert.Equal(100m, totaux.Single(t => t.TypeMatiereId == M3010).StockFinKg);
        Assert.Equal(100m, totaux.Single(t => t.TypeMatiereId == M3000).StockFinKg); // 70 + 30
    }

    [Fact]
    public void StockDebut_EstLeStockFinDeLaPeriodePrecedente()
    {
        Assert.Equal(new DateOnly(2026, 6, 1), InventaireComptageRules.PeriodePrecedente(Juillet));
        Assert.Equal(new DateOnly(2025, 12, 1), InventaireComptageRules.PeriodePrecedente(new DateOnly(2026, 1, 15)));
    }

    [Fact]
    public void UneQuantiteNegative_EstRefusee()
    {
        Assert.Throws<DomainValidationException>(() => InventaireConteneur.Creer(
            Juillet, "C-001", null, null, null, M3010, -1m, null, null, "T", Now));
    }

    [Fact]
    public void UnConteneurSansNumero_EstRefuse()
    {
        Assert.Throws<DomainValidationException>(() => InventaireConteneur.Creer(
            Juillet, "  ", null, null, null, M3010, 10m, null, null, "T", Now));
    }

    [Fact]
    public void UneLigneValidee_NEstPlusModifiable()
    {
        var conteneur = Conteneur(100m);

        Assert.Throws<DomainValidationException>(() => conteneur.Modifier(
            Juillet, "C-002", null, null, null, M3010, 50m, null, null, "M", Now));
    }
}
