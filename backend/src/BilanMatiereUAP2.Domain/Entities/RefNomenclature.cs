using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class RefNomenclature
{
    private RefNomenclature() { }

    private RefNomenclature(
        Guid id,
        string codeArticleParent,
        string? alternativeNomenclature,
        string codeParentNiveau,
        string codeComposant,
        string niveau,
        string sequence,
        string? designationComposant,
        decimal? quantiteLien,
        decimal? quantiteLienCumulee,
        string? uniteQuantite,
        string? categorieComposant,
        string? typeLien,
        decimal? pourcentageRebut,
        string cleMetier,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt,
        bool isActive)
    {
        Id = id;
        CodeArticleParent = NormaliserCode(codeArticleParent, 100);
        AlternativeNomenclature = NormaliserCodeOptional(alternativeNomenclature, 50);
        CodeParentNiveau = NormaliserCode(codeParentNiveau, 50);
        CodeComposant = NormaliserCode(codeComposant, 100);
        Niveau = NormaliserCode(niveau, 50);
        Sequence = NormaliserCode(sequence, 50);
        DesignationComposant = NormaliserTexte(designationComposant, 250);
        QuantiteLien = quantiteLien;
        QuantiteLienCumulee = quantiteLienCumulee;
        UniteQuantite = NormaliserTexte(uniteQuantite, 100);
        CategorieComposant = NormaliserTexte(categorieComposant, 100);
        TypeLien = NormaliserTexte(typeLien, 100);
        PourcentageRebut = pourcentageRebut;
        CleMetier = cleMetier;
        SourceSystem = NormaliserTexte(sourceSystem, 100) ?? string.Empty;
        ImportBatchId = importBatchId;
        ImportedAt = importedAt;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public string CodeArticleParent { get; private set; } = string.Empty;
    public string? AlternativeNomenclature { get; private set; }
    public string CodeParentNiveau { get; private set; } = string.Empty;
    public string CodeComposant { get; private set; } = string.Empty;
    public string Niveau { get; private set; } = string.Empty;
    public string Sequence { get; private set; } = string.Empty;
    public string? DesignationComposant { get; private set; }
    public decimal? QuantiteLien { get; private set; }
    public decimal? QuantiteLienCumulee { get; private set; }
    public string? UniteQuantite { get; private set; }
    public string? CategorieComposant { get; private set; }
    public string? TypeLien { get; private set; }
    public decimal? PourcentageRebut { get; private set; }
    public string CleMetier { get; private set; } = string.Empty;
    public string SourceSystem { get; private set; } = string.Empty;
    public Guid ImportBatchId { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public DateTime? SourceUpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static RefNomenclature Creer(
        string codeArticleParent,
        string? alternativeNomenclature,
        string codeParentNiveau,
        string codeComposant,
        string niveau,
        string sequence,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
        => Creer(
            codeArticleParent,
            alternativeNomenclature,
            codeParentNiveau,
            codeComposant,
            niveau,
            sequence,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            sourceSystem,
            importBatchId,
            importedAt);

    public static RefNomenclature Creer(
        string codeArticleParent,
        string? alternativeNomenclature,
        string codeParentNiveau,
        string codeComposant,
        string niveau,
        string sequence,
        string? designationComposant,
        decimal? quantiteLien,
        decimal? quantiteLienCumulee,
        string? uniteQuantite,
        string? categorieComposant,
        string? typeLien,
        decimal? pourcentageRebut,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
    {
        var cleMetier = GenererCleMetier(codeArticleParent, alternativeNomenclature, codeParentNiveau, codeComposant, niveau, sequence);
        return new RefNomenclature(
            Guid.NewGuid(),
            codeArticleParent,
            alternativeNomenclature,
            codeParentNiveau,
            codeComposant,
            niveau,
            sequence,
            designationComposant,
            quantiteLien,
            quantiteLienCumulee,
            uniteQuantite,
            categorieComposant,
            typeLien,
            pourcentageRebut,
            cleMetier,
            sourceSystem,
            importBatchId,
            importedAt,
            true);
    }

    public void MettreAJour(string? alternativeNomenclature, string codeParentNiveau, string codeComposant, string niveau, string sequence, DateTime? sourceUpdatedAt)
    {
        AlternativeNomenclature = NormaliserCodeOptional(alternativeNomenclature, 50);
        CodeParentNiveau = NormaliserCode(codeParentNiveau, 50);
        CodeComposant = NormaliserCode(codeComposant, 100);
        Niveau = NormaliserCode(niveau, 50);
        Sequence = NormaliserCode(sequence, 50);
        SourceUpdatedAt = sourceUpdatedAt;
        CleMetier = GenererCleMetier(CodeArticleParent, AlternativeNomenclature, CodeParentNiveau, CodeComposant, Niveau, Sequence);
    }

    public void MettreAJour(
        string? alternativeNomenclature,
        string codeParentNiveau,
        string codeComposant,
        string niveau,
        string sequence,
        string? designationComposant,
        decimal? quantiteLien,
        decimal? quantiteLienCumulee,
        string? uniteQuantite,
        string? categorieComposant,
        string? typeLien,
        decimal? pourcentageRebut,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
    {
        AlternativeNomenclature = NormaliserCodeOptional(alternativeNomenclature, 50);
        CodeParentNiveau = NormaliserCode(codeParentNiveau, 50);
        CodeComposant = NormaliserCode(codeComposant, 100);
        Niveau = NormaliserCode(niveau, 50);
        Sequence = NormaliserCode(sequence, 50);
        DesignationComposant = NormaliserTexte(designationComposant, 250);
        QuantiteLien = quantiteLien;
        QuantiteLienCumulee = quantiteLienCumulee;
        UniteQuantite = NormaliserTexte(uniteQuantite, 100);
        CategorieComposant = NormaliserTexte(categorieComposant, 100);
        TypeLien = NormaliserTexte(typeLien, 100);
        PourcentageRebut = pourcentageRebut;
        SourceSystem = NormaliserTexte(sourceSystem, 100) ?? string.Empty;
        ImportBatchId = importBatchId;
        ImportedAt = importedAt;
        CleMetier = GenererCleMetier(CodeArticleParent, AlternativeNomenclature, CodeParentNiveau, CodeComposant, Niveau, Sequence);
    }

    public void Desactiver() => IsActive = false;
    public void Reactiver() => IsActive = true;

    public static string GenererCleMetier(string codeArticleParent, string? alternativeNomenclature, string codeParentNiveau, string codeComposant, string niveau, string sequence)
        => string.Join(
            "|",
            new[] { codeArticleParent, alternativeNomenclature, codeParentNiveau, codeComposant, niveau, sequence }
                .Select(NormaliserPartieCle));

    private static string NormaliserPartieCle(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string NormaliserCode(string value, int maxLength)
        => NormaliserCodeOptional(value, maxLength) ?? throw new DomainValidationException("La valeur codee est obligatoire.");

    private static string? NormaliserCodeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength) throw new DomainValidationException($"La valeur ne peut pas depasser {maxLength} caracteres.");
        return normalized;
    }

    private static string? NormaliserTexte(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength) throw new DomainValidationException($"La valeur ne peut pas depasser {maxLength} caracteres.");
        return normalized;
    }
}
