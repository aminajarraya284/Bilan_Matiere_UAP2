using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using ExcelDataReader;

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class NomenclatureExcelReader : INomenclatureReader
{
    // Positional columns A..L => indices 0..11
    private const int ColCodeArticleParent = 0; // A
    private const int ColAlternativeNomenclature = 1; // B
    private const int ColCodeParentNiveau = 2; // C
    private const int ColCodeComposant = 3; // D
    private const int ColNiveau = 4; // E
    private const int ColSequence = 5; // F

    public Task<IEnumerable<RefNomenclature>> ReadAsync(Stream excelStream, string sourceSystem, Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var content = ReadAllBytes(excelStream);
        var file = new X3ImportFileContent(FileName: "upload.xlsx", Length: content.Length, Content: content);
        var sheets = ExcelWorkbook.Read(file, cancellationToken);
        var result = new List<RefNomenclature>();

        foreach (var sheet in sheets)
        {
            string? currentParentArticle = null;
            string? currentDesignationParent = null;
            string? currentAlternative = null;
            string? currentUniteGestion = null;

            for (var i = 0; i < sheet.Rows.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = sheet.Rows[i];
                if (!ExcelWorkbook.HasData(row)) continue;

                var codeArticleParent = ExcelWorkbook.Text(GetAt(row, ColCodeArticleParent));
                var alternative = ExcelWorkbook.Text(GetAt(row, ColAlternativeNomenclature));
                var codeParentNiveau = ExcelWorkbook.Text(GetAt(row, ColCodeParentNiveau));
                var codeComposant = ExcelWorkbook.Text(GetAt(row, ColCodeComposant));
                var niveau = ExcelWorkbook.Text(GetAt(row, ColNiveau));
                var sequence = ExcelWorkbook.Text(GetAt(row, ColSequence));

                // Propagate parent info when cells are empty
                if (!string.IsNullOrWhiteSpace(codeArticleParent))
                {
                    currentParentArticle = Normalize(codeArticleParent);
                    currentAlternative = NormalizeOptional(alternative);
                }

                if (string.IsNullOrWhiteSpace(codeComposant))
                {
                    // skip titles or technical lines
                    continue;
                }

                var finalParent = currentParentArticle;
                if (string.IsNullOrWhiteSpace(finalParent))
                {
                    // cannot resolve parent, skip
                    continue;
                }

                // normalize
                var codeParentNiveauNorm = Normalize(codeParentNiveau);
                var codeComposantNorm = Normalize(codeComposant);
                var niveauNorm = Normalize(niveau);
                var sequenceNorm = Normalize(sequence);

                var cle = RefNomenclature.GenererCleMetier(finalParent!, currentAlternative, codeParentNiveauNorm, codeComposantNorm, niveauNorm, sequenceNorm);
                var entity = RefNomenclature.Creer(finalParent!, currentAlternative, codeParentNiveauNorm, codeComposantNorm, niveauNorm, sequenceNorm, sourceSystem, importBatchId, DateTime.UtcNow);
                result.Add(entity);
            }
        }

        return Task.FromResult<IEnumerable<RefNomenclature>>(result);
    }

    private static object? GetAt(object?[] row, int index) => index < row.Length ? row[index] : null;

    private static string Normalize(string value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? NormalizeOptional(string? optional) => string.IsNullOrWhiteSpace(optional) ? null : optional.Trim().ToUpperInvariant();

    private static byte[] ReadAllBytes(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
