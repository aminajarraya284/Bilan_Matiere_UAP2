using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class RapportMachineExcelReader : IReportMachineReader
{
    public Task<X3ReadResult<RapportMachineRow>> ReadAsync(
        X3ImportFileContent file,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<RapportMachineRow>();
        var errors = new List<X3ReadError>();
        var totalRows = 0;

        foreach (var sheet in ExcelWorkbook.Read(file, cancellationToken))
        {
            for (var index = 0; index < sheet.Rows.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = sheet.Rows[index];
                if (!ExcelWorkbook.HasData(source)) continue;

                totalRows++;
                var line = index + 1;
                var dateValue = ExcelWorkbook.Text(GetAt(source, 0));
                var codePoste = Normalize(ExcelWorkbook.Text(GetAt(source, 1)));
                var numeroOperation = Normalize(ExcelWorkbook.Text(GetAt(source, 2)));
                var codeArticle = Normalize(ExcelWorkbook.Text(GetAt(source, 3)));
                var numeroOF = X3ImportParsing.NormaliserNumeroOF(GetAt(source, 5));

                if (string.IsNullOrWhiteSpace(numeroOF) || !X3ImportParsing.EstNumeroOF(numeroOF) ||
                    string.IsNullOrWhiteSpace(codeArticle) || string.IsNullOrWhiteSpace(codePoste) ||
                    string.IsNullOrWhiteSpace(numeroOperation))
                {
                    continue;
                }

                if (!X3ImportParsing.TryParseDate(GetAt(source, 0), out var dateProduction))
                {
                    errors.Add(new X3ReadError(line, StatutRapprochementX3.MouvementInvalide, "La date de production est invalide.", numeroOF, NomFeuilleSource: sheet.Name));
                    continue;
                }

                if (!TryReadRequiredDecimal(GetAt(source, 7), out var quantiteCTRL) ||
                    !TryReadRequiredDecimal(GetAt(source, 8), out var quantiteRebut) ||
                    quantiteCTRL < 0 || quantiteRebut < 0)
                {
                    errors.Add(new X3ReadError(line, StatutRapprochementX3.ValeurInvalide, "Une quantité CTRL ou rebut est invalide ou négative.", numeroOF, NomFeuilleSource: sheet.Name));
                    continue;
                }

                rows.Add(new RapportMachineRow(
                    line,
                    dateProduction,
                    codePoste,
                    numeroOperation,
                    codeArticle,
                    NormalizeOptional(ExcelWorkbook.Text(GetAt(source, 4))),
                    numeroOF,
                    NormalizeOptional(ExcelWorkbook.Text(GetAt(source, 6))),
                    quantiteCTRL,
                    quantiteRebut,
                    NormalizeOptional(ExcelWorkbook.Text(GetAt(source, 9))),
                    sheet.Name));
            }
        }

        return Task.FromResult(new X3ReadResult<RapportMachineRow>(rows, errors, totalRows));
    }

    private static object? GetAt(object?[] row, int index) => index < row.Length ? row[index] : null;

    private static string Normalize(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static bool TryReadRequiredDecimal(object? value, out decimal result)
        => X3ImportParsing.TryParseDecimal(value, out result);
}
