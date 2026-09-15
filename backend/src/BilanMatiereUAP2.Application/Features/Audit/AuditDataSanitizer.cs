using System.Collections;
using System.Text.Json;

namespace BilanMatiereUAP2.Application.Features.Audit;

public sealed class AuditDataSanitizer : IAuditDataSanitizer
{
    private static readonly string[] SensitiveFragments =
    [
        "password", "passwordhash", "secret", "clientsecret", "accesstoken",
        "refreshtoken", "idtoken", "authorization", "cookie", "connectionstring", "apikey",
        "privatekey", "certificatepassword"
    ];

    public IReadOnlyDictionary<string, object?> Sanitize(IReadOnlyDictionary<string, object?> values)
        => values
            .Where(pair => !IsSensitive(pair.Key))
            .ToDictionary(pair => pair.Key, pair => SanitizeValue(pair.Value), StringComparer.Ordinal);

    private static bool IsSensitive(string key)
    {
        var normalized = new string(key.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return SensitiveFragments.Any(normalized.Contains);
    }

    private object? SanitizeValue(object? value)
    {
        if (value is null) return null;
        if (value is JsonElement json) return SanitizeJson(json);
        if (value is IReadOnlyDictionary<string, object?> readOnly) return Sanitize(readOnly);
        if (value is IDictionary dictionary)
        {
            var result = new Dictionary<string, object?>();
            foreach (DictionaryEntry item in dictionary)
            {
                var key = item.Key?.ToString();
                if (!string.IsNullOrWhiteSpace(key) && !IsSensitive(key))
                    result[key] = SanitizeValue(item.Value);
            }
            return result;
        }
        if (value is IEnumerable enumerable and not string)
            return enumerable.Cast<object?>().Select(SanitizeValue).ToArray();
        if (value is DateTime dateTime) return dateTime.ToUniversalTime().ToString("O");
        if (value is DateOnly dateOnly) return dateOnly.ToString("yyyy-MM-dd");
        if (value is Guid guid) return guid.ToString();
        if (value is Enum enumeration) return enumeration.ToString();
        if (value is string or bool or byte or short or int or long or float or double or decimal)
            return value;
        return "[NON_SERIALISE]";
    }

    private object? SanitizeJson(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .Where(property => !IsSensitive(property.Name))
                .ToDictionary(property => property.Name, property => SanitizeJson(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(SanitizeJson).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetDecimal(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
}
