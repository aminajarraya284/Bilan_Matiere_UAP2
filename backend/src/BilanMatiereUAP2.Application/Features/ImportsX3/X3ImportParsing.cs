using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BilanMatiereUAP2.Application.Features.ImportsX3;

public static class X3ImportParsing
{
    private static readonly CultureInfo FrenchCulture = CultureInfo.GetCultureInfo("fr-FR");

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

    public static string NormaliserNumeroOF(object? value)
    {
        var raw = Convert.ToString(value, CultureInfo.InvariantCulture)?
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace("\u202F", string.Empty) ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        return raw;
    }

    public static bool EstNumeroOF(object? value)
    {
        return NormaliserNumeroOF(value).StartsWith("OF", StringComparison.Ordinal);
    }

    public static string NormaliserCode(object? value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public static string NormaliserValeurMetier(object? value)
    {
        var normalized = NormaliserEntete(value);
        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static bool TryParseDecimal(object? value, out decimal result)
    {
        if (value is decimal decimalValue)
        {
            result = decimalValue;
            return true;
        }

        if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double)
        {
            try
            {
                result = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch (OverflowException)
            {
                result = 0;
                return false;
            }
        }

        var raw = Convert.ToString(value, FrenchCulture)?.Trim() ?? string.Empty;
        raw = raw
            .Replace("\u00A0", string.Empty)
            .Replace("\u202F", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("%", string.Empty)
            .Replace("kg", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign, FrenchCulture, out result))
        {
            return true;
        }

        return decimal.TryParse(
            raw.Replace(',', '.'),
            NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out result);
    }

    public static bool TryParseDate(object? value, out DateOnly result)
    {
        if (value is DateOnly dateOnly)
        {
            result = dateOnly;
            return true;
        }

        if (value is DateTime dateTime)
        {
            result = DateOnly.FromDateTime(dateTime);
            return true;
        }

        if (value is double serial && serial > 0)
        {
            try
            {
                result = DateOnly.FromDateTime(DateTime.FromOADate(serial));
                return true;
            }
            catch (ArgumentException)
            {
                result = default;
                return false;
            }
        }

        var raw = Convert.ToString(value, FrenchCulture)?.Trim() ?? string.Empty;
        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd/MM/yy" };
        if (DateOnly.TryParseExact(raw, formats, FrenchCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        return DateOnly.TryParse(raw, FrenchCulture, DateTimeStyles.None, out result);
    }

    public static string CalculerSha256(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content));
    }

    public static string CalculerHashCouple(string encoursHash, string mouvementsHash)
    {
        return CalculerSha256(Encoding.UTF8.GetBytes($"{encoursHash}:{mouvementsHash}"));
    }
}
