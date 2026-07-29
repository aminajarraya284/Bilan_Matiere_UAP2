using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class ImportBatch
{
    private ImportBatch() { }

    private ImportBatch(Guid id, string hashFichierSha256, TypeImport typeImport, string profilImport, Guid? creeParUtilisateurId, string? creeParNom, DateTime dateCreationUtc)
    {
        Id = id;
        HashFichierSha256 = ValiderHash(hashFichierSha256);
        TypeImport = typeImport;
        ProfilImport = profilImport?.Trim();
        Statut = StatutImport.EnCours;
        CreeParUtilisateurId = creeParUtilisateurId;
        CreeParNom = NormaliserTexte(creeParNom, 150);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public string HashFichierSha256 { get; private set; } = string.Empty;
    public TypeImport TypeImport { get; private set; }
    public string? ProfilImport { get; private set; }
    public StatutImport Statut { get; private set; }
    public Guid? CreeParUtilisateurId { get; private set; }
    public string? CreeParNom { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateFinUtc { get; private set; }
    public int NombreLignesLues { get; private set; }
    public int NombreLignesValides { get; private set; }
    public int NombreLignesImportees { get; private set; }
    public int NombreAnomalies { get; private set; }
    public int ReadCount { get; private set; }
    public int ValidCount { get; private set; }
    public int InsertedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int UnchangedCount { get; private set; }
    public int RejectedCount { get; private set; }
    public string? ArchiveErrorMessage { get; private set; }

    public static ImportBatch Demarrer(string hashFichierSha256, TypeImport typeImport, string profilImport, Guid? creeParUtilisateurId, string? creeParNom, DateTime dateCreationUtc, Guid? id = null)
    {
        return new ImportBatch(id ?? Guid.NewGuid(), hashFichierSha256, typeImport, profilImport, creeParUtilisateurId, creeParNom, dateCreationUtc);
    }

    public void Terminer(StatutImport statut, DateTime dateFinUtc)
    {
        if (dateFinUtc < DateCreationUtc) throw new DomainValidationException("La date de fin d'import est invalide.");
        Statut = statut;
        DateFinUtc = dateFinUtc;
    }

    public void Terminer(
        StatutImport statut,
        DateTime dateFinUtc,
        int nombreLignesLues,
        int nombreLignesValides,
        int nombreLignesImportees,
        int nombreAnomalies)
    {
        Terminer(statut, dateFinUtc);
        NombreLignesLues = nombreLignesLues;
        NombreLignesValides = nombreLignesValides;
        NombreLignesImportees = nombreLignesImportees;
        NombreAnomalies = nombreAnomalies;
        ReadCount = nombreLignesLues;
        ValidCount = nombreLignesValides;
        InsertedCount = Math.Min(nombreLignesImportees, nombreLignesValides);
        UpdatedCount = 0;
        UnchangedCount = Math.Max(0, nombreLignesValides - InsertedCount);
        RejectedCount = Math.Max(0, nombreLignesLues - nombreLignesValides);
    }

    public void TerminerImport(
        StatutImport statut,
        DateTime dateFinUtc,
        int readCount,
        int validCount,
        int insertedCount,
        int updatedCount,
        int unchangedCount,
        int rejectedCount,
        int nombreAnomalies)
    {
        if (readCount < 0 || validCount < 0 || insertedCount < 0 || updatedCount < 0 ||
            unchangedCount < 0 || rejectedCount < 0 || nombreAnomalies < 0)
        {
            throw new DomainValidationException("Les compteurs d'import ne peuvent pas etre negatifs.");
        }

        if (readCount != validCount + rejectedCount)
        {
            throw new DomainValidationException("ReadCount doit etre egal a ValidCount + RejectedCount.");
        }

        if (validCount != insertedCount + updatedCount + unchangedCount)
        {
            throw new DomainValidationException("ValidCount doit etre egal a InsertedCount + UpdatedCount + UnchangedCount.");
        }

        Terminer(statut, dateFinUtc);
        ReadCount = readCount;
        ValidCount = validCount;
        InsertedCount = insertedCount;
        UpdatedCount = updatedCount;
        UnchangedCount = unchangedCount;
        RejectedCount = rejectedCount;
        NombreLignesLues = readCount;
        NombreLignesValides = validCount;
        NombreLignesImportees = insertedCount + updatedCount;
        NombreAnomalies = nombreAnomalies;
    }

    public void SignalerErreurArchive(string message)
    {
        ArchiveErrorMessage = NormaliserTexte(message, 2000);
        if (Statut != StatutImport.Duplicate)
        {
            Statut = StatutImport.TermineAvecAvertissementArchive;
        }
    }

    internal static string ValiderHash(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new DomainValidationException("Le hash SHA-256 est invalide.");
        }

        return normalized;
    }

    internal static string? NormaliserTexte(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength) throw new DomainValidationException($"La valeur ne peut pas depasser {maxLength} caracteres.");
        return normalized;
    }
}
