using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Domain.Services;

/// <summary>Total d'inventaire d'un type matiere sur une periode, en kilogrammes.</summary>
/// <param name="TypeMatiereId">Type matiere inventorie.</param>
/// <param name="TotalConteneursKg">Partie 1 : somme des conteneurs VALIDES.</param>
/// <param name="TotalCategoriesKg">Partie 2 : somme des categories VALIDEES.</param>
/// <param name="TotalFoursKg">Partie 3 : somme des fours VALIDES, hors bilan.</param>
public sealed record TotalInventaireParMatiere(
    Guid TypeMatiereId,
    decimal TotalConteneursKg,
    decimal TotalCategoriesKg,
    decimal TotalFoursKg)
{
    /// <summary>
    /// Stock FIN de la periode : parties 1 + 2 uniquement.
    /// Les fours sont volontairement exclus (inventaire four hors bilan).
    /// </summary>
    public decimal StockFinKg => TotalConteneursKg + TotalCategoriesKg;

    /// <summary>
    /// Total affiche dans le RAPPORT d'inventaire : les trois parties.
    /// Il differe volontairement de <see cref="StockFinKg"/>, qui alimente le bilan.
    /// </summary>
    public decimal TotalRapportKg => TotalConteneursKg + TotalCategoriesKg + TotalFoursKg;
}

/// <summary>
/// Regles d'agregation du comptage d'inventaire Fonderie.
///
/// REGLE ARBITREE : le comptage physique constitue le STOCK FIN de la periode.
///   StockFin(periode, matiere) = somme des conteneurs VALIDES
///                              + somme des categories VALIDEES
///   Les fours sont EXCLUS du stock (mais restent dans le rapport).
///   StockDebut(periode) = StockFin(periode precedente).
///
/// Aucune ligne Brouillon ou Annulee n'entre dans un total : une saisie non validee
/// ne doit jamais deplacer un stock.
/// </summary>
public static class InventaireComptageRules
{
    /// <summary>Periode precedente, dont le Stock fin devient le Stock debut courant.</summary>
    public static DateOnly PeriodePrecedente(DateOnly periode)
        => new DateOnly(periode.Year, periode.Month, 1).AddMonths(-1);

    public static IReadOnlyList<TotalInventaireParMatiere> CalculerTotaux(
        IEnumerable<InventaireConteneur> conteneurs,
        IEnumerable<InventaireCategorieComptage> categories,
        IEnumerable<InventaireFourComptage> fours)
    {
        var totaux = new Dictionary<Guid, (decimal Conteneurs, decimal Categories, decimal Fours)>();

        void Cumuler(Guid typeMatiereId, decimal conteneur, decimal categorie, decimal four)
        {
            totaux.TryGetValue(typeMatiereId, out var courant);
            totaux[typeMatiereId] = (
                courant.Conteneurs + conteneur,
                courant.Categories + categorie,
                courant.Fours + four);
        }

        foreach (var ligne in conteneurs.Where(l => l.EstRetenuePourLeStock))
        {
            Cumuler(ligne.TypeMatiereId, ligne.QuantiteKg, 0m, 0m);
        }

        foreach (var ligne in categories.Where(l => l.EstRetenuePourLeStock))
        {
            Cumuler(ligne.TypeMatiereId, 0m, ligne.QuantiteKg, 0m);
        }

        foreach (var ligne in fours.Where(l => l.EstRetenuePourLeStock))
        {
            Cumuler(ligne.TypeMatiereId, 0m, 0m, ligne.QuantiteKg);
        }

        return totaux
            .Select(item => new TotalInventaireParMatiere(item.Key, item.Value.Conteneurs, item.Value.Categories, item.Value.Fours))
            .OrderBy(total => total.TypeMatiereId)
            .ToArray();
    }
}
