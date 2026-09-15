using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

/// <summary>
/// Comptage physique d'inventaire Fonderie, en trois parties distinctes :
///   * <see cref="InventaireConteneur"/>        - partie 1, comptage par conteneur ;
///   * <see cref="InventaireCategorieComptage"/> - partie 2, comptage par categorie ;
///   * <see cref="InventaireFourComptage"/>      - partie 3, comptage des fours.
///
/// REGLE METIER (arbitree) : le total des parties 1 et 2, sur les lignes VALIDES,
/// constitue le Stock FIN de la periode par type matiere. Les fours en sont EXCLUS :
/// ils restent traces et visibles dans le rapport d'inventaire, mais n'entrent dans
/// aucun bilan. Le Stock DEBUT d'une periode est le Stock FIN de la periode precedente.
/// Voir <see cref="InventaireComptageRules"/>.
///
/// Toutes les quantites de comptage sont en KILOGRAMMES.
/// </summary>
public abstract class LigneComptageInventaire
{
    protected LigneComptageInventaire()
    {
    }

    protected LigneComptageInventaire(
        Guid id,
        DateOnly periode,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc)
    {
        if (typeMatiereId == Guid.Empty)
        {
            throw new DomainValidationException("Le type matiere est obligatoire.");
        }

        if (quantiteKg < 0)
        {
            throw new DomainValidationException("La quantite comptee ne peut pas etre negative.");
        }

        Id = id;
        Periode = new DateOnly(periode.Year, periode.Month, 1);
        TypeMatiereId = typeMatiereId;
        QuantiteKg = quantiteKg;
        Observation = Normaliser(observation);
        Statut = StatutInventaireFonderie.Brouillon;
        CreeParUtilisateurId = creeParUtilisateurId;
        CreeParNom = Normaliser(creeParNom);
        CreeLeUtc = creeLeUtc;
    }

    public Guid Id { get; private set; }
    /// <summary>Premier jour du mois inventorie.</summary>
    public DateOnly Periode { get; private set; }
    public Guid TypeMatiereId { get; private set; }
    public TypeMatiere TypeMatiere { get; private set; } = null!;
    /// <summary>Quantite comptee, en kilogrammes.</summary>
    public decimal QuantiteKg { get; private set; }
    public string? Observation { get; private set; }
    public StatutInventaireFonderie Statut { get; private set; }

    public Guid? CreeParUtilisateurId { get; private set; }
    public string? CreeParNom { get; private set; }
    public DateTime CreeLeUtc { get; private set; }
    public Guid? ModifieParUtilisateurId { get; private set; }
    public string? ModifieParNom { get; private set; }
    public DateTime? ModifieLeUtc { get; private set; }
    public Guid? ValideParUtilisateurId { get; private set; }
    public string? ValideParNom { get; private set; }
    public DateTime? ValideLeUtc { get; private set; }

    /// <summary>Seules les lignes VALIDES alimentent le Stock fin.</summary>
    public bool EstRetenuePourLeStock => Statut == StatutInventaireFonderie.Valide;

    protected void AppliquerModification(
        DateOnly periode,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? modifieParUtilisateurId,
        string? modifieParNom,
        DateTime modifieLeUtc)
    {
        GarantirBrouillon("modifiee");
        if (typeMatiereId == Guid.Empty)
        {
            throw new DomainValidationException("Le type matiere est obligatoire.");
        }

        if (quantiteKg < 0)
        {
            throw new DomainValidationException("La quantite comptee ne peut pas etre negative.");
        }

        Periode = new DateOnly(periode.Year, periode.Month, 1);
        TypeMatiereId = typeMatiereId;
        QuantiteKg = quantiteKg;
        Observation = Normaliser(observation);
        ModifieParUtilisateurId = modifieParUtilisateurId;
        ModifieParNom = Normaliser(modifieParNom);
        ModifieLeUtc = modifieLeUtc;
    }

    public void Valider(Guid? utilisateurId, string? utilisateurNom, DateTime valideLeUtc)
    {
        GarantirBrouillon("validee");
        Statut = StatutInventaireFonderie.Valide;
        ValideParUtilisateurId = utilisateurId;
        ValideParNom = Normaliser(utilisateurNom);
        ValideLeUtc = valideLeUtc;
        ModifieLeUtc = valideLeUtc;
    }

    /// <summary>Annulation idempotente : la ligne reste dans l'historique, hors des totaux.</summary>
    public void Annuler(Guid? utilisateurId, string? utilisateurNom, DateTime annuleLeUtc)
    {
        if (Statut == StatutInventaireFonderie.Annule)
        {
            return;
        }

        Statut = StatutInventaireFonderie.Annule;
        ModifieParUtilisateurId = utilisateurId;
        ModifieParNom = Normaliser(utilisateurNom);
        ModifieLeUtc = annuleLeUtc;
    }

    private void GarantirBrouillon(string action)
    {
        if (Statut != StatutInventaireFonderie.Brouillon)
        {
            throw new DomainValidationException($"Seule une ligne d'inventaire Brouillon peut etre {action}.");
        }
    }

    protected static string? Normaliser(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    protected static string NormaliserObligatoire(string? valeur, string libelle)
    {
        var normalise = valeur?.Trim();
        if (string.IsNullOrEmpty(normalise))
        {
            throw new DomainValidationException($"{libelle} est obligatoire.");
        }

        return normalise;
    }
}

/// <summary>Partie 1 : comptage par conteneur physique. Entre dans le Stock fin.</summary>
public sealed class InventaireConteneur : LigneComptageInventaire
{
    private InventaireConteneur()
    {
    }

    private InventaireConteneur(
        Guid id,
        DateOnly periode,
        DateOnly dateInventaire,
        string numeroConteneur,
        string? numeroOF,
        string? codeArticle,
        string? designationArticle,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc)
        : base(id, periode, typeMatiereId, quantiteKg, observation, creeParUtilisateurId, creeParNom, creeLeUtc)
    {
        DateInventaire = dateInventaire;
        NumeroConteneur = NormaliserObligatoire(numeroConteneur, "Le numero de conteneur");
        NumeroOF = Normaliser(numeroOF)?.ToUpperInvariant();
        CodeArticle = Normaliser(codeArticle)?.ToUpperInvariant();
        DesignationArticle = Normaliser(designationArticle);
    }

    public DateOnly DateInventaire { get; private set; }
    public string NumeroConteneur { get; private set; } = string.Empty;
    public string? NumeroOF { get; private set; }
    public string? CodeArticle { get; private set; }
    public string? DesignationArticle { get; private set; }

    public static InventaireConteneur Creer(
        DateOnly dateInventaire,
        string numeroConteneur,
        string? numeroOF,
        string? codeArticle,
        string? designationArticle,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            dateInventaire,
            dateInventaire,
            numeroConteneur,
            numeroOF,
            codeArticle,
            designationArticle,
            typeMatiereId,
            quantiteKg,
            observation,
            creeParUtilisateurId,
            creeParNom,
            creeLeUtc);

    public void Modifier(
        DateOnly dateInventaire,
        string numeroConteneur,
        string? numeroOF,
        string? codeArticle,
        string? designationArticle,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? modifieParUtilisateurId,
        string? modifieParNom,
        DateTime modifieLeUtc)
    {
        AppliquerModification(dateInventaire, typeMatiereId, quantiteKg, observation, modifieParUtilisateurId, modifieParNom, modifieLeUtc);
        DateInventaire = dateInventaire;
        NumeroConteneur = NormaliserObligatoire(numeroConteneur, "Le numero de conteneur");
        NumeroOF = Normaliser(numeroOF)?.ToUpperInvariant();
        CodeArticle = Normaliser(codeArticle)?.ToUpperInvariant();
        DesignationArticle = Normaliser(designationArticle);
    }
}

/// <summary>Partie 2 : comptage par categorie. Entre dans le Stock fin.</summary>
public sealed class InventaireCategorieComptage : LigneComptageInventaire
{
    private InventaireCategorieComptage()
    {
    }

    private InventaireCategorieComptage(
        Guid id,
        DateOnly periode,
        string categorie,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc)
        : base(id, periode, typeMatiereId, quantiteKg, observation, creeParUtilisateurId, creeParNom, creeLeUtc)
        => Categorie = NormaliserObligatoire(categorie, "La categorie");

    public string Categorie { get; private set; } = string.Empty;

    public static InventaireCategorieComptage Creer(
        DateOnly periode,
        string categorie,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), periode, categorie, typeMatiereId, quantiteKg, observation, creeParUtilisateurId, creeParNom, creeLeUtc);

    public void Modifier(
        DateOnly periode,
        string categorie,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? modifieParUtilisateurId,
        string? modifieParNom,
        DateTime modifieLeUtc)
    {
        AppliquerModification(periode, typeMatiereId, quantiteKg, observation, modifieParUtilisateurId, modifieParNom, modifieLeUtc);
        Categorie = NormaliserObligatoire(categorie, "La categorie");
    }
}

/// <summary>
/// Partie 3 : comptage des fours. TRACE et visible dans le rapport d'inventaire,
/// mais EXCLU de tout bilan et du Stock fin.
/// </summary>
public sealed class InventaireFourComptage : LigneComptageInventaire
{
    private InventaireFourComptage()
    {
    }

    private InventaireFourComptage(
        Guid id,
        DateOnly periode,
        DateOnly dateInventaire,
        string codeFour,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc)
        : base(id, periode, typeMatiereId, quantiteKg, observation, creeParUtilisateurId, creeParNom, creeLeUtc)
    {
        DateInventaire = dateInventaire;
        CodeFour = NormaliserObligatoire(codeFour, "Le four");
    }

    public DateOnly DateInventaire { get; private set; }
    public string CodeFour { get; private set; } = string.Empty;

    /// <summary>Invariant metier : un four n'entre jamais dans le Stock fin.</summary>
    public const bool InclusDansBilanFonderie = false;

    public static InventaireFourComptage Creer(
        DateOnly dateInventaire,
        string codeFour,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            dateInventaire,
            dateInventaire,
            codeFour,
            typeMatiereId,
            quantiteKg,
            observation,
            creeParUtilisateurId,
            creeParNom,
            creeLeUtc);

    public void Modifier(
        DateOnly dateInventaire,
        string codeFour,
        Guid typeMatiereId,
        decimal quantiteKg,
        string? observation,
        Guid? modifieParUtilisateurId,
        string? modifieParNom,
        DateTime modifieLeUtc)
    {
        AppliquerModification(dateInventaire, typeMatiereId, quantiteKg, observation, modifieParUtilisateurId, modifieParNom, modifieLeUtc);
        DateInventaire = dateInventaire;
        CodeFour = NormaliserObligatoire(codeFour, "Le four");
    }
}
