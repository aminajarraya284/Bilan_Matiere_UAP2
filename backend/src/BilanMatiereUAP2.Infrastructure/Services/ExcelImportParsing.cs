using System.Globalization;
using System.Text;

namespace BilanMatiereUAP2.Infrastructure.Services;

public static class ExcelImportParsing
{
    public static string NormaliserEntete(object? value)
    {
        var source = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        var decomposed = source.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string NormaliserEnteteColonne(object? value)
    {
        var normalized = NormaliserEntete(value);
        return normalized switch
        {
            "pieceorigin" => "pieceorigine",
            _ => normalized
        };
    }
}
