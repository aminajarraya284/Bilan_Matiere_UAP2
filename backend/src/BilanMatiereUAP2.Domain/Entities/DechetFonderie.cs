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
        OrigineDechet origine,
        decimal poidsKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime dateCreationUtc)
    {
        ValiderDonnees(typeMatiereId, poidsKg, observation);
        Id = id;
        DateDeclaration = dateDeclaration;
        SemaineIso = CalculerSemaineIso(dateDeclaration);
        TypeMatiereId = typeMatiereId;
        TypeDechet = typeDechet;
        Origine = origine;
        PoidsKg = poidsKg;
        Observation = NormaliserTexte(observation);
        Statut = StatutDechet.Brouillon;
        CreeParUtilisateurId = creeParUtilisateurId;
        CreeParNom = NormaliserTexte(creeParNom);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public DateOnly DateDeclaration { get; private set; }
    public int SemaineIso { get; private set; }
    public Guid TypeMatiereId { get; private set; }
    public TypeMatiere TypeMatiere { get; private set; } = null!;
    public TypeDechet TypeDechet { get; private set; }
    public OrigineDechet Origine { get; private set; }
    public decimal PoidsKg { get; private set; }
    public string? Observation { get; private set; }
    public StatutDechet Statut { get; private set; }
    public Guid? CreeParUtilisateurId { get; private set; }
    public string? CreeParNom { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateModificationUtc { get; private set; }
    public Guid? ValideParUtilisateurId { get; private set; }
    public string? ValideParNom { get; private set; }
    public DateTime? ValideLeUtc { get; private set; }
    public Guid? AnnuleParUtilisateurId { get; private set; }
    public string? AnnuleParNom { get; private set; }
    public DateTime? AnnuleLeUtc { get; private set; }

    // Champs historiques conserves pour la compatibilite de schema. Les nouveaux
    // flux utilisent uniquement le statut Annule, sans suppression logique.
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
        => Creer(
            dateDeclaration,
            typeMatiereId,
            typeDechet,
            OrigineDechet.Fonderie,
            poidsKg,
            observation,
            creeParUtilisateurId,
            creeParNom,
            dateCreationUtc,
            id);

    public static DechetFonderie Creer(
        DateOnly dateDeclaration,
        Guid typeMatiereId,
        TypeDechet typeDechet,
        OrigineDechet origine,
        decimal poidsKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime dateCreationUtc,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            dateDeclaration,
            typeMatiereId,
            typeDechet,
            origine,
            poidsKg,
            observation,
            creeParUtilisateurId,
            creeParNom,
            dateCreationUtc);

    public void Modifier(
        DateOnly dateDeclaration,
        Guid typeMatiereId,
        TypeDechet typeDechet,
        decimal poidsKg,
        string? observation,
        DateTime dateModificationUtc)
        => Modifier(
            dateDeclaration,
            typeMatiereId,
            typeDechet,
            Origine,
            poidsKg,
            observation,
            dateModificationUtc);

    public void Modifier(
        DateOnly dateDeclaration,
        Guid typeMatiereId,
        TypeDechet typeDechet,
        OrigineDechet origine,
        decimal poidsKg,
        string? observation,
        DateTime dateModificationUtc)
    {
        if (Statut != StatutDechet.Brouillon)
        {
            throw new DomainValidationException("Seul un dechet Brouillon peut etre modifie.");
        }

        ValiderDonnees(typeMatiereId, poidsKg, observation);
        DateDeclaration = dateDeclaration;
        SemaineIso = CalculerSemaineIso(dateDeclaration);
        TypeMatiereId = typeMatiereId;
        TypeDechet = typeDechet;
        Origine = origine;
        PoidsKg = poidsKg;
        Observation = NormaliserTexte(observation);
        DateModificationUtc = dateModificationUtc;
    }

    public void Valider(Guid? utilisateurId, string? utilisateurNom, DateTime dateValidationUtc)
    {
        if (Statut != StatutDechet.Brouillon)
        {
            throw new DomainValidationException("Seul un dechet Brouillon peut etre valide.");
        }

        Statut = StatutDechet.Valide;
        ValideParUtilisateurId = utilisateurId;
        ValideParNom = NormaliserTexte(utilisateurNom);
        ValideLeUtc = dateValidationUtc;
        DateModificationUtc = dateValidationUtc;
    }

    public void Annuler(DateTime dateModificationUtc)
        => Annuler(null, null, dateModificationUtc);

    public void Annuler(Guid? utilisateurId, string? utilisateurNom, DateTime dateModificationUtc)
    {
        if (Statut == StatutDechet.Annule)
        {
            return;
        }

        Statut = StatutDechet.Annule;
        AnnuleParUtilisateurId = utilisateurId;
        AnnuleParNom = NormaliserTexte(utilisateurNom);
        AnnuleLeUtc = dateModificationUtc;
        DateModificationUtc = dateModificationUtc;
    }

    [Obsolete("Utiliser Annuler : les dechets restent dans l'historique.")]
    public void SupprimerLogiquement(DateTime dateSuppressionUtc)
    {
        Annuler(dateSuppressionUtc);
    }

    public static int CalculerSemaineIso(DateOnly date)
        => ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));

    private static void ValiderDonnees(Guid typeMatiereId, decimal poidsKg, string? observation)
    {
        if (typeMatiereId == Guid.Empty)
        {
            throw new DomainValidationException("Le type matiere est obligatoire.");
        }

        if (poidsKg <= 0)
        {
            throw new DomainValidationException("Le poids doit etre strictement superieur a zero.");
        }

        if (observation?.Trim().Length > 500)
        {
            throw new DomainValidationException("L'observation ne peut pas depasser 500 caracteres.");
        }
    }

    private static string? NormaliserTexte(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
