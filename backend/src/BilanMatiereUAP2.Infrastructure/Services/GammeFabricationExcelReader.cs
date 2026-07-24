using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Application.Common;
using ExcelDataReader;

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class GammeFabricationExcelReader : IGammeFabricationReader
{
    // Positional columns A..V => indices 0..21
    private const int ColCodeArticle = 0; // A
    private const int ColAlternativeGamme = 1; // B
    private const int ColNumeroOperation = 2; // C
    private const int ColCodePoste = 3; // D
    private const int ColCodeAtelier = 4; // E

    public Task<IEnumerable<RefGammeFabrication>> ReadAsync(Stream excelStream, string sourceSystem, Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var content = ReadAllBytes(excelStream);
        var file = new X3ImportFileContent(FileName: "upload.xlsx", Length: content.Length, Content: content);
        IReadOnlyList<ExcelWorkbook.Sheet> sheets;
        try
        {
            sheets = ExcelWorkbook.Read(file, cancellationToken);
        }
        catch (ImportFileException)
        {
            // If the uploaded content is not a readable Excel file, return empty result instead of throwing
            return Task.FromResult<IEnumerable<RefGammeFabrication>>(Array.Empty<RefGammeFabrication>());
        }
        var result = new List<RefGammeFabrication>();

        foreach (var sheet in sheets)
        {
            for (var i = 0; i < sheet.Rows.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = sheet.Rows[i];
                if (!ExcelWorkbook.HasData(row)) continue;

                var codeArticle = ExcelWorkbook.Text(GetAt(row, ColCodeArticle));
                var numeroOperation = ExcelWorkbook.Text(GetAt(row, ColNumeroOperation));
                var codeAtelier = ExcelWorkbook.Text(GetAt(row, ColCodeAtelier));

                // minimal validation
                if (string.IsNullOrWhiteSpace(codeArticle) || string.IsNullOrWhiteSpace(numeroOperation) || string.IsNullOrWhiteSpace(codeAtelier))
                {
                    // produce anomaly by skipping here; import process will record anomalies
                    continue;
                }

                var alternative = ExcelWorkbook.Text(GetAt(row, ColAlternativeGamme));
                var codePoste = ExcelWorkbook.Text(GetAt(row, ColCodePoste));

                // normalize
                codeArticle = Normalize(codeArticle);
                numeroOperation = Normalize(numeroOperation);
                codeAtelier = Normalize(codeAtelier);
                alternative = NormalizeOptional(alternative);
                codePoste = NormalizeOptional(codePoste);

                var cle = RefGammeFabrication.GenererCleMetier(codeArticle, alternative, numeroOperation, codePoste);
                var entity = RefGammeFabrication.Creer(codeArticle, alternative, numeroOperation, codePoste, sourceSystem, importBatchId, DateTime.UtcNow);
                result.Add(entity);
            }
        }

        return Task.FromResult<IEnumerable<RefGammeFabrication>>(result);
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
