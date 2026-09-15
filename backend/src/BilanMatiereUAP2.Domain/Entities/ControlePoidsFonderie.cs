using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class ControlePoidsFonderie
{
    private ControlePoidsFonderie()
    {
    }

    private ControlePoidsFonderie(
        Guid id,
        DateOnly dateControle,
        string? numeroOF,
        string codeArticle,
        Guid typeMatiereId,
        string? numeroLotOuSerie,
        decimal poidsPhysiqueUnitaireKg,
        int? nombreEchantillons,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc)
    {
        Valider(dateControle, codeArticle, typeMatiereId, poidsPhysiqueUnitaireKg, nombreEchantillons, observation);

        Id = id;
        DateControle = dateControle;
        NumeroOF = NormaliserTexte(numeroOF);
        CodeArticle = NormaliserCode(codeArticle);
        TypeMatiereId = typeMatiereId;
        NumeroLotOuSerie = NormaliserTexte(numeroLotOuSerie);
        PoidsPhysiqueUnitaireKg = poidsPhysiqueUnitaireKg;
        NombreEchantillons = nombreEchantillons;
        Observation = NormaliserTexte(observation);
        Statut = StatutControlePoidsFonderie.Brouillon;
        CreeParUtilisateurId = creeParUtilisateurId;
        CreeParNom = NormaliserTexte(creeParNom);
        CreeLeUtc = creeLeUtc;
    }

    public Guid Id { get; private set; }
    public DateOnly DateControle { get; private set; }
    public string? NumeroOF { get; private set; }
    public string CodeArticle { get; private set; } = string.Empty;
    public Guid TypeMatiereId { get; private set; }
    public TypeMatiere TypeMatiere { get; private set; } = null!;
    public string? NumeroLotOuSerie { get; private set; }
    public decimal PoidsPhysiqueUnitaireKg { get; private set; }
    public int? NombreEchantillons { get; private set; }
    public string? Observation { get; private set; }
    public StatutControlePoidsFonderie Statut { get; private set; }
    public Guid? CreeParUtilisateurId { get; private set; }
    public string? CreeParNom { get; private set; }
    public DateTime CreeLeUtc { get; private set; }
    public Guid? ValideParUtilisateurId { get; private set; }
    public string? ValideParNom { get; private set; }
    public DateTime? ValideLeUtc { get; private set; }
    public DateTime? ModifieLeUtc { get; private set; }
    public bool EstSupprime { get; private set; }

    public static ControlePoidsFonderie Creer(
        DateOnly dateControle,
        string? numeroOF,
        string codeArticle,
        Guid typeMatiereId,
        string? numeroLotOuSerie,
        decimal poidsPhysiqueUnitaireKg,
        int? nombreEchantillons,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc,
        Guid? id = null)
    {
        return new ControlePoidsFonderie(
            id ?? Guid.NewGuid(),
            dateControle,
            numeroOF,
            codeArticle,
            typeMatiereId,
            numeroLotOuSerie,
            poidsPhysiqueUnitaireKg,
            nombreEchantillons,
            observation,
            creeParUtilisateurId,
            creeParNom,
            creeLeUtc);
    }

    public void Modifier(
        DateOnly dateControle,
        string? numeroOF,
        string codeArticle,
        Guid typeMatiereId,
        string? numeroLotOuSerie,
        decimal poidsPhysiqueUnitaireKg,
        int? nombreEchantillons,
        string? observation,
        DateTime modifieLeUtc)
    {
        if (Statut != StatutControlePoidsFonderie.Brouillon)
        {
            throw new DomainValidationException("Seul un contrôle poids brouillon peut être modifié.");
        }

        Valider(dateControle, codeArticle, typeMatiereId, poidsPhysiqueUnitaireKg, nombreEchantillons, observation);
        DateControle = dateControle;
        NumeroOF = NormaliserTexte(numeroOF);
        CodeArticle = NormaliserCode(codeArticle);
        TypeMatiereId = typeMatiereId;
        NumeroLotOuSerie = NormaliserTexte(numeroLotOuSerie);
        PoidsPhysiqueUnitaireKg = poidsPhysiqueUnitaireKg;
        NombreEchantillons = nombreEchantillons;
        Observation = NormaliserTexte(observation);
        ModifieLeUtc = modifieLeUtc;
    }

    public void Valider(Guid? valideParUtilisateurId, string? valideParNom, DateTime valideLeUtc)
    {
        if (Statut == StatutControlePoidsFonderie.Annule)
        {
            throw new DomainValidationException("Un contrôle poids annulé ne peut pas être validé.");
        }

        Statut = StatutControlePoidsFonderie.Valide;
        ValideParUtilisateurId = valideParUtilisateurId;
        ValideParNom = NormaliserTexte(valideParNom);
        ValideLeUtc = valideLeUtc;
        ModifieLeUtc = valideLeUtc;
    }

    public void Annuler(DateTime annuleLeUtc)
    {
        if (Statut == StatutControlePoidsFonderie.Annule)
        {
            return;
        }

        Statut = StatutControlePoidsFonderie.Annule;
        ModifieLeUtc = annuleLeUtc;
    }

    public void SupprimerLogiquement(DateTime supprimeLeUtc)
    {
        if (Statut == StatutControlePoidsFonderie.Valide)
        {
            throw new DomainValidationException("Un contrôle poids validé ne peut pas être supprimé.");
        }

        EstSupprime = true;
        Statut = StatutControlePoidsFonderie.Annule;
        ModifieLeUtc = supprimeLeUtc;
    }

    private static void Valider(
        DateOnly dateControle,
        string codeArticle,
        Guid typeMatiereId,
        decimal poidsPhysiqueUnitaireKg,
        int? nombreEchantillons,
        string? observation)
    {
        if (dateControle == default)
        {
            throw new DomainValidationException("La date de contrôle est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(codeArticle))
        {
            throw new DomainValidationException("Le code article est obligatoire.");
        }

        if (typeMatiereId == Guid.Empty)
        {
            throw new DomainValidationException("Le type matière est obligatoire.");
        }

        if (poidsPhysiqueUnitaireKg <= 0)
        {
            throw new DomainValidationException("Le poids physique unitaire doit être strictement positif.");
        }

        if (nombreEchantillons is <= 0)
        {
            throw new DomainValidationException("Le nombre d'échantillons doit être strictement positif.");
        }

        if (observation?.Trim().Length > 500)
        {
            throw new DomainValidationException("L'observation ne peut pas dépasser 500 caractères.");
        }
    }

    private static string NormaliserCode(string value) => value.Trim().ToUpperInvariant();

    private static string? NormaliserTexte(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
