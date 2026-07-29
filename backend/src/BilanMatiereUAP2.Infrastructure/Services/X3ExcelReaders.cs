using System.Text;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Domain.Enums;
using ExcelDataReader;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BilanMatiereUAP2.Api.IntegrationTests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BilanMatiereUAP2.UnitTests")]

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class EncoursProductExcelReader : IEncoursProductReader
{
    private static readonly string[] RequiredHeaders = ["nordre", "article", "itmdes10"];

    public Task<X3ReadResult<EncoursProductRow>> ReadAsync(
        X3ImportFileContent file,
        CancellationToken cancellationToken)
    {
        var rows = new List<EncoursProductRow>();
        var errors = new List<X3ReadError>();
        var totalRows = 0;
        var sheets = ExcelWorkbook.Read(file, cancellationToken);
        var readableSheetFound = false;

        foreach (var sheet in sheets)
        {
            var headerIndex = ExcelWorkbook.FindHeaderRow(sheet.Rows, RequiredHeaders);
            if (headerIndex < 0)
            {
                if (sheet.Rows.Any(ExcelWorkbook.HasData))
                {
                    errors.Add(new X3ReadError(0, StatutRapprochementX3.MouvementInvalide,
                        $"La feuille '{sheet.Name}' ne contient pas les colonnes EncoursProduct obligatoires.",
                        NomFeuilleSource: sheet.Name));
                }

                continue;
            }

            readableSheetFound = true;
            var columns = ExcelWorkbook.CreateHeaderMap(sheet.Rows[headerIndex]);
            for (var index = headerIndex + 1; index < sheet.Rows.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = sheet.Rows[index];
                if (!ExcelWorkbook.HasData(source))
                {
                    continue;
                }

                totalRows++;
                var line = index + 1;
                var numeroOf = X3ImportParsing.NormaliserNumeroOF(ExcelWorkbook.Get(source, columns, "nordre"));
                if (!X3ImportParsing.EstNumeroOF(numeroOf))
                {
                    // Les SG et autres références ne sont pas des OF, donc ne participent pas à l'import métier.
                    continue;
                }

                var article = ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "article"));
                var designation = ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "itmdes10"));
                if (string.IsNullOrWhiteSpace(article) || string.IsNullOrWhiteSpace(designation))
                {
                    errors.Add(new X3ReadError(line, StatutRapprochementX3.ArticlePrincipalIntrouvable,
                        "L'article ou la désignation principale de l'OF est absent.", numeroOf,
                        NomFeuilleSource: sheet.Name));
                    continue;
                }

                var dateOperation = ReadOptionalDate(source, columns, "dopd", line, numeroOf, sheet.Name, errors);
                var dateObjectif = ReadOptionalDate(source, columns, "dateobjof", line, numeroOf, sheet.Name, errors);
                rows.Add(new EncoursProductRow(
                    line,
                    numeroOf,
                    article,
                    designation,
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "situationof")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "atl")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "nop")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "postp")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "typp")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "np")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "effp")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "cad")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "efop")),
                    ExcelWorkbook.ReadOptionalDecimal(source, columns, "qteprv"),
                    ExcelWorkbook.ReadOptionalDecimal(source, columns, "qterest"),
                    dateOperation,
                    dateObjectif,
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "statut")),
                    sheet.Name));
            }
        }

        if (!readableSheetFound)
        {
            throw new ImportFileException("Aucune feuille EncoursProduct exploitable n'a été trouvée.");
        }

        return Task.FromResult(new X3ReadResult<EncoursProductRow>(rows, errors, totalRows));
    }

    private static DateOnly? ReadOptionalDate(
        object?[] row,
        IReadOnlyDictionary<string, int> columns,
        string header,
        int line,
        string numeroOf,
        string sheet,
        ICollection<X3ReadError> errors)
    {
        var value = ExcelWorkbook.Get(row, columns, header);
        if (string.IsNullOrWhiteSpace(ExcelWorkbook.Text(value)))
        {
            return null;
        }

        if (X3ImportParsing.TryParseDate(value, out var date))
        {
            return date;
        }

        errors.Add(new X3ReadError(line, StatutRapprochementX3.MouvementInvalide,
            $"La date '{header}' est invalide.", numeroOf, NomFeuilleSource: sheet));
        return null;
    }
}

public sealed class MouvementsStockX3ExcelReader : IMouvementStockX3Reader
{
    private static readonly string[] RequiredHeaders =
    [
        "dateimput", "code", "designation", "quantite", "unite", "transaction", "typepce", "pieceorigine"
    ];

    public Task<X3ReadResult<MouvementStockX3Row>> ReadAsync(
        X3ImportFileContent file,
        CancellationToken cancellationToken)
    {
        var rows = new List<MouvementStockX3Row>();
        var errors = new List<X3ReadError>();
        var totalRows = 0;
        IReadOnlyDictionary<string, int>? continuationColumns = null;
        var readableSheetFound = false;

        foreach (var sheet in ExcelWorkbook.Read(file, cancellationToken))
        {
            var headerIndex = ExcelWorkbook.FindHeaderRow(sheet.Rows, RequiredHeaders);
            IReadOnlyDictionary<string, int>? columns;
            var dataStart = 0;
            if (headerIndex >= 0)
            {
                columns = ExcelWorkbook.CreateHeaderMap(sheet.Rows[headerIndex]);
                continuationColumns = columns;
                dataStart = headerIndex + 1;
                readableSheetFound = true;
            }
            else if (continuationColumns is not null && sheet.Rows.Any(ExcelWorkbook.HasData))
            {
                // MVTSTOTRSVCR continue sur une seconde feuille sans en-tête.
                columns = continuationColumns;
                dataStart = 0;
            }
            else
            {
                if (sheet.Rows.Any(ExcelWorkbook.HasData))
                {
                    errors.Add(new X3ReadError(0, StatutRapprochementX3.MouvementInvalide,
                        $"La feuille '{sheet.Name}' ne possède pas d'en-tête MVTSTOTRSVCR exploitable.",
                        NomFeuilleSource: sheet.Name));
                }

                continue;
            }

            for (var index = dataStart; index < sheet.Rows.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = sheet.Rows[index];
                if (!ExcelWorkbook.HasData(source))
                {
                    continue;
                }

                totalRows++;
                var line = index + 1;
                var origin = ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "pieceorigine"));
                var normalizedOf = X3ImportParsing.NormaliserNumeroOF(origin);
                var dateValue = ExcelWorkbook.Get(source, columns, "dateimput");
                if (!X3ImportParsing.TryParseDate(dateValue, out var dateImputation))
                {
                    errors.Add(new X3ReadError(line, StatutRapprochementX3.MouvementInvalide,
                        "La date d'imputation est invalide.", normalizedOf, NomFeuilleSource: sheet.Name));
                    continue;
                }

                var quantityValue = ExcelWorkbook.Get(source, columns, "quantite");
                if (!X3ImportParsing.TryParseDecimal(quantityValue, out var quantity))
                {
                    errors.Add(new X3ReadError(line, StatutRapprochementX3.MouvementInvalide,
                        "La quantité X3 est invalide.", normalizedOf, NomFeuilleSource: sheet.Name));
                    continue;
                }

                var creationValue = ExcelWorkbook.Get(source, columns, "datecrea");
                DateOnly? creationDate = null;
                if (!string.IsNullOrWhiteSpace(ExcelWorkbook.Text(creationValue)) &&
                    X3ImportParsing.TryParseDate(creationValue, out var parsedCreationDate))
                {
                    creationDate = parsedCreationDate;
                }

                rows.Add(new MouvementStockX3Row(
                    line,
                    dateImputation,
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "code")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "designation")),
                    creationDate,
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "heurecrea")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "utilisateur")),
                    quantity,
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "unite")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "transaction")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "typepce")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "lot")),
                    origin,
                    normalizedOf,
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "designationmouvement")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "emplacement")),
                    ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "statut")),
                    sheet.Name));
            }
        }

        if (!readableSheetFound)
        {
            throw new ImportFileException("Aucune feuille MVTSTOTRSVCR exploitable n'a été trouvée.");
        }

        return Task.FromResult(new X3ReadResult<MouvementStockX3Row>(rows, errors, totalRows));
    }
}

internal static class ExcelWorkbook
{
    private static readonly object EncodingLock = new();
    private static bool encodingRegistered;

    internal sealed record Sheet(string Name, IReadOnlyList<object?[]> Rows);

    public static IReadOnlyList<Sheet> Read(X3ImportFileContent file, CancellationToken cancellationToken)
    {
        try
        {
            EnsureEncodingProvider();
            using var stream = new MemoryStream(file.Content, writable: false);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var sheets = new List<Sheet>();
            do
            {
                var rows = new List<object?[]>();
                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var row = new object?[reader.FieldCount];
                    for (var column = 0; column < reader.FieldCount; column++)
                    {
                        row[column] = reader.GetValue(column);
                    }

                    rows.Add(row);
                }

                sheets.Add(new Sheet(reader.Name ?? $"Feuille{reader.ResultsCount}", rows));
            }
            while (reader.NextResult());

            return sheets;
        }
        catch (ImportFileException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or ExcelDataReader.Exceptions.HeaderException)
        {
            throw new ImportFileException("Le classeur Excel est illisible.");
        }
    }

    public static int FindHeaderRow(IReadOnlyList<object?[]> rows, IReadOnlyCollection<string> requiredHeaders)
    {
        for (var index = 0; index < Math.Min(rows.Count, 30); index++)
        {
            var headers = CreateHeaderMap(rows[index]);
            if (requiredHeaders.All(headers.ContainsKey))
            {
                return index;
            }
        }

        return -1;
    }

    public static Dictionary<string, int> CreateHeaderMap(object?[] row)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < row.Length; index++)
        {
            var header = X3ImportParsing.NormaliserEntete(row[index]);
            if (!string.IsNullOrWhiteSpace(header) && !result.ContainsKey(header))
            {
                result.Add(header, index);
            }
        }

        return result;
    }

    public static bool HasData(object?[] row) => row.Any(value => !string.IsNullOrWhiteSpace(Text(value)));

    public static object? Get(object?[] row, IReadOnlyDictionary<string, int> columns, string header)
    {
        return columns.TryGetValue(header, out var index) && index < row.Length ? row[index] : null;
    }

    public static string? Text(object? value)
    {
        var text = Convert.ToString(value)?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public static decimal? ReadOptionalDecimal(object?[] row, IReadOnlyDictionary<string, int> columns, string header)
    {
        var value = Get(row, columns, header);
        return string.IsNullOrWhiteSpace(Text(value))
            ? null
            : X3ImportParsing.TryParseDecimal(value, out var decimalValue) ? decimalValue : null;
    }

    private static void EnsureEncodingProvider()
    {
        lock (EncodingLock)
        {
            if (encodingRegistered)
            {
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            encodingRegistered = true;
        }
    }
}
