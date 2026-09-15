using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Services;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class ParametreApplication
{
    private ParametreApplication()
    {
    }

    private ParametreApplication(string cle, string valeur, string description)
    {
        Cle = NormaliserCleObligatoire(cle);
        Valeur = NormaliserTexteObligatoire(valeur, 500);
        Description = NormaliserTexteObligatoire(description, 500);
    }

    public string Cle { get; private set; } = string.Empty;
    public string Valeur { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public static ParametreApplication Creer(string cle, string valeur, string description)
        => new(cle, valeur, description);

    public void ModifierValeur(string valeur)
    {
        Valeur = NormaliserTexteObligatoire(valeur, 500);
    }

    private static string NormaliserCleObligatoire(string? value)
    {
        var normalized = BilanMetierRules.NormaliserCle(value);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new DomainValidationException("La cle du parametre application est obligatoire.");
        }

        if (normalized.Length > 100)
        {
            throw new DomainValidationException("La cle du parametre application ne peut pas depasser 100 caracteres.");
        }

        return normalized;
    }

    private static string NormaliserTexteObligatoire(string? value, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(normalized))
        {
            throw new DomainValidationException("La valeur du parametre application est obligatoire.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainValidationException($"La valeur ne peut pas depasser {maxLength} caracteres.");
        }

        return normalized;
    }
}

public static class ParametresApplicationCles
{
    public const string SeuilEcartFonderie = "SEUIL_ECART_FONDERIE";
    public const string SeuilEcartUap2 = "SEUIL_ECART_UAP2";
    public const string CodeAtelierUap2 = "CODE_ATELIER_UAP2";
    public const string TransactionEntreeOf = "TRANSACTION_ENTREE_OF";
    public const string TypesMatiere = "TYPES_MATIERE";
}
