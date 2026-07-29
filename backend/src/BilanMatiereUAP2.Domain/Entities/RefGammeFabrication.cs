using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class RefGammeFabrication
{
    private RefGammeFabrication() { }

    private RefGammeFabrication(
        Guid id,
        string codeArticle,
        string? designationArticle,
        DateOnly? dateFinValidite,
        string? etapeSuivi,
        string? alternativeGamme,
        string numeroOperation,
        string? codeAtelier,
        string? codePoste,
        decimal? nombrePostes,
        string? groupePosteCharge,
        string? designationOperation,
        string? uniteOperation,
        decimal? coefficientOperationStock,
        decimal? tempsCycle,
        decimal? trs,
        decimal? tempsAttente,
        string? jalon,
        decimal? quantite,
        decimal? cadence,
        DateTime? dateModificationX3,
        decimal? tempsReglage,
        decimal? coefficientCcpt,
        string cleMetier,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt,
        bool isActive)
    {
        Id = id;
        CodeArticle = NormaliserCode(codeArticle, 100);
        DesignationArticle = NormaliserTexte(designationArticle, 250);
        DateFinValidite = dateFinValidite;
        EtapeSuivi = NormaliserTexte(etapeSuivi, 50);
        AlternativeGamme = NormaliserCodeOptional(alternativeGamme, 50);
        NumeroOperation = NormaliserCode(numeroOperation, 50);
        CodeAtelier = NormaliserCodeOptional(codeAtelier, 50) ?? string.Empty;
        CodePoste = NormaliserCodeOptional(codePoste, 50) ?? string.Empty;
        NombrePostes = nombrePostes;
        GroupePosteCharge = NormaliserTexte(groupePosteCharge, 100);
        DesignationOperation = NormaliserTexte(designationOperation, 250);
        UniteOperation = NormaliserTexte(uniteOperation, 20);
        CoefficientOperationStock = coefficientOperationStock;
        TempsCycle = tempsCycle;
        Trs = trs;
        TempsAttente = tempsAttente;
        Jalon = NormaliserTexte(jalon, 50);
        Quantite = quantite;
        Cadence = cadence;
        DateModificationX3 = dateModificationX3;
        SourceUpdatedAt = dateModificationX3;
        TempsReglage = tempsReglage;
        CoefficientCcpt = coefficientCcpt;
        CleMetier = cleMetier;
        SourceSystem = NormaliserTexte(sourceSystem, 100) ?? string.Empty;
        ImportBatchId = importBatchId;
        ImportedAt = importedAt;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public string CodeArticle { get; private set; } = string.Empty;
    public string? DesignationArticle { get; private set; }
    public DateOnly? DateFinValidite { get; private set; }
    public string? EtapeSuivi { get; private set; }
    public string? AlternativeGamme { get; private set; }
    public string NumeroOperation { get; private set; } = string.Empty;
    public string CodeAtelier { get; private set; } = string.Empty;
    public string CodePoste { get; private set; } = string.Empty;
    public decimal? NombrePostes { get; private set; }
    public string? GroupePosteCharge { get; private set; }
    public string? DesignationOperation { get; private set; }
    public string? UniteOperation { get; private set; }
    public decimal? CoefficientOperationStock { get; private set; }
    public decimal? TempsCycle { get; private set; }
    public decimal? Trs { get; private set; }
    public decimal? TempsAttente { get; private set; }
    public string? Jalon { get; private set; }
    public decimal? Quantite { get; private set; }
    public decimal? Cadence { get; private set; }
    public DateTime? DateModificationX3 { get; private set; }
    public decimal? TempsReglage { get; private set; }
    public decimal? CoefficientCcpt { get; private set; }
    public string CleMetier { get; private set; } = string.Empty;
    public string SourceSystem { get; private set; } = string.Empty;
    public Guid ImportBatchId { get; private set; }
    public DateTime ImportedAt { get; private set; }
    public DateTime? SourceUpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static RefGammeFabrication Creer(
        string codeArticle,
        string? alternativeGamme,
        string numeroOperation,
        string codePoste,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
        => Creer(
            codeArticle,
            null,
            null,
            null,
            alternativeGamme,
            numeroOperation,
            null,
            codePoste,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
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

    public static RefGammeFabrication Creer(
        string codeArticle,
        string? designationArticle,
        DateOnly? dateFinValidite,
        string? etapeSuivi,
        string? alternativeGamme,
        string numeroOperation,
        string? codeAtelier,
        string? codePoste,
        decimal? nombrePostes,
        string? groupePosteCharge,
        string? designationOperation,
        string? uniteOperation,
        decimal? coefficientOperationStock,
        decimal? tempsCycle,
        decimal? trs,
        decimal? tempsAttente,
        string? jalon,
        decimal? quantite,
        decimal? cadence,
        DateTime? dateModificationX3,
        decimal? tempsReglage,
        decimal? coefficientCcpt,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
    {
        var cleMetier = GenererCleMetier(codeArticle, alternativeGamme, numeroOperation, codeAtelier, codePoste);
        return new RefGammeFabrication(
            Guid.NewGuid(),
            codeArticle,
            designationArticle,
            dateFinValidite,
            etapeSuivi,
            alternativeGamme,
            numeroOperation,
            codeAtelier,
            codePoste,
            nombrePostes,
            groupePosteCharge,
            designationOperation,
            uniteOperation,
            coefficientOperationStock,
            tempsCycle,
            trs,
            tempsAttente,
            jalon,
            quantite,
            cadence,
            dateModificationX3,
            tempsReglage,
            coefficientCcpt,
            cleMetier,
            sourceSystem,
            importBatchId,
            importedAt,
            true);
    }

    public void MettreAJour(string? alternativeGamme, string numeroOperation, string codePoste, DateTime? sourceUpdatedAt)
    {
        AlternativeGamme = NormaliserCodeOptional(alternativeGamme, 50);
        NumeroOperation = NormaliserCode(numeroOperation, 50);
        CodePoste = NormaliserCodeOptional(codePoste, 50) ?? string.Empty;
        SourceUpdatedAt = sourceUpdatedAt;
        DateModificationX3 = sourceUpdatedAt;
        CleMetier = GenererCleMetier(CodeArticle, AlternativeGamme, NumeroOperation, CodeAtelier, CodePoste);
    }

    public void MettreAJour(
        string? designationArticle,
        DateOnly? dateFinValidite,
        string? etapeSuivi,
        string? alternativeGamme,
        string numeroOperation,
        string? codeAtelier,
        string? codePoste,
        decimal? nombrePostes,
        string? groupePosteCharge,
        string? designationOperation,
        string? uniteOperation,
        decimal? coefficientOperationStock,
        decimal? tempsCycle,
        decimal? trs,
        decimal? tempsAttente,
        string? jalon,
        decimal? quantite,
        decimal? cadence,
        DateTime? dateModificationX3,
        decimal? tempsReglage,
        decimal? coefficientCcpt,
        string sourceSystem,
        Guid importBatchId,
        DateTime importedAt)
    {
        DesignationArticle = NormaliserTexte(designationArticle, 250);
        DateFinValidite = dateFinValidite;
        EtapeSuivi = NormaliserTexte(etapeSuivi, 50);
        AlternativeGamme = NormaliserCodeOptional(alternativeGamme, 50);
        NumeroOperation = NormaliserCode(numeroOperation, 50);
        CodeAtelier = NormaliserCodeOptional(codeAtelier, 50) ?? string.Empty;
        CodePoste = NormaliserCodeOptional(codePoste, 50) ?? string.Empty;
        NombrePostes = nombrePostes;
        GroupePosteCharge = NormaliserTexte(groupePosteCharge, 100);
        DesignationOperation = NormaliserTexte(designationOperation, 250);
        UniteOperation = NormaliserTexte(uniteOperation, 20);
        CoefficientOperationStock = coefficientOperationStock;
        TempsCycle = tempsCycle;
        Trs = trs;
        TempsAttente = tempsAttente;
        Jalon = NormaliserTexte(jalon, 50);
        Quantite = quantite;
        Cadence = cadence;
        DateModificationX3 = dateModificationX3;
        SourceUpdatedAt = dateModificationX3;
        TempsReglage = tempsReglage;
        CoefficientCcpt = coefficientCcpt;
        SourceSystem = NormaliserTexte(sourceSystem, 100) ?? string.Empty;
        ImportBatchId = importBatchId;
        ImportedAt = importedAt;
        CleMetier = GenererCleMetier(CodeArticle, AlternativeGamme, NumeroOperation, CodeAtelier, CodePoste);
    }

    public void Desactiver() => IsActive = false;
    public void Reactiver() => IsActive = true;

    public static string GenererCleMetier(string codeArticle, string? alternativeGamme, string numeroOperation, string codePoste)
        => GenererCleMetier(codeArticle, alternativeGamme, numeroOperation, string.Empty, codePoste);

    public static string GenererCleMetier(string codeArticle, string? alternativeGamme, string numeroOperation, string? codeAtelier, string? codePoste)
        => string.Join(
            "|",
            new[] { codeArticle, numeroOperation, codeAtelier, codePoste, alternativeGamme }
                .Select(NormaliserPartieCle));

    // X3 exporte "sans limite de validite" via une date sentinelle tres ancienne (ex. 31/12/1599,
    // constate sur 99% des lignes reelles GAMMEFAB) plutot qu'une valeur NULL. Une DateFinValidite
    // anterieure a ce seuil est donc traitee comme "toujours valide", pas comme "expiree en 1599".
    private static readonly DateOnly SeuilDateSentinelle = new(1900, 1, 1);

    public bool EstValideLe(DateOnly dateReception)
        => DateFinValidite is null || DateFinValidite < SeuilDateSentinelle || DateFinValidite >= dateReception;

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
