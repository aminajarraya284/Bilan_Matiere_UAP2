using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class X3RapportMachine
{
    private X3RapportMachine() { }

    private X3RapportMachine(
        Guid id,
        DateOnly dateProduction,
        string codePoste,
        OrigineProcessus origineProcessus,
        string numeroOperation,
        string codeArticle,
        string? designationArticle,
        string numeroOF,
        string? equipe,
        decimal quantiteCTRL,
        decimal quantiteRebut,
        string? alternativeGamme,
        string cleMetier,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
    {
        Id = id;
        DateProduction = dateProduction;
        CodePoste = Normalize(codePoste, 50);
        OrigineProcessus = origineProcessus;
        NumeroOperation = Normalize(numeroOperation, 50);
        CodeArticle = Normalize(codeArticle, 100);
        DesignationArticle = NormalizeOptional(designationArticle, 250);
        NumeroOF = Normalize(numeroOF, 50);
        Equipe = NormalizeOptional(equipe, 50);
        QuantiteCTRL = quantiteCTRL;
        QuantiteRebut = quantiteRebut;
        AlternativeGamme = NormalizeOptional(alternativeGamme, 50);
        CleMetier = cleMetier;
        SourceSystem = Normalize(sourceSystem, 100);
        ImportBatchId = importBatchId;
        ImportedAt = importedAt;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public DateOnly DateProduction { get; private set; }
    public string CodePoste { get; private set; } = string.Empty;
    public OrigineProcessus OrigineProcessus { get; private set; }
    public string NumeroOperation { get; private set; } = string.Empty;
    public string CodeArticle { get; private set; } = string.Empty;
    public string? DesignationArticle { get; private set; }
    public string NumeroOF { get; private set; } = string.Empty;
    public string? Equipe { get; private set; }
    public decimal QuantiteCTRL { get; private set; }
    public decimal QuantiteRebut { get; private set; }
    public string? AlternativeGamme { get; private set; }
    public string CleMetier { get; private set; } = string.Empty;
    public string SourceSystem { get; private set; } = string.Empty;
    public Guid ImportBatchId { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public bool IsActive { get; private set; }

    // origineProcessus est resolue par l'appelant avec OrigineProcessusRules.
    public static X3RapportMachine Creer(
        DateOnly dateProduction,
        string codePoste,
        OrigineProcessus origineProcessus,
        string numeroOperation,
        string codeArticle,
        string? designationArticle,
        string numeroOF,
        string? equipe,
        decimal quantiteCTRL,
        decimal quantiteRebut,
        string? alternativeGamme,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
        => new(
            Guid.NewGuid(),
            dateProduction,
            codePoste,
            origineProcessus,
            numeroOperation,
            codeArticle,
            designationArticle,
            numeroOF,
            equipe,
            quantiteCTRL,
            quantiteRebut,
            alternativeGamme,
            GenererCleMetier(dateProduction, numeroOF, codeArticle, numeroOperation, codePoste, equipe, alternativeGamme),
            sourceSystem,
            importBatchId,
            importedAt);

    public void RecalculerOrigineProcessus(OrigineProcessus nouvelleOrigine)
    {
        OrigineProcessus = nouvelleOrigine;
    }

    public void MettreAJour(
        string? designationArticle,
        decimal quantiteCTRL,
        decimal quantiteRebut,
        string? alternativeGamme,
        string sourceSystem,
        OrigineProcessus origineProcessus,
        Guid importBatchId,
        DateTime importedAt)
    {
        DesignationArticle = NormalizeOptional(designationArticle, 250);
        QuantiteCTRL = quantiteCTRL;
        QuantiteRebut = quantiteRebut;
        AlternativeGamme = NormalizeOptional(alternativeGamme, 50);
        SourceSystem = Normalize(sourceSystem, 100);
        OrigineProcessus = origineProcessus;
        ImportBatchId = importBatchId;
        ImportedAt = importedAt;
        IsActive = true;
    }

    public bool EstEquivalent(
        string? designationArticle,
        decimal quantiteCTRL,
        decimal quantiteRebut,
        string? alternativeGamme,
        string sourceSystem,
        OrigineProcessus origineProcessus)
        => string.Equals(DesignationArticle, NormalizeOptional(designationArticle, 250), StringComparison.Ordinal)
           && QuantiteCTRL == quantiteCTRL
           && QuantiteRebut == quantiteRebut
           && string.Equals(AlternativeGamme, NormalizeOptional(alternativeGamme, 50), StringComparison.Ordinal)
           && string.Equals(SourceSystem, Normalize(sourceSystem, 100), StringComparison.Ordinal)
           && OrigineProcessus == origineProcessus;

    public void Desactiver() => IsActive = false;

    public static string GenererCleMetier(
        DateOnly dateProduction,
        string numeroOF,
        string codeArticle,
        string numeroOperation,
        string codePoste,
        string? equipe,
        string? alternativeGamme)
        => string.Join("|", new[]
        {
            dateProduction.ToString("yyyy-MM-dd"),
            numeroOF,
            codeArticle,
            numeroOperation,
            codePoste,
            equipe,
            alternativeGamme
        }.Select(value => value?.Trim().ToUpperInvariant() ?? string.Empty));

    private static string Normalize(string value, int maxLength)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
            throw new DomainValidationException("La valeur du rapport machine est invalide.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > maxLength) throw new DomainValidationException("La valeur du rapport machine est invalide.");
        return normalized;
    }
}
