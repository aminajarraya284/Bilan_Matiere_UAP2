using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class InventaireCategorie
{
    private InventaireCategorie()
    {
    }

    private InventaireCategorie(
        Guid id,
        DateOnly dateInventaire,
        CategorieInventaireFonderie categorie,
        string codeTypeMatiere,
        decimal poidsReelKg,
        string? commentaire,
        DateTime creeLeUtc)
    {
        ValiderDonnees(dateInventaire, categorie, codeTypeMatiere, poidsReelKg, commentaire);

        Id = id;
        DateInventaire = dateInventaire;
        Categorie = categorie;
        CodeTypeMatiere = NormaliserObligatoire(codeTypeMatiere);
        PoidsReelKg = poidsReelKg;
        Commentaire = NormaliserTexte(commentaire);
        Statut = StatutInventaire.Brouillon;
        CreeLeUtc = creeLeUtc;
    }

    public Guid Id { get; private set; }
    public DateOnly DateInventaire { get; private set; }
    public CategorieInventaireFonderie Categorie { get; private set; }
    public string CodeTypeMatiere { get; private set; } = string.Empty;
    public decimal PoidsReelKg { get; private set; }
    public string? Commentaire { get; private set; }
    public StatutInventaire Statut { get; private set; }
    public DateTime CreeLeUtc { get; private set; }
    public DateTime? ModifieLeUtc { get; private set; }
    public DateTime? ValideLeUtc { get; private set; }
    public string? ValidePar { get; private set; }
    public DateTime? AnnuleLeUtc { get; private set; }
    public string? AnnulePar { get; private set; }

    public bool InclusBilanFonderie => true;
    public bool VisibleRapportInventaire => true;

    public static InventaireCategorie Creer(
        DateOnly dateInventaire,
        CategorieInventaireFonderie categorie,
        string codeTypeMatiere,
        decimal poidsReelKg,
        string? commentaire,
        DateTime creeLeUtc,
        Guid? id = null)
    {
        return new InventaireCategorie(
            id ?? Guid.NewGuid(),
            dateInventaire,
            categorie,
            codeTypeMatiere,
            poidsReelKg,
            commentaire,
            creeLeUtc);
    }

    public void Modifier(
        DateOnly dateInventaire,
        CategorieInventaireFonderie categorie,
        string codeTypeMatiere,
        decimal poidsReelKg,
        string? commentaire,
        DateTime modifieLeUtc)
    {
        GarantirBrouillon("modifiee");
        ValiderDonnees(dateInventaire, categorie, codeTypeMatiere, poidsReelKg, commentaire);

        DateInventaire = dateInventaire;
        Categorie = categorie;
        CodeTypeMatiere = NormaliserObligatoire(codeTypeMatiere);
        PoidsReelKg = poidsReelKg;
        Commentaire = NormaliserTexte(commentaire);
        ModifieLeUtc = modifieLeUtc;
    }

    public void Valider(string utilisateur, DateTime? valideLeUtc = null)
    {
        GarantirBrouillon("validee");
        if (string.IsNullOrWhiteSpace(utilisateur))
        {
            throw new DomainValidationException("L'utilisateur ayant valide la ligne est obligatoire.");
        }

        Statut = StatutInventaire.Valide;
        ValidePar = utilisateur.Trim();
        ValideLeUtc = valideLeUtc ?? DateTime.UtcNow;
        ModifieLeUtc = ValideLeUtc;
    }

    public void Annuler(string utilisateur, DateTime? annuleLeUtc = null)
    {
        if (Statut == StatutInventaire.Annule)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(utilisateur))
        {
            throw new DomainValidationException("L'utilisateur ayant annule la ligne est obligatoire.");
        }

        Statut = StatutInventaire.Annule;
        AnnulePar = utilisateur.Trim();
        AnnuleLeUtc = annuleLeUtc ?? DateTime.UtcNow;
        ModifieLeUtc = AnnuleLeUtc;
    }

    private void GarantirBrouillon(string action)
    {
        if (Statut != StatutInventaire.Brouillon)
        {
            throw new DomainValidationException($"Seule une categorie d'inventaire Brouillon peut etre {action}.");
        }
    }

    private static void ValiderDonnees(
        DateOnly dateInventaire,
        CategorieInventaireFonderie categorie,
        string codeTypeMatiere,
        decimal poidsReelKg,
        string? commentaire)
    {
        if (dateInventaire == default)
        {
            throw new DomainValidationException("La date d'inventaire est obligatoire.");
        }

        if (!Enum.IsDefined(categorie))
        {
            throw new DomainValidationException("La categorie d'inventaire est invalide.");
        }

        if (string.IsNullOrWhiteSpace(codeTypeMatiere))
        {
            throw new DomainValidationException("Le code type matiere est obligatoire.");
        }

        if (poidsReelKg < 0)
        {
            throw new DomainValidationException("Le poids reel ne peut pas etre negatif.");
        }

        if (commentaire?.Trim().Length > 500)
        {
            throw new DomainValidationException("Le commentaire ne peut pas depasser 500 caracteres.");
        }
    }

    private static string NormaliserObligatoire(string value) => value.Trim().ToUpperInvariant();

    private static string? NormaliserTexte(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
