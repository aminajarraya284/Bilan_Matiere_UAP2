using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class InventaireFonderie
{
    private InventaireFonderie()
    {
    }

    private InventaireFonderie(
        Guid id,
        DateOnly periode,
        Guid typeMatiereId,
        decimal? stockDebutKg,
        decimal stockFinKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc)
    {
        ValiderDonnees(periode, typeMatiereId, stockDebutKg, stockFinKg, observation);
        Id = id;
        Periode = NormaliserPeriode(periode);
        TypeMatiereId = typeMatiereId;
        StockDebutKg = stockDebutKg;
        StockFinKg = stockFinKg;
        Observation = NormaliserTexte(observation);
        Statut = StatutInventaireFonderie.Brouillon;
        CreeParUtilisateurId = creeParUtilisateurId;
        CreeParNom = NormaliserTexte(creeParNom);
        CreeLeUtc = creeLeUtc;
    }

    public Guid Id { get; private set; }
    public DateOnly Periode { get; private set; }
    public Guid TypeMatiereId { get; private set; }
    public TypeMatiere TypeMatiere { get; private set; } = null!;
    /// <summary>
    /// Stock debut de la periode = stock fin de la periode PRECEDENTE.
    /// NULL lorsque cette periode precedente est inconnue : jamais un faux zero.
    /// </summary>
    public decimal? StockDebutKg { get; private set; }
    public decimal StockFinKg { get; private set; }
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
    public Guid? AnnuleParUtilisateurId { get; private set; }
    public string? AnnuleParNom { get; private set; }
    public DateTime? AnnuleLeUtc { get; private set; }

    public static InventaireFonderie Creer(
        DateOnly periode,
        Guid typeMatiereId,
        decimal? stockDebutKg,
        decimal stockFinKg,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            periode,
            typeMatiereId,
            stockDebutKg,
            stockFinKg,
            observation,
            creeParUtilisateurId,
            creeParNom,
            creeLeUtc);

    public void Modifier(
        DateOnly periode,
        Guid typeMatiereId,
        decimal? stockDebutKg,
        decimal stockFinKg,
        string? observation,
        Guid? modifieParUtilisateurId,
        string? modifieParNom,
        DateTime modifieLeUtc)
    {
        GarantirBrouillon("modifie");
        ValiderDonnees(periode, typeMatiereId, stockDebutKg, stockFinKg, observation);
        Periode = NormaliserPeriode(periode);
        TypeMatiereId = typeMatiereId;
        StockDebutKg = stockDebutKg;
        StockFinKg = stockFinKg;
        Observation = NormaliserTexte(observation);
        ModifieParUtilisateurId = modifieParUtilisateurId;
        ModifieParNom = NormaliserTexte(modifieParNom);
        ModifieLeUtc = modifieLeUtc;
    }

    /// <summary>
    /// Met à jour la projection calculée à partir du comptage physique.
    /// Une projection n'est pas une saisie manuelle : elle peut donc être recalculée
    /// même si son statut historique n'est plus Brouillon.
    /// </summary>
    public void RecalculerProjection(
        DateOnly periode,
        Guid typeMatiereId,
        decimal? stockDebutKg,
        decimal stockFinKg,
        string? observation,
        Guid? modifieParUtilisateurId,
        string? modifieParNom,
        DateTime modifieLeUtc)
    {
        ValiderDonnees(periode, typeMatiereId, stockDebutKg, stockFinKg, observation);
        Periode = NormaliserPeriode(periode);
        TypeMatiereId = typeMatiereId;
        StockDebutKg = stockDebutKg;
        StockFinKg = stockFinKg;
        Observation = NormaliserTexte(observation);
        ModifieParUtilisateurId = modifieParUtilisateurId;
        ModifieParNom = NormaliserTexte(modifieParNom);
        ModifieLeUtc = modifieLeUtc;
    }

    public void Valider(Guid? utilisateurId, string? utilisateurNom, DateTime valideLeUtc)
    {
        GarantirBrouillon("valide");
        Statut = StatutInventaireFonderie.Valide;
        ValideParUtilisateurId = utilisateurId;
        ValideParNom = NormaliserTexte(utilisateurNom);
        ValideLeUtc = valideLeUtc;
        ModifieParUtilisateurId = utilisateurId;
        ModifieParNom = NormaliserTexte(utilisateurNom);
        ModifieLeUtc = valideLeUtc;
    }

    public void Annuler(Guid? utilisateurId, string? utilisateurNom, DateTime annuleLeUtc)
    {
        if (Statut == StatutInventaireFonderie.Annule)
        {
            return;
        }

        Statut = StatutInventaireFonderie.Annule;
        AnnuleParUtilisateurId = utilisateurId;
        AnnuleParNom = NormaliserTexte(utilisateurNom);
        AnnuleLeUtc = annuleLeUtc;
        ModifieParUtilisateurId = utilisateurId;
        ModifieParNom = NormaliserTexte(utilisateurNom);
        ModifieLeUtc = annuleLeUtc;
    }

    private void GarantirBrouillon(string action)
    {
        if (Statut != StatutInventaireFonderie.Brouillon)
        {
            throw new DomainValidationException($"Seul un inventaire Brouillon peut etre {action}.");
        }
    }

    private static void ValiderDonnees(
        DateOnly periode,
        Guid typeMatiereId,
        decimal? stockDebutKg,
        decimal stockFinKg,
        string? observation)
    {
        if (periode == default)
        {
            throw new DomainValidationException("La periode est obligatoire.");
        }

        if (typeMatiereId == Guid.Empty)
        {
            throw new DomainValidationException("Le type matiere est obligatoire.");
        }

        if (stockDebutKg is < 0 || stockFinKg < 0)
        {
            throw new DomainValidationException("Les stocks debut et fin ne peuvent pas etre negatifs.");
        }

        if (observation?.Trim().Length > 500)
        {
            throw new DomainValidationException("L'observation ne peut pas depasser 500 caracteres.");
        }
    }

    private static DateOnly NormaliserPeriode(DateOnly periode) => new(periode.Year, periode.Month, 1);

    private static string? NormaliserTexte(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
