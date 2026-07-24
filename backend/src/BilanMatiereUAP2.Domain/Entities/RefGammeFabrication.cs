using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class RefGammeFabrication
{
    private RefGammeFabrication() { }

    private RefGammeFabrication(Guid id,
        string codeArticle,
        string? alternativeGamme,
        string numeroOperation,
        string codePoste,
        string cleMetier,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
    {
        Id = id;
        CodeArticle = NormaliserTexte(codeArticle, 100)!;
        AlternativeGamme = NormaliserTexte(alternativeGamme, 50);
        NumeroOperation = NormaliserTexte(numeroOperation, 50)!;
        CodePoste = NormaliserTexte(codePoste, 50)!;
        CleMetier = cleMetier;
        SourceSystem = sourceSystem;
        ImportBatchId = importBatchId;
        ImportedAt = importedAt;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string CodeArticle { get; private set; } = string.Empty;
    public string? AlternativeGamme { get; private set; }
    public string NumeroOperation { get; private set; } = string.Empty;
    public string CodePoste { get; private set; } = string.Empty;
    public string CleMetier { get; private set; } = string.Empty;
    public string SourceSystem { get; private set; } = string.Empty;
    public Guid ImportBatchId { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public DateTime? SourceUpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static RefGammeFabrication Creer(string codeArticle, string? alternativeGamme, string numeroOperation, string codePoste, string sourceSystem, Guid importBatchId, DateTime importedAt)
    {
        var cle = GenererCleMetier(codeArticle, alternativeGamme, numeroOperation, codePoste);
        return new RefGammeFabrication(Guid.NewGuid(), codeArticle, alternativeGamme, numeroOperation, codePoste, cle, sourceSystem, importBatchId, importedAt);
    }

    public void MettreAJour(string? alternativeGamme, string numeroOperation, string codePoste, DateTime? sourceUpdatedAt)
    {
        AlternativeGamme = NormaliserTexte(alternativeGamme, 50);
        NumeroOperation = NormaliserTexte(numeroOperation, 50)!;
        CodePoste = NormaliserTexte(codePoste, 50)!;
        SourceUpdatedAt = sourceUpdatedAt;
        CleMetier = GenererCleMetier(CodeArticle, AlternativeGamme, NumeroOperation, CodePoste);
    }

    public void Desactiver() => IsActive = false;
    public void Reactiver() => IsActive = true;

    public static string GenererCleMetier(string codeArticle, string? alternativeGamme, string numeroOperation, string codePoste)
    {
        var parts = new[] { codeArticle?.Trim() ?? string.Empty, alternativeGamme?.Trim() ?? string.Empty, numeroOperation?.Trim() ?? string.Empty, codePoste?.Trim() ?? string.Empty };
        return string.Join("|", parts).ToUpperInvariant();
    }

    internal static string? NormaliserTexte(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength) throw new DomainValidationException($"La valeur ne peut pas depasser {maxLength} caracteres.");
        return normalized;
    }
}
