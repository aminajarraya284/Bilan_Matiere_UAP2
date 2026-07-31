using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Infrastructure.Services;

namespace BilanMatiereUAP2.UnitTests.Infrastructure;

public sealed class X3ExcelReadersContractTests
{
    [Fact]
    public void DetecterLigneEntete_Yrjmch_AccepteQuatreCorrespondancesDansLesTrentePremieresLignes()
    {
        var rows = new List<object?[]>
        {
            new object?[] { "Rapport journalier machine" },
            new object?[] { "Centre de charge" },
            new object?[] { "Rapport jour" },
            Array.Empty<object?>(),
            new object?[] { "Date", "Poste", "Ope", "Article", "Désignation", "Numéro OF", "Equipe", "Qté Tot", "Qté R" }
        };

        var line = ExcelWorkbook.DetecterLigneEntete(
            rows,
            ["date", "poste", "ope", "article", "numeroof", "qtetot"],
            4);

        Assert.Equal(4, line);
    }

    [Fact]
    public void DetecterLigneEntete_Mvt_AccepteLesDeuxVariantesDePieceOrigine()
    {
        var rowWithOrigin = new object?[] { "Date imput", "Code", "Quantité", "Transaction", "Pièce Origin" };
        var rowWithOrigine = new object?[] { "Date imput", "Code", "Quantité", "Transaction", "Pièce Origine." };

        Assert.Equal(0, ExcelWorkbook.DetecterLigneEntete([rowWithOrigin], ["dateimput", "code", "quantite", "transaction", "pieceorigine"], 4));
        Assert.Equal(0, ExcelWorkbook.DetecterLigneEntete([rowWithOrigine], ["dateimput", "code", "quantite", "transaction", "pieceorigine"], 4));
        Assert.Contains("pieceorigine", ExcelWorkbook.CreateHeaderMap(rowWithOrigin).Keys);
    }

    [Fact]
    public void DetecterLigneEntete_Mvt_PeutEtreEnDixiemeLigne()
    {
        var rows = Enumerable.Range(0, 9)
            .Select(_ => Array.Empty<object?>())
            .Append(new object?[] { "Date imput", "Code", "Quantité", "Transaction", "Pièce Origine." })
            .ToArray();

        Assert.Equal(9, ExcelWorkbook.DetecterLigneEntete(
            rows,
            ["dateimput", "code", "quantite", "transaction", "pieceorigine"],
            4));
    }

    [Fact]
    public void Sheet2SansEntete_PeutReutiliserLeMappingDeLaPremiereFeuille()
    {
        var header = new object?[] { "Date imput", "Code", "Quantité", "Transaction", "Pièce Origine" };
        var data = new object?[] { "2026-01-01", "M3000L0", -980m, "Entrée OF", "OF1" };
        var columns = ExcelWorkbook.CreateHeaderMap(header);

        Assert.Equal("OF1", ExcelWorkbook.Text(ExcelWorkbook.Get(data, columns, "pieceorigine")));
        Assert.Equal(-980m, ExcelWorkbook.Get(data, columns, "quantite"));
    }

    [Fact]
    public void QuantitesSignees_AnnulationMoinsPlusDonneZero()
    {
        Assert.True(X3ImportParsing.TryParseDecimal("-980", out var negative));
        Assert.True(X3ImportParsing.TryParseDecimal("+980", out var positive));

        Assert.Equal(0m, negative + positive);
    }

    [Fact]
    public async Task MvtRecette_ReconnaitLesExportsDisponibles()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        var files = Directory.Exists(Path.Combine(root, "recette-artifacts"))
            ? Directory.GetFiles(Path.Combine(root, "recette-artifacts"), "MVT*.xls")
            : [];
        if (files.Length == 0)
        {
            return;
        }

        var reader = new MouvementsStockX3ExcelReader();
        foreach (var path in files)
        {
            var bytes = await File.ReadAllBytesAsync(path);
            var result = await reader.ReadAsync(new X3ImportFileContent(Path.GetFileName(path), bytes.Length, bytes), CancellationToken.None);
            Assert.NotEmpty(result.Rows);
        }
    }
}
