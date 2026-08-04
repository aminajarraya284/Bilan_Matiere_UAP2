using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts.Bilans;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Bilans;

public sealed record GetBilanFonderieQuery(string Periode);

public interface IBilanFonderieService
{
    Task<BilanFonderieResponse> GetAsync(string periode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bilan Fonderie mensuel, une ligne par type matiere actif, uniquement en kilogrammes.
///
/// Formule confirmee (RM-FON-003) :
///   INPUT  = StockDebut + SortiesMagasinX3
///   OUTPUT = StockFin + DechetsFonderie + ReceptionsUsinage2
///   ECART  = INPUT - OUTPUT
///
/// L'inventaire Four est exclu (RM-INV-004) : il ne participe pas au stock de fin,
/// c'est la projection d'inventaire qui applique deja cette exclusion.
/// </summary>
public sealed class BilanFonderieService(IBilanFonderieRepository repository) : IBilanFonderieService
{
    /// <summary>
    /// Seule unite directement sommable. Les consommations X3 arrivent en unites mixtes
    /// (UN, KG, BA, M, LI) : additionner sans conversion produirait un total faux, et
    /// aucune regle de conversion n'est confirmee a ce jour.
    /// </summary>
    private const string UniteKilogramme = "KG";

    public const string AnomalieStockDebut = "STOCK_DEBUT_INDISPONIBLE";
    public const string AnomalieStockFin = "STOCK_FIN_INDISPONIBLE";
    public const string AnomalieSortiesUnites = "SORTIES_X3_UNITES_NON_CONVERTIBLES";
    public const string AnomalieReceptionsPoids = "RECEPTIONS_U2_POIDS_MANQUANT";
    public const string AnomalieReceptionsTypeMatiere = "RECEPTIONS_U2_TYPE_MATIERE_NON_RESOLU";

    public async Task<BilanFonderieResponse> GetAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        var premierJour = ParsePeriode(periode);

        var types = await repository.GetTypesMatiereActifsAsync(cancellationToken);
        var stocks = (await repository.GetStocksAsync(premierJour, cancellationToken))
            .ToDictionary(stock => stock.TypeMatiereId);
        var sorties = (await repository.GetSortiesMagasinX3Async(premierJour, cancellationToken))
            .GroupBy(sortie => sortie.CodeTypeMatiere, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(groupe => groupe.Key, groupe => groupe.ToArray(), StringComparer.OrdinalIgnoreCase);
        var dechets = (await repository.GetDechetsAsync(premierJour, cancellationToken))
            .GroupBy(dechet => dechet.TypeMatiereId)
            .ToDictionary(groupe => groupe.Key, groupe => groupe.Sum(dechet => dechet.PoidsKg));
        var receptions = (await repository.GetReceptionsUsinage2Async(premierJour, cancellationToken))
            .ToDictionary(reception => reception.CodeTypeMatiere, StringComparer.OrdinalIgnoreCase);
        var receptionsSansType = await repository.CompterReceptionsSansTypeMatiereAsync(premierJour, cancellationToken);

        var lignes = types
            .Select(type => ConstruireLigne(
                type,
                periode,
                stocks.GetValueOrDefault(type.Id),
                sorties.GetValueOrDefault(type.Code) ?? [],
                dechets.GetValueOrDefault(type.Id),
                receptions.GetValueOrDefault(type.Code),
                receptionsSansType))
            .OrderBy(ligne => ligne.CodeTypeMatiere, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new BilanFonderieResponse(
            periode,
            lignes,
            TotalOuNull(lignes, ligne => ligne.InputKg),
            TotalOuNull(lignes, ligne => ligne.OutputKg),
            TotalOuNull(lignes, ligne => ligne.EcartKg),
            lignes.Length,
            lignes.Count(ligne => ligne.Statut != "Complet"),
            lignes.Sum(ligne => ligne.Anomalies.Count));
    }

    private static BilanFonderieLigneResponse ConstruireLigne(
        BilanFonderieTypeMatiere type,
        string periode,
        BilanFonderieStock? stock,
        IReadOnlyList<BilanFonderieSortieX3> sortiesDuType,
        decimal? dechetsKg,
        BilanFonderieReception? reception,
        int receptionsSansType)
    {
        var anomalies = new List<string>();

        var stockDebutKg = stock?.StockDebutKg;
        if (stockDebutKg is null) anomalies.Add(AnomalieStockDebut);

        decimal? stockFinKg = stock?.StockFinKg;
        if (stock is null) anomalies.Add(AnomalieStockFin);

        // Seules les lignes deja en kg sont sommees ; les autres sont signalees avec leur
        // unite pour que l'ecart affiche ne soit jamais pris pour un total exhaustif.
        var sortiesKg = sortiesDuType
            .Where(sortie => string.Equals(sortie.Unite, UniteKilogramme, StringComparison.OrdinalIgnoreCase))
            .Sum(sortie => sortie.Quantite);
        var unitesNonConvertibles = sortiesDuType
            .Where(sortie => !string.Equals(sortie.Unite, UniteKilogramme, StringComparison.OrdinalIgnoreCase))
            .Select(sortie => sortie.Unite)
            .Where(unite => !string.IsNullOrWhiteSpace(unite))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(unite => unite, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        decimal? sortiesMagasinX3Kg = sortiesKg;
        if (unitesNonConvertibles.Length > 0)
        {
            anomalies.Add($"{AnomalieSortiesUnites}:{string.Join(',', unitesNonConvertibles)}");
        }

        var dechetsFonderieKg = dechetsKg ?? 0m;

        decimal? receptionsKg = reception?.PoidsTotalKg ?? 0m;
        if (reception is { NombreLignesSansPoids: > 0 })
        {
            anomalies.Add($"{AnomalieReceptionsPoids}:{reception.NombreLignesSansPoids}");
        }

        if (receptionsSansType > 0)
        {
            anomalies.Add($"{AnomalieReceptionsTypeMatiere}:{receptionsSansType}");
        }

        var inputKg = Additionner(stockDebutKg, sortiesMagasinX3Kg);
        var outputKg = Additionner(Additionner(stockFinKg, dechetsFonderieKg), receptionsKg);
        var ecartKg = inputKg is null || outputKg is null ? (decimal?)null : inputKg - outputKg;
        var tauxEcartPourcent = ecartKg is null || inputKg is null || inputKg.Value == 0m
            ? (decimal?)null
            : Math.Round(ecartKg.Value / inputKg.Value * 100m, 2, MidpointRounding.AwayFromZero);

        return new BilanFonderieLigneResponse(
            type.Id,
            type.Code,
            periode,
            stockDebutKg,
            sortiesMagasinX3Kg,
            inputKg,
            stockFinKg,
            dechetsFonderieKg,
            receptionsKg,
            outputKg,
            ecartKg,
            tauxEcartPourcent,
            anomalies,
            anomalies.Count == 0 ? "Complet" : "Incomplet");
    }

    /// <summary>Une composante inconnue rend la somme inconnue : jamais de repli sur zero.</summary>
    private static decimal? Additionner(decimal? gauche, decimal? droite)
        => gauche is null || droite is null ? null : gauche + droite;

    /// <summary>Un total partiel serait lu comme un total complet : on prefere l'absence de valeur.</summary>
    private static decimal? TotalOuNull(
        IReadOnlyList<BilanFonderieLigneResponse> lignes,
        Func<BilanFonderieLigneResponse, decimal?> selecteur)
        => lignes.Any(ligne => selecteur(ligne) is null) ? null : lignes.Sum(ligne => selecteur(ligne)!.Value);

    private static DateOnly ParsePeriode(string periode)
    {
        if (!DateTime.TryParseExact(
                periode,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            throw new ValidationException([
                new ValidationFailure(nameof(GetBilanFonderieQuery.Periode), "La période doit respecter le format YYYY-MM.")
            ]);
        }

        return new DateOnly(parsed.Year, parsed.Month, 1);
    }
}
