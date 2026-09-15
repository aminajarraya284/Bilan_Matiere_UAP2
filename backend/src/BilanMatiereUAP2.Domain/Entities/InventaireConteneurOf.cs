using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class InventaireConteneurOf
{
    private InventaireConteneurOf()
    {
    }

    private InventaireConteneurOf(
        Guid id,
        DateOnly dateInventaire,
        string numeroConteneur,
        string numeroOF,
        string codeTypeMatiere,
        decimal poidsReelKg,
        string? observation,
        DateTime creeLeUtc)
    {
        ValiderDonnees(dateInventaire, numeroConteneur, numeroOF, codeTypeMatiere, poidsReelKg, observation);

        Id = id;
        DateInventaire = dateInventaire;
        NumeroConteneur = NormaliserObligatoire(numeroConteneur);
        NumeroOF = NormaliserObligatoire(numeroOF);
        CodeTypeMatiere = NormaliserObligatoire(codeTypeMatiere);
        PoidsReelKg = poidsReelKg;
        Observation = NormaliserTexte(observation);
        Statut = StatutInventaire.Brouillon;
        CreeLeUtc = creeLeUtc;
    }

    public Guid Id { get; private set; }
    public DateOnly DateInventaire { get; private set; }
    public string NumeroConteneur { get; private set; } = string.Empty;
    public string NumeroOF { get; private set; } = string.Empty;
    public string CodeTypeMatiere { get; private set; } = string.Empty;
    public decimal PoidsReelKg { get; private set; }
    public string? Observation { get; private set; }
    public StatutInventaire Statut { get; private set; }
    public DateTime CreeLeUtc { get; private set; }
    public DateTime? ModifieLeUtc { get; private set; }
    public DateTime? ValideLeUtc { get; private set; }
    public string? ValidePar { get; private set; }
    public DateTime? AnnuleLeUtc { get; private set; }
    public string? AnnulePar { get; private set; }

    public static InventaireConteneurOf Creer(
        DateOnly dateInventaire,
        string numeroConteneur,
        string numeroOF,
        string codeTypeMatiere,
        decimal poidsReelKg,
        string? observation,
        DateTime creeLeUtc,
        Guid? id = null)
    {
        return new InventaireConteneurOf(
            id ?? Guid.NewGuid(),
            dateInventaire,
            numeroConteneur,
            numeroOF,
            codeTypeMatiere,
            poidsReelKg,
            observation,
            creeLeUtc);
    }

    public void Modifier(
        DateOnly dateInventaire,
        string numeroConteneur,
        string numeroOF,
        string codeTypeMatiere,
        decimal poidsReelKg,
        string? observation,
        DateTime modifieLeUtc)
    {
        GarantirBrouillon("modifie");
        ValiderDonnees(dateInventaire, numeroConteneur, numeroOF, codeTypeMatiere, poidsReelKg, observation);

        DateInventaire = dateInventaire;
        NumeroConteneur = NormaliserObligatoire(numeroConteneur);
        NumeroOF = NormaliserObligatoire(numeroOF);
        CodeTypeMatiere = NormaliserObligatoire(codeTypeMatiere);
        PoidsReelKg = poidsReelKg;
        Observation = NormaliserTexte(observation);
        ModifieLeUtc = modifieLeUtc;
    }

    public void Valider(string utilisateur, DateTime? valideLeUtc = null)
    {
        GarantirBrouillon("valide");
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
            throw new DomainValidationException($"Seul un inventaire conteneur Brouillon peut etre {action}.");
        }
    }

    private static void ValiderDonnees(
        DateOnly dateInventaire,
        string numeroConteneur,
        string numeroOF,
        string codeTypeMatiere,
        decimal poidsReelKg,
        string? observation)
    {
        if (dateInventaire == default)
        {
            throw new DomainValidationException("La date d'inventaire est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(numeroConteneur))
        {
            throw new DomainValidationException("Le numero de conteneur est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(numeroOF))
        {
            throw new DomainValidationException("Le numero d'OF est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(codeTypeMatiere))
        {
            throw new DomainValidationException("Le code type matiere est obligatoire.");
        }

        if (poidsReelKg < 0)
        {
            throw new DomainValidationException("Le poids reel ne peut pas etre negatif.");
        }

        if (observation?.Trim().Length > 500)
        {
            throw new DomainValidationException("L'observation ne peut pas depasser 500 caracteres.");
        }
    }

    private static string NormaliserObligatoire(string value) => value.Trim().ToUpperInvariant();

    private static string? NormaliserTexte(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
