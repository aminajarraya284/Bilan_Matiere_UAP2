using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

// Archive le fichier source original d'un import (GAMMEFAB, BOMARTPARENT, YRJMCH, ou l'un des deux
// fichiers d'un import X3 Sorties matiere), independamment du batch specifique (ImportBatch ou
// ImportX3Batch) qui l'a traite : ImportBatchId reference l'un ou l'autre selon TypeImport,
// sans contrainte FK (les deux familles de batch ne partagent pas d'espace de cles commun).
public sealed class ImportSourceFile
{
    private ImportSourceFile() { }

    private ImportSourceFile(
        Guid id,
        Guid importBatchId,
        TypeImport typeImport,
        string? profilImport,
        string nomFichierOriginal,
        string extension,
        string mimeType,
        long tailleOctets,
        string hashSha256,
        string cheminStockage,
        DateTime dateUploadUtc,
        Guid? uploadeParUtilisateurId,
        string? uploadeParNom)
    {
        if (importBatchId == Guid.Empty)
        {
            throw new DomainValidationException("Le batch d'import associe est obligatoire.");
        }

        if (tailleOctets <= 0)
        {
            throw new DomainValidationException("Le fichier archive ne peut pas etre vide.");
        }

        Id = id;
        ImportBatchId = importBatchId;
        TypeImport = typeImport;
        ProfilImport = NormaliserTexte(profilImport, 100);
        NomFichierOriginal = NormaliserTexte(nomFichierOriginal, 260)
            ?? throw new DomainValidationException("Le nom original du fichier est obligatoire.");
        Extension = NormaliserTexte(extension, 20)?.ToLowerInvariant()
            ?? throw new DomainValidationException("L'extension du fichier est obligatoire.");
        MimeType = NormaliserTexte(mimeType, 150) ?? "application/octet-stream";
        TailleOctets = tailleOctets;
        HashSha256 = ValiderHash(hashSha256);
        CheminStockage = NormaliserTexte(cheminStockage, 500)
            ?? throw new DomainValidationException("Le chemin de stockage est obligatoire.");
        DateUploadUtc = dateUploadUtc;
        UploadeParUtilisateurId = uploadeParUtilisateurId;
        UploadeParNom = NormaliserTexte(uploadeParNom, 150);
    }

    public Guid Id { get; private set; }
    public Guid ImportBatchId { get; private set; }
    public TypeImport TypeImport { get; private set; }
    public string? ProfilImport { get; private set; }
    public string NomFichierOriginal { get; private set; } = string.Empty;
    public string Extension { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long TailleOctets { get; private set; }
    public string HashSha256 { get; private set; } = string.Empty;
    public string CheminStockage { get; private set; } = string.Empty;
    public DateTime DateUploadUtc { get; private set; }
    public Guid? UploadeParUtilisateurId { get; private set; }
    public string? UploadeParNom { get; private set; }

    public static ImportSourceFile Creer(
        Guid importBatchId,
        TypeImport typeImport,
        string? profilImport,
        string nomFichierOriginal,
        string extension,
        string mimeType,
        long tailleOctets,
        string hashSha256,
        string cheminStockage,
        DateTime dateUploadUtc,
        Guid? uploadeParUtilisateurId,
        string? uploadeParNom,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            importBatchId,
            typeImport,
            profilImport,
            nomFichierOriginal,
            extension,
            mimeType,
            tailleOctets,
            hashSha256,
            cheminStockage,
            dateUploadUtc,
            uploadeParUtilisateurId,
            uploadeParNom);

    private static string ValiderHash(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new DomainValidationException("Le hash SHA-256 est invalide.");
        }

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
