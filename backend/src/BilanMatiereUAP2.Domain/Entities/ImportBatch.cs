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
