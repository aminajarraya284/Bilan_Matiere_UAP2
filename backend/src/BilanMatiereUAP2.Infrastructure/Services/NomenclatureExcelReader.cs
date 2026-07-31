using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class NomenclatureExcelReader : INomenclatureReader
{
    public Task<IEnumerable<RefNomenclature>> ReadAsync(
        Stream excelStream,
        string sourceSystem,
        Guid importBatchId,
        CancellationToken cancellationToken = default)
    {
        var content = ReadAllBytes(excelStream);
        var file = new X3ImportFileContent("BOMARTPARENT.xls", content.Length, content);
        var result = new List<RefNomenclature>();

        foreach (var sheet in ExcelWorkbook.Read(file, cancellationToken))
        {
            foreach (var row in sheet.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var nomenclature = TryParseRow(row, sourceSystem, importBatchId);
                if (nomenclature is not null) result.Add(nomenclature);
            }
        }

        return Task.FromResult<IEnumerable<RefNomenclature>>(result);
    }

    // Extrait pour etre testable directement avec un object?[] construit a la main,
    // sans dependre d'un fichier Excel binaire reel.
    internal static RefNomenclature? TryParseRow(object?[] row, string sourceSystem, Guid importBatchId)
    {
        if (!ExcelWorkbook.HasData(row)) return null;
        if (IsTitleOrHeaderRow(row)) return null;

        var codeParentNiveau = NormalizeCode(ExcelWorkbook.Text(GetAt(row, 0)));
        var codeComposant = NormalizeCode(ExcelWorkbook.Text(GetAt(row, 1)));
        var codeArticleParent = NormalizeCode(ExcelWorkbook.Text(GetAt(row, 2)));
        var niveau = NormalizeCode(ExcelWorkbook.Text(GetAt(row, 3)));
        var designationComposant = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 4)));
        var sequence = NormalizeCode(ExcelWorkbook.Text(GetAt(row, 5)));

        if (string.IsNullOrWhiteSpace(codeParentNiveau) ||
            string.IsNullOrWhiteSpace(codeComposant) ||
            string.IsNullOrWhiteSpace(codeArticleParent) ||
            string.IsNullOrWhiteSpace(niveau) ||
            string.IsNullOrWhiteSpace(sequence))
        {
            return null;
        }

        // Dans le rapport X3 reel, les lignes de niveau 1 (liens directs sous l'article racine)
        // laissent la colonne 6 vide (pas de QuantiteLien propre a ce niveau) : les champs
        // quantite/unite/categorie/type lien/%rebut sont alors decales d'une position (7 a 12).
        // Les niveaux 2+ remplissent la colonne 6 normalement (mapping "sur le papier", 6 a 11).
        // Detection ligne par ligne car ce decalage n'est pas constant sur tout le fichier.
        var col6IsBlank = string.IsNullOrWhiteSpace(ExcelWorkbook.Text(GetAt(row, 6)));
        var offset = col6IsBlank ? 7 : 6;

        var quantiteLien = ReadDecimal(GetAt(row, offset));
        var quantiteLienCumulee = ReadDecimal(GetAt(row, offset + 1));
        var uniteQuantite = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, offset + 2)));
        var categorieComposant = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, offset + 3)));
        var typeLien = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, offset + 4)));
        var pourcentageRebut = ReadDecimal(GetAt(row, offset + 5));

        return RefNomenclature.Creer(
            codeArticleParent,
            null,
            codeParentNiveau,
            codeComposant,
            niveau,
            sequence,
            designationComposant,
            quantiteLien,
            quantiteLienCumulee,
            uniteQuantite,
            categorieComposant,
            typeLien,
            pourcentageRebut,
            sourceSystem,
            importBatchId,
            DateTime.UtcNow);
    }

    // Le fichier reel repete un bloc "titre" (Nomenclature produit / Article parent :) puis une ligne
    // d'en-tete (Typ.Alt / Parent / Enfant / ...) pour chaque article racine. Les deux doivent etre ignores.
    private static bool IsTitleOrHeaderRow(object?[] row)
    {
        var col0 = ExcelImportParsing.NormaliserEntete(GetAt(row, 0));
        if (col0.Contains("nomenclature") || col0 == "articleparent" || col0 == "typalt")
        {
            return true;
        }

        var col1 = ExcelImportParsing.NormaliserEntete(GetAt(row, 1));
        var col2 = ExcelImportParsing.NormaliserEntete(GetAt(row, 2));
        return col1 == "parent" && col2 == "enfant";
    }

    private static object? GetAt(object?[] row, int index) => index < row.Length ? row[index] : null;

    private static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal? ReadDecimal(object? value)
        => string.IsNullOrWhiteSpace(ExcelWorkbook.Text(value))
            ? null
            : X3ImportParsing.TryParseDecimal(value, out var parsed) ? parsed : null;

    private static byte[] ReadAllBytes(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
