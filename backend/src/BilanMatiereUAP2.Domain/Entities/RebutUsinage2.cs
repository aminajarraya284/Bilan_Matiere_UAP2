using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

/// <summary>
/// Rebut declare a l'atelier Usinage 2, exprime en PIECES.
///
/// A ne pas confondre avec <see cref="DechetFonderie"/>, qui est un dechet Fonderie
/// exprime en kg et sans rattachement OF / article. Le Bilan Usinage 2 raisonne
/// exclusivement en pieces et par cle « Numero OF + Code article » : il lui faut donc
/// une source dediee.
///
/// Seuls les rebuts au statut <see cref="StatutRebutUsinage2.Valide"/> alimentent le bilan.
/// </summary>
public sealed class RebutUsinage2
{
    private RebutUsinage2()
    {
    }

    private RebutUsinage2(
        Guid id,
        DateOnly dateDeclaration,
        string numeroOF,
        string codeArticle,
        decimal quantiteRebutPieces,
        string? motif,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc)
    {
        Valider(dateDeclaration, numeroOF, codeArticle, quantiteRebutPieces, motif, observation);

        Id = id;
        DateDeclaration = dateDeclaration;
        Annee = dateDeclaration.Year;
        Mois = dateDeclaration.Month;
        NumeroOF = NormaliserCode(numeroOF);
        CodeArticle = NormaliserCode(codeArticle);
        QuantiteRebutPieces = quantiteRebutPieces;
        Motif = NormaliserTexte(motif);
        Observation = NormaliserTexte(observation);
        Statut = StatutRebutUsinage2.Brouillon;
        CreeParUtilisateurId = creeParUtilisateurId;
        CreeParNom = NormaliserTexte(creeParNom);
        CreeLeUtc = creeLeUtc;
    }

    public Guid Id { get; private set; }
    public DateOnly DateDeclaration { get; private set; }

    /// <summary>Annee et mois derives de <see cref="DateDeclaration"/>, pour filtrer par periode sans calcul SQL.</summary>
    public int Annee { get; private set; }
    public int Mois { get; private set; }

    public string NumeroOF { get; private set; } = string.Empty;
    public string CodeArticle { get; private set; } = string.Empty;

    /// <summary>Quantite rebutee, en PIECES (jamais en kg).</summary>
    public decimal QuantiteRebutPieces { get; private set; }

    public string? Motif { get; private set; }
    public string? Observation { get; private set; }
    public StatutRebutUsinage2 Statut { get; private set; }
    public Guid? CreeParUtilisateurId { get; private set; }
    public string? CreeParNom { get; private set; }
    public DateTime CreeLeUtc { get; private set; }
    public Guid? ValideParUtilisateurId { get; private set; }
    public string? ValideParNom { get; private set; }
    public DateTime? ValideLeUtc { get; private set; }
    public DateTime? ModifieLeUtc { get; private set; }
    public bool EstSupprime { get; private set; }

    public static RebutUsinage2 Creer(
        DateOnly dateDeclaration,
        string numeroOF,
        string codeArticle,
        decimal quantiteRebutPieces,
        string? motif,
        string? observation,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime creeLeUtc,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            dateDeclaration,
            numeroOF,
            codeArticle,
            quantiteRebutPieces,
            motif,
            observation,
            creeParUtilisateurId,
            creeParNom,
            creeLeUtc);

    public void Modifier(
        DateOnly dateDeclaration,
        string numeroOF,
        string codeArticle,
        decimal quantiteRebutPieces,
        string? motif,
        string? observation,
        DateTime modifieLeUtc)
    {
        if (Statut != StatutRebutUsinage2.Brouillon)
        {
            throw new DomainValidationException("Seul un rebut Usinage 2 brouillon peut être modifié.");
        }

        Valider(dateDeclaration, numeroOF, codeArticle, quantiteRebutPieces, motif, observation);
        DateDeclaration = dateDeclaration;
        Annee = dateDeclaration.Year;
        Mois = dateDeclaration.Month;
        NumeroOF = NormaliserCode(numeroOF);
        CodeArticle = NormaliserCode(codeArticle);
        QuantiteRebutPieces = quantiteRebutPieces;
        Motif = NormaliserTexte(motif);
        Observation = NormaliserTexte(observation);
        ModifieLeUtc = modifieLeUtc;
    }

    public void Valider(Guid? valideParUtilisateurId, string? valideParNom, DateTime valideLeUtc)
    {
        if (Statut == StatutRebutUsinage2.Annule)
        {
            throw new DomainValidationException("Un rebut Usinage 2 annulé ne peut pas être validé.");
        }

        Statut = StatutRebutUsinage2.Valide;
        ValideParUtilisateurId = valideParUtilisateurId;
        ValideParNom = NormaliserTexte(valideParNom);
        ValideLeUtc = valideLeUtc;
        ModifieLeUtc = valideLeUtc;
    }

    public void Annuler(DateTime annuleLeUtc)
    {
        if (Statut == StatutRebutUsinage2.Annule) return;

        Statut = StatutRebutUsinage2.Annule;
        ModifieLeUtc = annuleLeUtc;
    }

    private static void Valider(
        DateOnly dateDeclaration,
        string numeroOF,
        string codeArticle,
        decimal quantiteRebutPieces,
        string? motif,
        string? observation)
    {
        if (dateDeclaration == default)
        {
            throw new DomainValidationException("La date de déclaration est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(numeroOF))
        {
            throw new DomainValidationException("Le numéro d'OF est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(codeArticle))
        {
            throw new DomainValidationException("Le code article est obligatoire.");
        }

        // Un rebut nul n'apporte rien au bilan ; un rebut negatif n'a pas de sens metier.
        if (quantiteRebutPieces <= 0)
        {
            throw new DomainValidationException("La quantité rebutée doit être strictement positive et exprimée en pièces.");
        }

        if (motif?.Trim().Length > 200)
        {
            throw new DomainValidationException("Le motif ne peut pas dépasser 200 caractères.");
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
