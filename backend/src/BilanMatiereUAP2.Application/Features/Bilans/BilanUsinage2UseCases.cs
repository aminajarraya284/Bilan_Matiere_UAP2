using System.Globalization;
using System.Text;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Bilans;

public sealed record GetBilanUsinage2Query(
    string Periode,
    string? NumeroOF = null,
    string? CodeArticle = null,
    string? CodeTypeMatiere = null,
    string? Statut = null,
    bool? AvecAnomalies = null,
    int Page = 1,
    int PageSize = 50);

public interface IBilanUsinage2Service
{
    Task<BilanUsinage2Response> GetAsync(string periode, CancellationToken cancellationToken = default);
    Task<BilanUsinage2Response> QueryAsync(GetBilanUsinage2Query query, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(string periode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bilan Usinage 2 mensuel, une ligne par OF, uniquement en pièces.
/// Formule validée : Réception U2 - Rebut U2 - Entrée OF signée.
/// </summary>
public sealed class BilanUsinage2Service(
    IReceptionUsinage2Repository receptionRepository,
    IRebutUsinage2Repository rebutRepository,
    IEntreeOfRepository entreeOfRepository,
    IReportMachineRepository reportMachineRepository,
    ISortieMpMaterialRepository sortieMpRepository,
    IOfContextReferencesRepository referencesRepository,
    IParametreApplicationProvider parametres) : IBilanUsinage2Service
{
    private const string UniteEntreeOfPieces = "UN";

    private sealed class SourcesOf
    {
        public List<ReceptionUsinage2> Receptions { get; } = [];
        public List<RebutUsinage2> Rebuts { get; } = [];
        public List<EntreeOfComposant> Entrees { get; } = [];
        public List<SortieMpMaterialLink> Sorties { get; } = [];
        public List<X3RapportMachine> Rapports { get; } = [];
    }

    public Task<BilanUsinage2Response> GetAsync(
        string periode,
        CancellationToken cancellationToken = default)
        => QueryAsync(new GetBilanUsinage2Query(periode), cancellationToken);

    public async Task<BilanUsinage2Response> QueryAsync(
        GetBilanUsinage2Query query,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(query.Page, query.PageSize);
        var toutes = await ConstruireLignesAsync(query.Periode, cancellationToken);

        var numeroOf = NormaliserOptionnel(query.NumeroOF);
        var article = NormaliserOptionnel(query.CodeArticle);
        var typeMatiere = NormaliserOptionnel(query.CodeTypeMatiere);
        var statut = query.Statut?.Trim();

        var filtrees = toutes
            .Where(ligne => numeroOf is null || ligne.NumeroOF.Contains(numeroOf, StringComparison.OrdinalIgnoreCase))
            .Where(ligne => article is null || ligne.CodeArticle.Contains(article, StringComparison.OrdinalIgnoreCase))
            .Where(ligne => typeMatiere is null || string.Equals(ligne.TypeMatiere, typeMatiere, StringComparison.OrdinalIgnoreCase))
            .Where(ligne => string.IsNullOrWhiteSpace(statut) || string.Equals(ligne.Statut, statut, StringComparison.OrdinalIgnoreCase))
            .Where(ligne => query.AvecAnomalies is null || (ligne.Anomalies.Count > 0) == query.AvecAnomalies.Value)
            .ToArray();

        var pageItems = filtrees
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();
        var bilansConnus = filtrees
            .Where(ligne => ligne.BilanUsinage2Pieces is not null)
            .Select(ligne => ligne.BilanUsinage2Pieces!.Value)
            .ToArray();

        return new BilanUsinage2Response(
            query.Periode,
            pageItems,
            filtrees.Sum(ligne => ligne.QuantiteReceptionneePieces),
            filtrees.Length,
            filtrees.Count(ligne => ligne.Statut == "Incomplet"),
            filtrees.Sum(ligne => ligne.RebutUsinage2Pieces),
            filtrees.Where(ligne => ligne.EntreeOfPieces is not null).Sum(ligne => ligne.EntreeOfPieces!.Value),
            bilansConnus.Length == filtrees.Length ? bilansConnus.Sum() : null,
            filtrees.Sum(ligne => ligne.Anomalies.Count),
            filtrees.Length,
            query.Page,
            query.PageSize);
    }

    public async Task<byte[]> ExportCsvAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        var lignes = await ConstruireLignesAsync(periode, cancellationToken);
        if (lignes.Any(ligne => ligne.BilanUsinage2Pieces is null))
        {
            throw new ConflictException(
                "L'export est indisponible tant que le Bilan Usinage 2 contient des données manquantes.");
        }

        var sb = new StringBuilder();
        sb.AppendLine("NumeroOF;CodeArticle;DesignationArticle;CodeAtelier;TypeMatiere;ReceptionU2Pieces;RebutU2Pieces;EntreeOFSigneePieces;BilanU2Pieces;Statut;Anomalies");
        foreach (var ligne in lignes)
        {
            sb.AppendLine(string.Join(';',
                ligne.NumeroOF,
                ligne.CodeArticle,
                ligne.DesignationArticle ?? string.Empty,
                ligne.CodeAtelier ?? string.Empty,
                ligne.TypeMatiere ?? string.Empty,
                DecimalCsv(ligne.QuantiteReceptionneePieces),
                DecimalCsv(ligne.RebutUsinage2Pieces),
                DecimalCsv(ligne.EntreeOfPieces),
                DecimalCsv(ligne.BilanUsinage2Pieces),
                ligne.Statut,
                string.Join(" | ", ligne.Anomalies)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<IReadOnlyList<BilanUsinage2LigneResponse>> ConstruireLignesAsync(
        string periode,
        CancellationToken cancellationToken)
    {
        var (annee, mois) = ParsePeriode(periode);
        var receptions = await receptionRepository.GetToutesValidesByPeriodeAsync(annee, mois, cancellationToken);
        var rebuts = await rebutRepository.GetValidesByPeriodeAsync(annee, mois, cancellationToken);
        var entrees = await entreeOfRepository.GetByPeriodeAsync(annee, mois, cancellationToken);
        var sorties = await sortieMpRepository.GetAllByPeriodeAsync(annee, mois, cancellationToken);
        var rapports = await reportMachineRepository.GetByPeriodeAsync(annee, mois, cancellationToken);

        var sourcesParOf = new Dictionary<string, SourcesOf>(StringComparer.OrdinalIgnoreCase);
        SourcesOf Source(string numeroOf)
        {
            var cle = BilanMetierRules.NormaliserCle(numeroOf);
            if (!sourcesParOf.TryGetValue(cle, out var source))
            {
                source = new SourcesOf();
                sourcesParOf.Add(cle, source);
            }

            return source;
        }

        // Seules les réceptions définissent l'univers des OF du Bilan Usinage 2.
        // Règle métier : un OF n'existe dans le bilan que s'il a au moins une
        // Réception U2 validée sur la période. Les rebuts, entrées OF et sorties MP
        // ENRICHISSENT uniquement les OF déjà présents ; ils ne peuvent pas en créer
        // de nouveaux. C'est le même principe que pour les rapports machine (ci-dessous).
        foreach (var reception in receptions) Source(reception.NumeroOF).Receptions.Add(reception);

        foreach (var rebut in rebuts)
        {
            var cle = BilanMetierRules.NormaliserCle(rebut.NumeroOF);
            if (sourcesParOf.TryGetValue(cle, out var src)) src.Rebuts.Add(rebut);
        }

        foreach (var entree in entrees)
        {
            var cle = BilanMetierRules.NormaliserCle(entree.NumeroOF);
            if (sourcesParOf.TryGetValue(cle, out var src)) src.Entrees.Add(entree);
        }

        foreach (var sortie in sorties)
        {
            var cle = BilanMetierRules.NormaliserCle(sortie.NumeroOF);
            if (sourcesParOf.TryGetValue(cle, out var src)) src.Sorties.Add(sortie);
        }

        // YRJMCH enrichit les lignes déjà dans l'univers mais ne crée pas d'OF à lui seul.
        foreach (var rapport in rapports)
        {
            var cle = BilanMetierRules.NormaliserCle(rapport.NumeroOF);
            if (sourcesParOf.TryGetValue(cle, out var source))
            {
                source.Rapports.Add(rapport);
            }
        }

        sourcesParOf.Remove(string.Empty);

        var articles = sourcesParOf.Values
            .SelectMany(Articles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var gammes = await referencesRepository.GetGammesActivesByArticlesAsync(articles, cancellationToken);
        var codeAtelierUap2 = BilanMetierRules.NormaliserCle(await parametres.GetValueAsync(
            ParametresApplicationCles.CodeAtelierUap2,
            cancellationToken));
        var articlesEligibles = gammes
            .Where(gamme => string.Equals(
                BilanMetierRules.NormaliserCle(gamme.CodeAtelier),
                codeAtelierUap2,
                StringComparison.OrdinalIgnoreCase))
            .Select(gamme => BilanMetierRules.NormaliserCle(gamme.CodeArticle))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return sourcesParOf
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => ConstruireLigne(
                periode,
                item.Key,
                item.Value,
                articlesEligibles,
                codeAtelierUap2))
            .ToArray();
    }

    private static BilanUsinage2LigneResponse ConstruireLigne(
        string periode,
        string numeroOf,
        SourcesOf sources,
        IReadOnlySet<string> articlesEligibles,
        string codeAtelierUap2)
    {
        var anomalies = new List<string>();
        var articles = Articles(sources)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var article = articles.Length == 1 ? articles[0] : string.Empty;
        if (articles.Length == 0) anomalies.Add("ARTICLE_OF_INTROUVABLE");
        if (articles.Length > 1) anomalies.Add("PLUSIEURS_ARTICLES_POUR_OF");

        var eligible = article.Length > 0 && articlesEligibles.Contains(article);
        if (codeAtelierUap2.Length == 0)
        {
            anomalies.Add($"PARAMETRE_APPLICATION_MANQUANT:{ParametresApplicationCles.CodeAtelierUap2}");
        }
        else if (!eligible)
        {
            anomalies.Add("HORS_ATELIER_UAP2");
        }

        var receptionsEligibles = sources.Receptions
            .Where(reception => reception.EligibleAtelier16)
            .ToArray();
        if (sources.Receptions.Count > receptionsEligibles.Length)
        {
            anomalies.Add("RECEPTION_U2_HORS_ATELIER_EXCLUE");
        }

        var receptionU2 = receptionsEligibles.Sum(reception => reception.NombrePiecesReceptionnees);
        var rebutU2 = eligible
            ? sources.Rebuts.Sum(rebut => rebut.QuantiteRebutPieces)
            : 0m;

        decimal? entreeOf = null;
        if (sources.Entrees.Count == 0)
        {
            anomalies.Add("ENTREE_OF_INTROUVABLE");
        }
        else
        {
            var unites = sources.Entrees
                .Select(entree => BilanMetierRules.NormaliserCle(entree.Unite))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (unites.Length != 1 || !string.Equals(unites[0], UniteEntreeOfPieces, StringComparison.OrdinalIgnoreCase))
            {
                anomalies.Add("ENTREE_OF_UNITE_INCOMPATIBLE");
            }
            else
            {
                entreeOf = sources.Entrees.Sum(entree => entree.QuantiteNetteSignee);
            }
        }

        decimal? bilan = eligible && article.Length > 0 && entreeOf is not null
            ? BilanMetierRules.CalculerBilanUsinage2(receptionU2, rebutU2, entreeOf.Value)
            : null;

        var rapportsEmr = sources.Rapports
            .Where(rapport => OrigineProcessusRules.DeterminerOrigineProcessus(rapport.CodePoste) == OrigineProcessus.Emr)
            .ToArray();
        decimal? totalEmr = rapportsEmr.Length > 0 ? rapportsEmr.Sum(rapport => rapport.QuantiteTotale) : null;
        decimal? rebutEmr = rapportsEmr.Length > 0 ? rapportsEmr.Sum(rapport => rapport.QuantiteRebut) : null;
        decimal? netEmr = totalEmr - rebutEmr;
        decimal? nonReception = netEmr - receptionU2;

        var designation = ValeurUnique(
            sources.Receptions.Select(reception => reception.DesignationArticle)
                .Concat(sources.Entrees.Select(entree => entree.DesignationArticle))
                .Concat(sources.Rapports.Select(rapport => rapport.DesignationArticle)),
            "DESIGNATION_ARTICLE_AMBIGUE",
            anomalies);
        // Type matiere : la Sortie MP fait seule autorite, comme dans OfContextResolver.
        // Le snapshot fige sur la reception n'est PAS unionne ici : il produirait une fausse
        // ambiguite des que le referentiel a evolue depuis la saisie.
        var typesSortie = sources.Sorties
            .Select(sortie => BilanMetierRules.NormaliserCle(sortie.CodeTypeMatiere))
            .Where(valeur => valeur.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string? typeMatiere = typesSortie.Length == 1 ? typesSortie[0] : null;
        if (typesSortie.Length > 1)
        {
            anomalies.Add("TYPE_MATIERE_SORTIE_MP_AMBIGUE");
        }
        else if (typesSortie.Length == 0)
        {
            anomalies.Add("TYPE_MATIERE_SORTIE_MP_INTROUVABLE");
        }

        var poidsPhysique = ValeurUnique(
            receptionsEligibles.Select(reception => reception.PoidsPhysiqueUnitaireKg),
            "POIDS_PHYSIQUE_AMBIGU",
            anomalies);
        var poidsTheorique = ValeurUnique(
            receptionsEligibles.Select(reception => reception.PoidsTheoriqueUnitaireKg),
            "POIDS_THEORIQUE_AMBIGU",
            anomalies);
        var poidsTotal = SommeNullable(receptionsEligibles.Select(reception => reception.PoidsTotalReceptionneKg));
        var ecartPoids = poidsPhysique - poidsTheorique;
        var tauxPoids = BilanMetierRules.CalculerTaux(ecartPoids, poidsTheorique, valeurAbsolueNumerateur: true);

        var statut = bilan switch
        {
            null => eligible ? "Incomplet" : "ExcluAtelier",
            0 => "OK",
            _ => "AVerifier"
        };

        return new BilanUsinage2LigneResponse(
            numeroOf,
            article,
            designation,
            eligible ? codeAtelierUap2 : null,
            typeMatiere,
            periode,
            totalEmr,
            rebutEmr,
            netEmr,
            receptionU2,
            nonReception,
            rebutU2,
            entreeOf,
            bilan,
            BilanMetierRules.CalculerTaux(receptionU2, netEmr),
            BilanMetierRules.CalculerTaux(nonReception, netEmr),
            poidsTotal,
            poidsPhysique,
            poidsTheorique,
            ecartPoids,
            tauxPoids,
            BilanMetierRules.DeterminerStatutNonReception(nonReception),
            poidsPhysique is null || poidsTheorique is null ? "DonneesManquantes" : "Trouve",
            anomalies.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            eligible,
            statut);
    }

    private static IEnumerable<string> Articles(SourcesOf sources)
        => sources.Receptions.Select(reception => reception.CodeArticle)
            .Concat(sources.Rebuts.Select(rebut => rebut.CodeArticle))
            .Concat(sources.Entrees.Select(entree => entree.CodeArticle))
            .Concat(sources.Sorties.Select(sortie => sortie.CodeArticle))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(BilanMetierRules.NormaliserCle);

    internal static (int Annee, int Mois) ParsePeriode(string periode)
    {
        if (!DateTime.TryParseExact(periode, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw Validation("periode", "La période doit respecter le format YYYY-MM.");
        }

        return (parsed.Year, parsed.Month);
    }

    private static string? ValeurUnique(
        IEnumerable<string?> values,
        string anomalieAmbiguite,
        ICollection<string> anomalies)
    {
        var distinct = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (distinct.Length > 1) anomalies.Add(anomalieAmbiguite);
        return distinct.Length == 1 ? distinct[0] : null;
    }

    private static decimal? ValeurUnique(
        IEnumerable<decimal?> values,
        string anomalieAmbiguite,
        ICollection<string> anomalies)
    {
        var distinct = values
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .Distinct()
            .ToArray();
        if (distinct.Length > 1) anomalies.Add(anomalieAmbiguite);
        return distinct.Length == 1 ? distinct[0] : null;
    }

    private static decimal? SommeNullable(IEnumerable<decimal?> values)
    {
        var connues = values.Where(value => value is not null).Select(value => value!.Value).ToArray();
        return connues.Length == 0 ? null : connues.Sum();
    }

    private static string? NormaliserOptionnel(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : BilanMetierRules.NormaliserCle(value);

    private static string DecimalCsv(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1) throw Validation("page", "La page doit être supérieure ou égale à 1.");
        if (pageSize is < 1 or > 200) throw Validation("pageSize", "La taille de page doit être comprise entre 1 et 200.");
    }

    private static ValidationException Validation(string propertyName, string message)
        => new([new ValidationFailure(propertyName, message)]);
}
