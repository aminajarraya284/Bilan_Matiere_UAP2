using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class GammeFabricationExcelReader : IGammeFabricationReader
{
    public Task<IEnumerable<RefGammeFabrication>> ReadAsync(
        Stream excelStream,
        string sourceSystem,
        Guid importBatchId,
        CancellationToken cancellationToken = default)
    {
        var content = ReadAllBytes(excelStream);
        var file = new X3ImportFileContent("GAMMEFAB.xls", content.Length, content);
        var result = new List<RefGammeFabrication>();
        IReadOnlyList<ExcelWorkbook.Sheet> sheets;
        try
        {
            sheets = ExcelWorkbook.Read(file, cancellationToken);
        }
        catch (ImportFileException)
        {
            return Task.FromResult<IEnumerable<RefGammeFabrication>>(result);
        }

        foreach (var sheet in sheets)
        {
            foreach (var row in sheet.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ExcelWorkbook.HasData(row)) continue;

                var codeArticle = NormalizeText(ExcelWorkbook.Text(GetAt(row, 0)));
                var numeroOperationRaw = ExcelWorkbook.Text(GetAt(row, 4));
                var numeroOperation = NormalizeText(numeroOperationRaw);

                // Les en-têtes GAMMEFAB sont des libellés multi-lignes qui ne correspondent pas
                // systematiquement au contenu reel (cf. spec) : on ne peut donc pas filtrer les
                // titres/en-tetes par comparaison de texte. Le NumeroOperation (colonne E) est en
                // revanche toujours numerique sur une vraie ligne metier ; on l'utilise comme
                // discriminant fiable, en plus du filtre "ligne vide" deja applique par HasData.
                if (string.IsNullOrWhiteSpace(codeArticle) ||
                    string.IsNullOrWhiteSpace(numeroOperation) ||
                    !X3ImportParsing.TryParseDecimal(numeroOperationRaw, out _))
                {
                    continue;
                }

                var designationArticle = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 1)));
                var dateFinValidite = ReadDate(GetAt(row, 2));
                var etapeSuivi = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 3)));
                var codeAtelier = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 5)));
                var codePoste = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 6)));
                var nombrePostes = ReadDecimal(GetAt(row, 7));
                var groupePosteCharge = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 8)));
                var designationOperation = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 9)));
                var alternativeGamme = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 10)));
                var uniteOperation = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 11)));
                var coefficientOperationStock = ReadDecimal(GetAt(row, 12));
                var tempsCycle = ReadDecimal(GetAt(row, 13));
                var trs = ReadDecimal(GetAt(row, 14));
                var tempsAttente = ReadDecimal(GetAt(row, 15));
                var jalon = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 16)));
                var quantite = ReadDecimal(GetAt(row, 17));
                var cadence = ReadDecimal(GetAt(row, 18));
                var dateModificationX3 = ReadDateTime(GetAt(row, 19));
                var tempsReglage = ReadDecimal(GetAt(row, 20));
                var coefficientCcpt = ReadDecimal(GetAt(row, 21));

                result.Add(RefGammeFabrication.Creer(
                    codeArticle,
                    designationArticle,
                    dateFinValidite,
                    etapeSuivi,
                    alternativeGamme,
                    numeroOperation,
                    codeAtelier,
                    codePoste,
                    nombrePostes,
                    groupePosteCharge,
                    designationOperation,
                    uniteOperation,
                    coefficientOperationStock,
                    tempsCycle,
                    trs,
                    tempsAttente,
                    jalon,
                    quantite,
                    cadence,
                    dateModificationX3,
                    tempsReglage,
                    coefficientCcpt,
                    sourceSystem,
                    importBatchId,
                    DateTime.UtcNow));
            }
        }

        return Task.FromResult<IEnumerable<RefGammeFabrication>>(result);
    }

    private static object? GetAt(object?[] row, int index) => index < row.Length ? row[index] : null;

    private static string NormalizeText(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal? ReadDecimal(object? value)
        => string.IsNullOrWhiteSpace(ExcelWorkbook.Text(value))
            ? null
            : X3ImportParsing.TryParseDecimal(value, out var parsed) ? parsed : null;

    private static DateOnly? ReadDate(object? value)
        => string.IsNullOrWhiteSpace(ExcelWorkbook.Text(value))
            ? null
            : X3ImportParsing.TryParseDate(value, out var parsed) ? parsed : null;

    private static DateTime? ReadDateTime(object? value)
    {
        if (value is DateTime dateTime) return dateTime;
        if (value is DateOnly dateOnly) return dateOnly.ToDateTime(TimeOnly.MinValue);
        return ReadDate(value)?.ToDateTime(TimeOnly.MinValue);
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
