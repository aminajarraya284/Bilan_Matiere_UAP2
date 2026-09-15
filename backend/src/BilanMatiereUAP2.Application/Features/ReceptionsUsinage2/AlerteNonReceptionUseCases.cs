using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts.ReceptionsUsinage2;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;

namespace BilanMatiereUAP2.Application.Features.ReceptionsUsinage2;

public interface IAlerteNonReceptionService
{
    Task<AlerteNonReceptionResponse> GetAsync(
        DateOnly date,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Alerte journalière, en pièces, agrégée par date et numéro d'OF.
/// Univers : YRJMCH EMR ∪ Réceptions U2 ∪ Entrées OF ∪ Sorties MP.
/// </summary>
public sealed class AlerteNonReceptionService(
    ISortieMpMaterialRepository sortieMpRepository,
    IReportMachineRepository reportMachineRepository,
    IReceptionUsinage2Repository receptionRepository,
    IEntreeOfRepository entreeOfRepository) : IAlerteNonReceptionService
{
    private sealed class OfSources
    {
        public HashSet<string> Articles { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Designations { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> TypesMatiere { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<AlerteNonReceptionResponse> GetAsync(
        DateOnly date,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pageCourante = page < 1 ? 1 : page;
        var taille = pageSize is < 1 or > 200 ? 50 : pageSize;

        // Les repositories partagent le même DbContext scoped : appels séquentiels obligatoires.
        var sortiesMp = await sortieMpRepository.GetByDateAsync(date, cancellationToken);
        var lignesRapport = await reportMachineRepository.GetByDateAsync(date, cancellationToken);
        var receptions = await receptionRepository.GetValidesByDateAsync(date, cancellationToken);
        var entreesOf = await entreeOfRepository.GetByDateAsync(date, cancellationToken);

        var lignesEmr = lignesRapport
            .Where(ligne => OrigineProcessusRules.DeterminerOrigineProcessus(ligne.CodePoste) == OrigineProcessus.Emr)
            .ToArray();

        var sourcesParOf = new Dictionary<string, OfSources>(StringComparer.OrdinalIgnoreCase);
        OfSources Source(string? numeroOf)
        {
            var cle = Normaliser(numeroOf);
            if (!sourcesParOf.TryGetValue(cle, out var source))
            {
                source = new OfSources();
                sourcesParOf.Add(cle, source);
            }

            return source;
        }

        foreach (var ligne in lignesEmr)
        {
            var source = Source(ligne.NumeroOF);
            Ajouter(source.Articles, ligne.CodeArticle);
            Ajouter(source.Designations, ligne.DesignationArticle);
        }

        foreach (var reception in receptions)
        {
            var source = Source(reception.NumeroOF);
            Ajouter(source.Articles, reception.CodeArticle);
            Ajouter(source.Designations, reception.DesignationArticle);
            Ajouter(source.TypesMatiere, reception.CodeTypeMatiere);
        }

        foreach (var entree in entreesOf)
        {
            var source = Source(entree.NumeroOF);
            Ajouter(source.Articles, entree.CodeArticle);
            Ajouter(source.Designations, entree.DesignationArticle);
        }

        foreach (var sortie in sortiesMp)
        {
            var source = Source(sortie.NumeroOF);
            Ajouter(source.Articles, sortie.CodeArticle);
            Ajouter(source.TypesMatiere, sortie.CodeTypeMatiere);
        }

        sourcesParOf.Remove(string.Empty);

        var emrParOf = lignesEmr
            .GroupBy(ligne => Normaliser(ligne.NumeroOF))
            .ToDictionary(
                groupe => groupe.Key,
                groupe => (Total: groupe.Sum(ligne => ligne.QuantiteTotale), Rebut: groupe.Sum(ligne => ligne.QuantiteRebut)),
                StringComparer.OrdinalIgnoreCase);

        var receptionsParOf = receptions
            .GroupBy(reception => Normaliser(reception.NumeroOF))
            .ToDictionary(
                groupe => groupe.Key,
                groupe => groupe.Sum(reception => reception.NombrePiecesReceptionnees),
                StringComparer.OrdinalIgnoreCase);

        var lignes = sourcesParOf
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => ConstruireLigne(
                date,
                item.Key,
                item.Value,
                emrParOf,
                receptionsParOf))
            .ToArray();

        var valeursConnues = lignes
            .Where(ligne => ligne.PiecesNonReceptionnees is not null)
            .Select(ligne => ligne.PiecesNonReceptionnees!.Value)
            .ToArray();

        return new AlerteNonReceptionResponse(
            date,
            lignes.Skip((pageCourante - 1) * taille).Take(taille).ToArray(),
            lignes.Length,
            pageCourante,
            taille,
            lignes.Count(ligne => ligne.Statut == StatutAlerteNonReception.AVerifier),
            lignes.Count(ligne => ligne.Statut == StatutAlerteNonReception.SurReception),
            lignes.Count(ligne => ligne.Statut == StatutAlerteNonReception.DonneeManquante),
            valeursConnues.Length == 0 ? null : valeursConnues.Sum());
    }

    private static AlerteNonReceptionLigneResponse ConstruireLigne(
        DateOnly date,
        string numeroOF,
        OfSources sources,
        IReadOnlyDictionary<string, (decimal Total, decimal Rebut)> emrParOf,
        IReadOnlyDictionary<string, decimal> receptionsParOf)
    {
        var anomalies = new List<string>();
        if (sources.Articles.Count > 1)
        {
            anomalies.Add(AnomalieAlerteNonReception.PlusieursArticlesPourOf);
        }

        var emrTrouve = emrParOf.TryGetValue(numeroOF, out var emr);
        if (!emrTrouve)
        {
            anomalies.Add(AnomalieAlerteNonReception.RapportMachineEmrIntrouvable);
        }

        decimal? totalEmr = emrTrouve ? emr.Total : null;
        decimal? rebutEmr = emrTrouve ? emr.Rebut : null;
        decimal? netEmr = emrTrouve ? emr.Total - emr.Rebut : null;
        var receptionU2 = receptionsParOf.TryGetValue(numeroOF, out var recue) ? recue : 0m;
        decimal? nonReceptionnees = netEmr - receptionU2;

        var statut = nonReceptionnees switch
        {
            null => StatutAlerteNonReception.DonneeManquante,
            0 => StatutAlerteNonReception.Ok,
            > 0 => StatutAlerteNonReception.AVerifier,
            _ => StatutAlerteNonReception.SurReception
        };

        return new AlerteNonReceptionLigneResponse(
            date,
            numeroOF,
            ValeurUnique(sources.Articles, "Plusieurs articles"),
            ValeurUnique(sources.Designations, null),
            ValeurUnique(sources.TypesMatiere, "Plusieurs"),
            totalEmr,
            rebutEmr,
            netEmr,
            receptionU2,
            nonReceptionnees,
            statut,
            anomalies);
    }

    private static string? ValeurUnique(IReadOnlyCollection<string> valeurs, string? valeurAmbigue)
        => valeurs.Count == 1 ? valeurs.Single() : valeurs.Count > 1 ? valeurAmbigue : null;

    private static void Ajouter(ISet<string> valeurs, string? valeur)
    {
        var normalisee = Normaliser(valeur);
        if (normalisee.Length > 0)
        {
            valeurs.Add(normalisee);
        }
    }

    private static string Normaliser(string? value)
        => BilanMetierRules.NormaliserCle(value);
}
