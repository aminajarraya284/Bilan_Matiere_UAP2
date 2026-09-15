using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class InventaireFour
{
    private InventaireFour()
    {
    }

    private InventaireFour(
        Guid id,
        DateOnly dateInventaire,
        string codeFour,
        string codeTypeMatiere,
        decimal quantiteKg,
        string? observation,
        DateTime creeLeUtc)
    {
        ValiderDonnees(dateInventaire, codeFour, codeTypeMatiere, quantiteKg, observation);

        Id = id;
        DateInventaire = dateInventaire;
        Mois = dateInventaire.Month;
        Annee = dateInventaire.Year;
        CodeFour = NormaliserObligatoire(codeFour);
        CodeTypeMatiere = NormaliserObligatoire(codeTypeMatiere);
        QuantiteKg = quantiteKg;
        Observation = NormaliserTexte(observation);
        Statut = StatutInventaire.Brouillon;
        CreeLeUtc = creeLeUtc;
    }

    public Guid Id { get; private set; }
    public DateOnly DateInventaire { get; private set; }
    public int Mois { get; private set; }
    public int Annee { get; private set; }
    public string CodeFour { get; private set; } = string.Empty;
    public string CodeTypeMatiere { get; private set; } = string.Empty;
    public decimal QuantiteKg { get; private set; }
    public string? Observation { get; private set; }
    public StatutInventaire Statut { get; private set; }
    public DateTime CreeLeUtc { get; private set; }
    public DateTime? ModifieLeUtc { get; private set; }
    public DateTime? ValideLeUtc { get; private set; }
    public string? ValidePar { get; private set; }
    public DateTime? AnnuleLeUtc { get; private set; }
    public string? AnnulePar { get; private set; }

    public bool InclusBilanFonderie => false;
    public bool VisibleRapportInventaire => true;

    public static InventaireFour Creer(
        DateOnly dateInventaire,
        string codeFour,
        string codeTypeMatiere,
        decimal quantiteKg,
        string? observation,
        DateTime creeLeUtc,
        Guid? id = null)
    {
        return new InventaireFour(
            id ?? Guid.NewGuid(),
            dateInventaire,
            codeFour,
            codeTypeMatiere,
            quantiteKg,
            observation,
            creeLeUtc);
    }

    public void Modifier(
        DateOnly dateInventaire,
        string codeFour,
        string codeTypeMatiere,
        decimal quantiteKg,
        string? observation,
        DateTime modifieLeUtc)
    {
        GarantirBrouillon("modifie");
        ValiderDonnees(dateInventaire, codeFour, codeTypeMatiere, quantiteKg, observation);

        DateInventaire = dateInventaire;
        Mois = dateInventaire.Month;
        Annee = dateInventaire.Year;
        CodeFour = NormaliserObligatoire(codeFour);
        CodeTypeMatiere = NormaliserObligatoire(codeTypeMatiere);
        QuantiteKg = quantiteKg;
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
            throw new DomainValidationException($"Seul un inventaire four Brouillon peut etre {action}.");
        }
    }

    private static void ValiderDonnees(
        DateOnly dateInventaire,
        string codeFour,
        string codeTypeMatiere,
        decimal quantiteKg,
        string? observation)
    {
        if (dateInventaire == default)
        {
            throw new DomainValidationException("La date d'inventaire est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(codeFour))
        {
            throw new DomainValidationException("Le code du four est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(codeTypeMatiere))
        {
            throw new DomainValidationException("Le code type matiere est obligatoire.");
        }

        if (quantiteKg < 0)
        {
            throw new DomainValidationException("La quantite ne peut pas etre negative.");
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
