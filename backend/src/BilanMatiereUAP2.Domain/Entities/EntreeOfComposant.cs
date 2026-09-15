using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

// Agrege les mouvements X3 "Entree OF" par NumeroOF + CodeArticle + Unite + DateImputation,
// symetrique a ConsommationComposant (Sortie OF). Le signe X3 est conserve tel quel (jamais
// de Math.Abs par ligne) : QuantiteNetteSignee peut etre positive ou negative selon les
// corrections/annulations presentes dans le fichier source.
public sealed class EntreeOfComposant
{
    private EntreeOfComposant() { }

    private EntreeOfComposant(
        Guid id,
        Guid importBatchId,
        DateOnly dateImputation,
        string numeroOF,
        string codeArticle,
        string? designationArticle,
        string unite,
        decimal quantiteNetteSignee,
        DateTime dateCreationUtc)
    {
        Id = id;
        ImportBatchId = importBatchId;
        DateImputation = dateImputation;
        NumeroOF = NormaliserCode(numeroOF, 50);
        CodeArticle = NormaliserCode(codeArticle, 100);
        DesignationArticle = NormaliserTexte(designationArticle, 250);
        Unite = NormaliserCode(unite, 20);
        CleMetier = GenererCleMetier(NumeroOF, CodeArticle, Unite, dateImputation);
        QuantiteNetteSignee = quantiteNetteSignee;
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public Guid ImportBatchId { get; private set; }
    public DateOnly DateImputation { get; private set; }
    public string NumeroOF { get; private set; } = string.Empty;
    public string CodeArticle { get; private set; } = string.Empty;
    public string? DesignationArticle { get; private set; }
    public string Unite { get; private set; } = string.Empty;
    public string CleMetier { get; private set; } = string.Empty;
    public decimal QuantiteNetteSignee { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateMiseAJourUtc { get; private set; }

    public static EntreeOfComposant Creer(
        Guid importBatchId,
        DateOnly dateImputation,
        string numeroOF,
        string codeArticle,
        string? designationArticle,
        string unite,
        decimal quantiteNetteSignee,
        DateTime dateCreationUtc,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), importBatchId, dateImputation, numeroOF, codeArticle, designationArticle, unite, quantiteNetteSignee, dateCreationUtc);

    public void MettreAJour(string? designationArticle, decimal quantiteNetteSignee, Guid importBatchId, DateTime dateMiseAJourUtc)
    {
        DesignationArticle = NormaliserTexte(designationArticle, 250);
        QuantiteNetteSignee = quantiteNetteSignee;
        ImportBatchId = importBatchId;
        DateMiseAJourUtc = dateMiseAJourUtc;
    }

    public bool EstEquivalent(string? designationArticle, decimal quantiteNetteSignee)
        => string.Equals(DesignationArticle, NormaliserTexte(designationArticle, 250), StringComparison.Ordinal)
           && QuantiteNetteSignee == quantiteNetteSignee;

    public static string GenererCleMetier(string numeroOF, string codeArticle, string unite, DateOnly dateImputation)
        => $"{numeroOF.ToUpperInvariant()}|{codeArticle.ToUpperInvariant()}|{unite.ToUpperInvariant()}|{dateImputation:yyyy-MM-dd}";

    private static string NormaliserCode(string value, int maxLength)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized)) throw new DomainValidationException("Une valeur obligatoire du mouvement Entree OF est absente.");
        if (normalized.Length > maxLength) throw new DomainValidationException($"La valeur ne peut pas dépasser {maxLength} caractères.");
        return normalized;
    }

    private static string? NormaliserTexte(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
