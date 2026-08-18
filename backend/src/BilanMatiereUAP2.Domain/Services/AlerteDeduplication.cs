namespace BilanMatiereUAP2.Domain.Services;

public static class AlerteDeduplication
{
    private const string NullToken = "<NULL>";

    public static string Construire(
        string code,
        string? periode,
        string? numeroOf,
        string? codeArticle,
        string? typeMatiere,
        string source)
        => string.Join(
            '|',
            Normaliser(code),
            Normaliser(periode),
            Normaliser(numeroOf),
            Normaliser(codeArticle),
            Normaliser(typeMatiere),
            Normaliser(source));

    public static string Normaliser(string? value)
    {
        var normalisee = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalisee))
        {
            return NullToken;
        }

        // Encodage minimal et stable pour conserver un séparateur non ambigu.
        return normalisee
            .Replace("%", "%25", StringComparison.Ordinal)
            .Replace("|", "%7C", StringComparison.Ordinal);
    }
}
