using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class RefNomenclature
{
    private RefNomenclature() { }

    private RefNomenclature(Guid id,
        string codeArticleParent,
        string? alternativeNomenclature,
        string codeParentNiveau,
        string codeComposant,
        string niveau,
        string sequence,
        string cleMetier,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
    {
        Id = id;
        CodeArticleParent = NormaliserTexte(codeArticleParent, 100)!;
        AlternativeNomenclature = NormaliserTexte(alternativeNomenclature, 50);
        CodeParentNiveau = NormaliserTexte(codeParentNiveau, 50)!;
        CodeComposant = NormaliserTexte(codeComposant, 100)!;
        Niveau = NormaliserTexte(niveau, 50)!;
        Sequence = NormaliserTexte(sequence, 50)!;
        CleMetier = cleMetier;
        SourceSystem = sourceSystem;
        ImportBatchId = importBatchId;
        ImportedAt = importedAt;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string CodeArticleParent { get; private set; } = string.Empty;
    public string? AlternativeNomenclature { get; private set; }
    public string CodeParentNiveau { get; private set; } = string.Empty;
    public string CodeComposant { get; private set; } = string.Empty;
    public string Niveau { get; private set; } = string.Empty;
    public string Sequence { get; private set; } = string.Empty;
    public string CleMetier { get; private set; } = string.Empty;
    public string SourceSystem { get; private set; } = string.Empty;
    public Guid ImportBatchId { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public DateTime? SourceUpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static RefNomenclature Creer(string codeArticleParent, string? alternativeNomenclature, string codeParentNiveau, string codeComposant, string niveau, string sequence, string sourceSystem, Guid importBatchId, DateTime importedAt)
    {
        var cle = GenererCleMetier(codeArticleParent, alternativeNomenclature, codeParentNiveau, codeComposant, niveau, sequence);
        return new RefNomenclature(Guid.NewGuid(), codeArticleParent, alternativeNomenclature, codeParentNiveau, codeComposant, niveau, sequence, cle, sourceSystem, importBatchId, importedAt);
    }

    public void MettreAJour(string? alternativeNomenclature, string codeParentNiveau, string codeComposant, string niveau, string sequence, DateTime? sourceUpdatedAt)
    {
        AlternativeNomenclature = NormaliserTexte(alternativeNomenclature, 50);
        CodeParentNiveau = NormaliserTexte(codeParentNiveau, 50)!;
        CodeComposant = NormaliserTexte(codeComposant, 100)!;
        Niveau = NormaliserTexte(niveau, 50)!;
        Sequence = NormaliserTexte(sequence, 50)!;
        SourceUpdatedAt = sourceUpdatedAt;
        CleMetier = GenererCleMetier(CodeArticleParent, AlternativeNomenclature, CodeParentNiveau, CodeComposant, Niveau, Sequence);
    }

    public void Desactiver() => IsActive = false;
    public void Reactiver() => IsActive = true;

    public static string GenererCleMetier(string codeArticleParent, string? alternativeNomenclature, string codeParentNiveau, string codeComposant, string niveau, string sequence)
    {
        var parts = new[] { codeArticleParent?.Trim() ?? string.Empty, alternativeNomenclature?.Trim() ?? string.Empty, codeParentNiveau?.Trim() ?? string.Empty, codeComposant?.Trim() ?? string.Empty, niveau?.Trim() ?? string.Empty, sequence?.Trim() ?? string.Empty };
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
