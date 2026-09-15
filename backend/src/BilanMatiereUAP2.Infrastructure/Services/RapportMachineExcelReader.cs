using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class RapportMachineExcelReader : IReportMachineReader
{
    private static readonly string[] DetectionHeaders = ["date", "poste", "ope", "article", "numeroof", "qtetot"];

    public Task<X3ReadResult<RapportMachineRow>> ReadAsync(
        X3ImportFileContent file,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<RapportMachineRow>();
        var errors = new List<X3ReadError>();
        var totalRows = 0;
        IReadOnlyDictionary<string, int>? continuationColumns = null;

        foreach (var sheet in ExcelWorkbook.Read(file, cancellationToken))
        {
            var headerIndex = ExcelWorkbook.DetecterLigneEntete(sheet.Rows, DetectionHeaders, 4);
            IReadOnlyDictionary<string, int> columns;
            var dataStart = 0;
            if (headerIndex >= 0)
            {
                columns = ExcelWorkbook.CreateHeaderMap(sheet.Rows[headerIndex]);
                continuationColumns = columns;
                dataStart = headerIndex + 1;
            }
            else if (continuationColumns is not null)
            {
                columns = continuationColumns;
            }
            else
            {
                columns = CreatePositionalColumns();
            }

            for (var index = dataStart; index < sheet.Rows.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = sheet.Rows[index];
                if (!ExcelWorkbook.HasData(source)) continue;

                totalRows++;
                var line = index + 1;
                var dateValue = ExcelWorkbook.Get(source, columns, "date");
                var codePoste = Normalize(ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "poste")));
                var numeroOperation = Normalize(ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "ope")));
                var codeArticle = Normalize(ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "article")));
                var numeroOF = X3ImportParsing.NormaliserNumeroOF(ExcelWorkbook.Get(source, columns, "numeroof"));

                if (string.IsNullOrWhiteSpace(numeroOF) || !X3ImportParsing.EstNumeroOF(numeroOF) ||
                    string.IsNullOrWhiteSpace(codeArticle) || string.IsNullOrWhiteSpace(codePoste) ||
                    string.IsNullOrWhiteSpace(numeroOperation))
                {
                    continue;
                }

                if (!X3ImportParsing.TryParseDate(dateValue, out var dateProduction))
                {
                    errors.Add(new X3ReadError(line, StatutRapprochementX3.MouvementInvalide, "La date de production est invalide.", numeroOF, NomFeuilleSource: sheet.Name));
                    continue;
                }

                if (!TryReadRequiredDecimal(ExcelWorkbook.Get(source, columns, "qtetot"), out var quantiteTotale) ||
                    !TryReadRequiredDecimal(ExcelWorkbook.Get(source, columns, "qter"), out var quantiteRebut) ||
                    quantiteTotale < 0 || quantiteRebut < 0)
                {
                    errors.Add(new X3ReadError(line, StatutRapprochementX3.ValeurInvalide, "Une quantité totale ou rebut est invalide ou négative.", numeroOF, NomFeuilleSource: sheet.Name));
                    continue;
                }

                rows.Add(new RapportMachineRow(
                    line,
                    dateProduction,
                    codePoste,
                    numeroOperation,
                    codeArticle,
                    NormalizeOptional(ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "designation"))),
                    numeroOF,
                    NormalizeOptional(ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "equipe"))),
                    quantiteTotale,
                    quantiteRebut,
                    NormalizeOptional(ExcelWorkbook.Text(ExcelWorkbook.Get(source, columns, "altgm"))),
                    sheet.Name));
            }
        }

        return Task.FromResult(new X3ReadResult<RapportMachineRow>(rows, errors, totalRows));
    }

    private static object? GetAt(object?[] row, int index) => index < row.Length ? row[index] : null;

    private static Dictionary<string, int> CreatePositionalColumns()
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["date"] = 0,
            ["poste"] = 1,
            ["ope"] = 2,
            ["article"] = 3,
            ["designation"] = 4,
            ["numeroof"] = 5,
            ["equipe"] = 6,
            ["qtetot"] = 7,
            ["qter"] = 8,
            ["altgm"] = 9
        };

    private static string Normalize(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static bool TryReadRequiredDecimal(object? value, out decimal result)
        => X3ImportParsing.TryParseDecimal(value, out result);
}
