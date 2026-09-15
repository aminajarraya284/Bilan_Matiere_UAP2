using BilanMatiereUAP2.Infrastructure.Services;

namespace BilanMatiereUAP2.UnitTests.Infrastructure;

public sealed class NomenclatureExcelReaderTests
{
    private static readonly Guid BatchId = Guid.NewGuid();

    [Fact]
    public void Niveau1_ColonneQuantiteLienVide_DecaleLaLectureVersLesColonnes7A12()
    {
        // Ligne reelle de niveau 1 : la colonne 6 est vide (pas de QuantiteLien propre a ce niveau).
        object?[] row = ["0101A03-1", "0203203-1", "0101A03-1", "1", "CORPS MINI VANNE BS 1/2\" MF N", "5", "", "1", "1", "UN", "FAB", "N", "0"];

        var result = NomenclatureExcelReader.TryParseRow(row, "X3", BatchId);

        Assert.NotNull(result);
        Assert.Equal(1m, result!.QuantiteLien);
        Assert.Equal(1m, result.QuantiteLienCumulee);
        Assert.Equal("UN", result.UniteQuantite);
        Assert.Equal("FAB", result.CategorieComposant);
        Assert.Equal("N", result.TypeLien);
        Assert.Equal(0m, result.PourcentageRebut);
    }

    [Fact]
    public void Niveau2_ColonneQuantiteLienRemplie_UtiliseLesColonnes6A11SansDecalage()
    {
        // Ligne reelle de niveau 2 : la colonne 6 contient directement QuantiteLien, pas de decalage.
        object?[] row = ["0203203-1", "0203201-1", "0101A03-1", "2", "CORPS MINI VANNE BS 1/2\" MF J", "2", "1", "1", "UN", "FAB", "N", "0", ""];

        var result = NomenclatureExcelReader.TryParseRow(row, "X3", BatchId);

        Assert.NotNull(result);
        Assert.Equal(1m, result!.QuantiteLien);
        Assert.Equal(1m, result.QuantiteLienCumulee);
        Assert.Equal("UN", result.UniteQuantite);
        Assert.Equal("FAB", result.CategorieComposant);
        Assert.Equal("N", result.TypeLien);
        Assert.Equal(0m, result.PourcentageRebut);
    }

    [Fact]
    public void Niveau2_QuantiteLienDiffereDeQuantiteLienCumulee_ConserveLesDeuxValeurs()
    {
        // Une sous-piece profonde peut avoir une quantite cumulee differente de la quantite du lien direct.
        object?[] row = ["M400000", "M400S00", "0701201-1", "2", "TOURNURE DE LAITON SOUS-TRAITANCE", "2", "1", "0,072", "KG", "ACL", "N", "0", ""];

        var result = NomenclatureExcelReader.TryParseRow(row, "X3", BatchId);

        Assert.NotNull(result);
        Assert.Equal(1m, result!.QuantiteLien);
        Assert.Equal(0.072m, result.QuantiteLienCumulee);
        Assert.NotEqual(result.QuantiteLien, result.QuantiteLienCumulee);
    }

    [Fact]
    public void LigneTitreNomenclatureProduit_EstIgnoree()
    {
        object?[] row = ["Nomenclature produit", "16/06/2026 00:00:00", "multiniveaux(Production)", "31/12/1899 10:19:03", "", "", "", "", "", "", "", "", ""];

        var result = NomenclatureExcelReader.TryParseRow(row, "X3", BatchId);

        Assert.Null(result);
    }

    [Fact]
    public void LigneTitreArticleParent_EstIgnoree()
    {
        object?[] row = ["Article  parent    :", "0101A03-1", "MINI VANNE BS 1/2\" MF NICKELEE PAPILLON ROUGE", "1", "Unité de gestion :", "pour un"];

        var result = NomenclatureExcelReader.TryParseRow(row, "X3", BatchId);

        Assert.Null(result);
    }

    [Fact]
    public void LigneEnTeteTypAltParentEnfant_EstIgnoree()
    {
        object?[] row = ["Typ.\nAlt.", "Parent", "Enfant", "Prt Niv_0", "Niv.", "Désignation", "Séq.", "Qté lien", "Q lien cum", "Cat", "Type lien", "%Reb", ""];

        var result = NomenclatureExcelReader.TryParseRow(row, "X3", BatchId);

        Assert.Null(result);
    }

    [Fact]
    public void LigneVide_EstIgnoree()
    {
        object?[] row = ["", "", "", "", "", "", "", "", "", "", "", "", ""];

        var result = NomenclatureExcelReader.TryParseRow(row, "X3", BatchId);

        Assert.Null(result);
    }

    [Fact]
    public void DecimalAvecVirgule_EstParseCorrectement()
    {
        // Valeur reelle observee : temps/quantite avec virgule decimale francaise.
        object?[] row = ["0203201-1", "M1180RP", "0101A03-1", "3", "LAITON Ø 18 (SUIVANT EN12165 CW617N)LONGUEUR 4M", "2", "0,0119047619048", "0,0119047619048", "BA", "ACL", "N", "0", ""];

        var result = NomenclatureExcelReader.TryParseRow(row, "X3", BatchId);

        Assert.NotNull(result);
        Assert.Equal(0.0119047619048m, result!.QuantiteLien);
        Assert.Equal(0.0119047619048m, result.QuantiteLienCumulee);
    }

    [Fact]
    public void CleMetier_GenereeCorrectementPourUneLigneNiveau1()
    {
        object?[] row = ["0101A03-1", "0323203-1", "0101A03-1", "1", "PORTE SIEGE MINI VANNE NICKELE", "10", "", "1", "1", "UN", "FST", "N", "0"];

        var result = NomenclatureExcelReader.TryParseRow(row, "X3", BatchId);

        Assert.NotNull(result);
        Assert.Equal("0101A03-1", result!.CodeArticleParent);
        Assert.Equal("0323203-1", result.CodeComposant);
        Assert.Equal("1", result.Niveau);
        Assert.Equal("10", result.Sequence);
    }
}
