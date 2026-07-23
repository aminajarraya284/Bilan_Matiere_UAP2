using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class ImportX3Batch
{
    private ImportX3Batch()
    {
    }

    private ImportX3Batch(
        Guid id,
        string hashCoupleSha256,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime dateCreationUtc)
    {
        Id = id;
        HashCoupleSha256 = ValiderHash(hashCoupleSha256);
        Statut = StatutRapprochementX3.Rapproche;
        CreeParUtilisateurId = creeParUtilisateurId;
        CreeParNom = NormaliserTexte(creeParNom, 150);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public string HashCoupleSha256 { get; private set; } = string.Empty;
    public StatutRapprochementX3 Statut { get; private set; }
    public int LignesEncoursLues { get; private set; }
    public int LignesEncoursValides { get; private set; }
    public int LignesMouvementsLues { get; private set; }
    public int LignesMouvementsValides { get; private set; }
    public int NombreSortiesGenerees { get; private set; }
    public int NombreAnomalies { get; private set; }
    public Guid? CreeParUtilisateurId { get; private set; }
    public string? CreeParNom { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateFinUtc { get; private set; }
    public ICollection<ImportX3File> Fichiers { get; private set; } = new List<ImportX3File>();
    public ICollection<ImportX3Anomaly> Anomalies { get; private set; } = new List<ImportX3Anomaly>();
    public ICollection<X3MouvementStock> Mouvements { get; private set; } = new List<X3MouvementStock>();
    public ICollection<ConsommationComposant> Consommations { get; private set; } = new List<ConsommationComposant>();

    public static ImportX3Batch Demarrer(
        string hashCoupleSha256,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime dateCreationUtc,
        Guid? id = null)
    {
        return new ImportX3Batch(
            id ?? Guid.NewGuid(),
            hashCoupleSha256,
            creeParUtilisateurId,
            creeParNom,
            dateCreationUtc);
    }

    public void AjouterFichier(ImportX3File fichier)
    {
        if (Fichiers.Any(item => item.TypeFichier == fichier.TypeFichier))
        {
            throw new DomainValidationException("Un seul fichier de chaque type est autorise par import.");
        }

        Fichiers.Add(fichier);
    }

    public void AjouterAnomalie(ImportX3Anomaly anomalie) => Anomalies.Add(anomalie);

    public void AjouterMouvement(X3MouvementStock mouvement) => Mouvements.Add(mouvement);

    public void AjouterConsommation(ConsommationComposant consommation) => Consommations.Add(consommation);

    public void Terminer(
        int lignesEncoursLues,
        int lignesEncoursValides,
        int lignesMouvementsLues,
        int lignesMouvementsValides,
        DateTime dateFinUtc,
        int? nombreSortiesGenerees = null,
        int? nombreAnomalies = null)
    {
        if (dateFinUtc < DateCreationUtc)
        {
            throw new DomainValidationException("La date de fin d'import est invalide.");
        }

        LignesEncoursLues = ValiderCompteur(lignesEncoursLues);
        LignesEncoursValides = ValiderCompteur(lignesEncoursValides);
        LignesMouvementsLues = ValiderCompteur(lignesMouvementsLues);
        LignesMouvementsValides = ValiderCompteur(lignesMouvementsValides);
        NombreSortiesGenerees = nombreSortiesGenerees ?? Consommations.Count;
        NombreAnomalies = nombreAnomalies ?? Anomalies.Count;
        Statut = NombreAnomalies == 0
            ? StatutRapprochementX3.Rapproche
            : StatutRapprochementX3.ImportPartiel;
        DateFinUtc = dateFinUtc;
    }

    private static int ValiderCompteur(int value)
    {
        if (value < 0)
        {
            throw new DomainValidationException("Les compteurs d'import ne peuvent pas etre negatifs.");
        }

        return value;
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
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainValidationException($"La valeur ne peut pas depasser {maxLength} caracteres.");
        }

        return normalized;
    }
}
