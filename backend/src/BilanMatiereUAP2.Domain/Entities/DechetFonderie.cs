using System.Globalization;
using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class DechetFonderie
{
    private DechetFonderie()
    {
    }

    private DechetFonderie(
        Guid id,
        DateOnly dateDeclaration,
        Guid typeMatiereId,
        TypeDechet typeDechet,
        decimal poidsKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime dateCreationUtc)
    {
        Valider(typeMatiereId, poidsKg, observation);

        Id = id;
        DateDeclaration = dateDeclaration;
        SemaineIso = CalculerSemaineIso(dateDeclaration);
        TypeMatiereId = typeMatiereId;
        TypeDechet = typeDechet;
        PoidsKg = poidsKg;
        Observation = NormaliserObservation(observation);
        Statut = StatutDechet.Valide;
        CreeParUtilisateurId = creeParUtilisateurId;
        CreeParNom = NormaliserNomUtilisateur(creeParNom);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public DateOnly DateDeclaration { get; private set; }
    public int SemaineIso { get; private set; }
    public Guid TypeMatiereId { get; private set; }
    public TypeMatiere TypeMatiere { get; private set; } = null!;
    public TypeDechet TypeDechet { get; private set; }
    public decimal PoidsKg { get; private set; }
    public string? Observation { get; private set; }
    public StatutDechet Statut { get; private set; }
    public Guid? CreeParUtilisateurId { get; private set; }
    public string? CreeParNom { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateModificationUtc { get; private set; }
    public bool EstSupprime { get; private set; }
    public DateTime? DateSuppressionUtc { get; private set; }

    public static DechetFonderie Creer(
        DateOnly dateDeclaration,
        Guid typeMatiereId,
        TypeDechet typeDechet,
        decimal poidsKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime dateCreationUtc,
        Guid? id = null)
    {
        return new DechetFonderie(
            id ?? Guid.NewGuid(),
            dateDeclaration,
            typeMatiereId,
            typeDechet,
            poidsKg,
            observation,
            creeParUtilisateurId,
            creeParNom,
            dateCreationUtc);
    }

    public void Modifier(
        DateOnly dateDeclaration,
        Guid typeMatiereId,
        TypeDechet typeDechet,
        decimal poidsKg,
        string? observation,
        DateTime dateModificationUtc)
    {
        if (Statut == StatutDechet.Annule)
        {
            throw new DomainValidationException("Un déchet annulé ne peut pas être modifié.");
        }

        Valider(typeMatiereId, poidsKg, observation);
        DateDeclaration = dateDeclaration;
        SemaineIso = CalculerSemaineIso(dateDeclaration);
        TypeMatiereId = typeMatiereId;
        TypeDechet = typeDechet;
        PoidsKg = poidsKg;
        Observation = NormaliserObservation(observation);
        DateModificationUtc = dateModificationUtc;
    }

    public void Annuler(DateTime dateModificationUtc)
    {
        if (Statut == StatutDechet.Annule)
        {
            return;
        }

        Statut = StatutDechet.Annule;
        DateModificationUtc = dateModificationUtc;
    }

    public void SupprimerLogiquement(DateTime dateSuppressionUtc)
    {
        if (EstSupprime)
        {
            return;
        }

        EstSupprime = true;
        Statut = StatutDechet.Annule;
        DateSuppressionUtc = dateSuppressionUtc;
        DateModificationUtc = dateSuppressionUtc;
    }

    public static int CalculerSemaineIso(DateOnly date)
    {
        return ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));
    }

    private static void Valider(Guid typeMatiereId, decimal poidsKg, string? observation)
    {
        if (typeMatiereId == Guid.Empty)
        {
            throw new DomainValidationException("Le type matière est obligatoire.");
        }

        if (poidsKg <= 0)
        {
            throw new DomainValidationException("Le poids doit être strictement supérieur à zéro.");
        }

        if (observation?.Trim().Length > 500)
        {
            throw new DomainValidationException("L’observation ne peut pas dépasser 500 caractères.");
        }
    }

    private static string? NormaliserObservation(string? observation)
    {
        var normalized = observation?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string? NormaliserNomUtilisateur(string? userName)
    {
        var normalized = userName?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
