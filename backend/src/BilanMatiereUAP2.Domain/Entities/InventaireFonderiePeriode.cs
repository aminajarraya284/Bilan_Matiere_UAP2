using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class InventaireFonderiePeriode
{
    private InventaireFonderiePeriode()
    {
    }

    private InventaireFonderiePeriode(
        Guid id,
        int mois,
        int annee,
        DateTime creeLeUtc)
    {
        ValiderDonnees(mois, annee);

        Id = id;
        Mois = mois;
        Annee = annee;
        Statut = StatutInventairePeriode.Brouillon;
        CreeLeUtc = creeLeUtc;
    }

    public Guid Id { get; private set; }
    public int Mois { get; private set; }
    public int Annee { get; private set; }
    public StatutInventairePeriode Statut { get; private set; }
    public DateTime CreeLeUtc { get; private set; }
    public DateTime? ValideLeUtc { get; private set; }
    public string? ValidePar { get; private set; }
    public DateTime? ClotureLeUtc { get; private set; }
    public string? CloturePar { get; private set; }

    public static InventaireFonderiePeriode Creer(
        int mois,
        int annee,
        DateTime creeLeUtc,
        Guid? id = null)
    {
        return new InventaireFonderiePeriode(
            id ?? Guid.NewGuid(),
            mois,
            annee,
            creeLeUtc);
    }

    public void Demarrer()
    {
        if (Statut == StatutInventairePeriode.Cloture)
        {
            throw new DomainValidationException("Une periode cloturee ne peut plus etre demarree.");
        }

        Statut = StatutInventairePeriode.EnCours;
    }

    public void Valider(string utilisateur, DateTime? valideLeUtc = null)
    {
        if (Statut == StatutInventairePeriode.Cloture)
        {
            throw new DomainValidationException("Une periode cloturee ne peut pas etre validee.");
        }

        if (string.IsNullOrWhiteSpace(utilisateur))
        {
            throw new DomainValidationException("L'utilisateur est obligatoire.");
        }

        Statut = StatutInventairePeriode.Valide;
        ValidePar = utilisateur.Trim();
        ValideLeUtc = valideLeUtc ?? DateTime.UtcNow;
    }

    public void Cloturer(string utilisateur, DateTime? clotureLeUtc = null)
    {
        if (string.IsNullOrWhiteSpace(utilisateur))
        {
            throw new DomainValidationException("L'utilisateur est obligatoire.");
        }

        Statut = StatutInventairePeriode.Cloture;
        CloturePar = utilisateur.Trim();
        ClotureLeUtc = clotureLeUtc ?? DateTime.UtcNow;
    }

    private static void ValiderDonnees(int mois, int annee)
    {
        if (mois is < 1 or > 12)
        {
            throw new DomainValidationException("Le mois doit etre compris entre 1 et 12.");
        }

        if (annee is < 2000 or > 2100)
        {
            throw new DomainValidationException("L'annee est invalide.");
        }
    }
}
