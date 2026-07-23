using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class ImportX3File
{
    private ImportX3File()
    {
    }

    private ImportX3File(
        Guid id,
        Guid importBatchId,
        TypeFichierImportX3 typeFichier,
        string nomFichier,
        string extension,
        long tailleOctets,
        string hashSha256,
        DateTime dateCreationUtc)
    {
        if (importBatchId == Guid.Empty)
        {
            throw new DomainValidationException("L'import associe au fichier est obligatoire.");
        }

        if (tailleOctets <= 0)
        {
            throw new DomainValidationException("Le fichier importe est vide.");
        }

        Id = id;
        ImportBatchId = importBatchId;
        TypeFichier = typeFichier;
        NomFichier = ImportX3Batch.NormaliserTexte(nomFichier, 260)
            ?? throw new DomainValidationException("Le nom du fichier est obligatoire.");
        Extension = NormaliserExtension(extension);
        TailleOctets = tailleOctets;
        HashSha256 = ImportX3Batch.ValiderHash(hashSha256);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public Guid ImportBatchId { get; private set; }
    public ImportX3Batch ImportBatch { get; private set; } = null!;
    public TypeFichierImportX3 TypeFichier { get; private set; }
    public string NomFichier { get; private set; } = string.Empty;
    public string Extension { get; private set; } = string.Empty;
    public long TailleOctets { get; private set; }
    public string HashSha256 { get; private set; } = string.Empty;
    public DateTime DateCreationUtc { get; private set; }

    public static ImportX3File Creer(
        Guid importBatchId,
        TypeFichierImportX3 typeFichier,
        string nomFichier,
        string extension,
        long tailleOctets,
        string hashSha256,
        DateTime dateCreationUtc,
        Guid? id = null)
    {
        return new ImportX3File(
            id ?? Guid.NewGuid(),
            importBatchId,
            typeFichier,
            nomFichier,
            extension,
            tailleOctets,
            hashSha256,
            dateCreationUtc);
    }

    private static string NormaliserExtension(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized is not ".xls" and not ".xlsx")
        {
            throw new DomainValidationException("L'extension du fichier Excel est invalide.");
        }

        return normalized;
    }
}
