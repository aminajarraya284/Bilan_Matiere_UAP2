using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts.ReceptionsUsinage2;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;

namespace BilanMatiereUAP2.Application.Features.ReceptionsUsinage2;

public interface IAlerteNonReceptionService
{
    Task<AlerteNonReceptionResponse> GetAsync(DateOnly date, int page, int pageSize, CancellationToken cancellationToken = default);
}

/// <summary>
/// Alerte JOURNALIERE des pieces non receptionnees, en PIECES, une ligne par Numero OF.
///
/// L'univers des lignes vient des « Sorties MP par ordre de fabrication » de la date
/// selectionnee : ni YRJMCH, ni les receptions, ni les mouvements ne definissent la liste
/// des OF a traiter.
///
/// A ne pas confondre avec le Bilan Usinage 2, qui est MENSUEL et vaut
/// Receptions validees - Rebuts U2 valides - Entrees OF signees.
/// </summary>
public sealed class AlerteNonReceptionService(
    ISortieMpMaterialRepository sortieMpRepository,
    IReportMachineRepository reportMachineRepository,
    IReceptionUsinage2Repository receptionRepository) : IAlerteNonReceptionService
{
    public async Task<AlerteNonReceptionResponse> GetAsync(
        DateOnly date,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var sortiesMp = await sortieMpRepository.GetByDateAsync(date, cancellationToken);
        var lignesRapport = await reportMachineRepository.GetByDateAsync(date, cancellationToken);
        var receptions = await receptionRepository.GetValidesByDateAsync(date, cancellationToken);

        // Univers : les OF imputes ce jour-la dans les Sorties MP.
        var ofDuJour = sortiesMp
            .GroupBy(lien => Normaliser(lien.NumeroOF))
            .Where(groupe => groupe.Key.Length > 0)
            .OrderBy(groupe => groupe.Key, StringComparer.Ordinal)
            .ToArray();

        // CTRL et rebut EMR par OF : le processus vient des 3 premiers caracteres du poste.
        var emrParOf = lignesRapport
            .Where(ligne => OrigineProcessusRules.DeterminerOrigineProcessus(ligne.CodePoste) == OrigineProcessus.Emr)
            .GroupBy(ligne => Normaliser(ligne.NumeroOF))
            .ToDictionary(
                groupe => groupe.Key,
                groupe => (Ctrl: groupe.Sum(l => l.QuantiteCTRL), Rebut: groupe.Sum(l => l.QuantiteRebut)),
                StringComparer.OrdinalIgnoreCase);

        var receptionsParOf = receptions
            .GroupBy(reception => Normaliser(reception.NumeroOF))
            .ToDictionary(
                groupe => groupe.Key,
                groupe => groupe.Sum(reception => reception.NombrePiecesReceptionnees),
                StringComparer.OrdinalIgnoreCase);

        var lignes = ofDuJour.Select(groupe => ConstruireLigne(date, groupe.Key, groupe, emrParOf, receptionsParOf)).ToArray();

        var total = lignes.Length;
        var pageCourante = page < 1 ? 1 : page;
        var taille = pageSize is < 1 or > 200 ? 50 : pageSize;

        return new AlerteNonReceptionResponse(
            date,
            lignes.Skip((pageCourante - 1) * taille).Take(taille).ToArray(),
            total,
            pageCourante,
            taille,
            lignes.Count(l => l.Statut is StatutAlerteNonReception.AVerifier or StatutAlerteNonReception.AnomalieSurReception),
            lignes.Count(l => l.Statut == StatutAlerteNonReception.DonneeManquante));
    }

    private static AlerteNonReceptionLigneResponse ConstruireLigne(
        DateOnly date,
        string numeroOF,
        IEnumerable<SortieMpMaterialLink> liensSortieMp,
        IReadOnlyDictionary<string, (decimal Ctrl, decimal Rebut)> emrParOf,
        IReadOnlyDictionary<string, decimal> receptionsParOf)
    {
        var anomalies = new List<string>();

        // Un OF peut porter plusieurs articles dans les Sorties MP. On ne scinde jamais la
        // ligne : le bilan fonctionnel est au niveau de l'OF, l'article reste informatif.
        var articles = liensSortieMp
            .Select(lien => Normaliser(lien.CodeArticle))
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
        var typesMatiere = liensSortieMp
            .Select(lien => Normaliser(lien.CodeTypeMatiere))
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        var plusieursArticles = articles.Length > 1;
        if (plusieursArticles) anomalies.Add(AnomalieAlerteNonReception.PlusieursArticlesPourOf);

        var codeArticle = plusieursArticles ? "Plusieurs articles" : articles.FirstOrDefault();
        var codeTypeMatiere = typesMatiere.Length == 1 ? typesMatiere[0] : typesMatiere.Length > 1 ? "Plusieurs" : null;

        // Rapport machine EMR absent : les compteurs restent nuls, jamais ramenes a zero.
        var emrTrouve = emrParOf.TryGetValue(numeroOF, out var emr);
        if (!emrTrouve) anomalies.Add(AnomalieAlerteNonReception.RapportMachineEmrIntrouvable);

        decimal? ctrlEmrPieces = emrTrouve ? emr.Ctrl : null;
        decimal? rebutEmrPieces = emrTrouve ? emr.Rebut : null;
        decimal? nombreDePieces = emrTrouve ? emr.Ctrl - emr.Rebut : null;

        var quantiteReceptionnee = receptionsParOf.TryGetValue(numeroOF, out var recue) ? recue : 0m;

        // Le signe est conserve : ni Math.Abs, ni Math.Max, ni clamp a zero.
        decimal? quantiteNonReceptionnee = nombreDePieces - quantiteReceptionnee;

        var statut = plusieursArticles
            ? StatutAlerteNonReception.AnomalieDonnees
            : quantiteNonReceptionnee switch
            {
                null => StatutAlerteNonReception.DonneeManquante,
                0 => StatutAlerteNonReception.Ok,
                > 0 => StatutAlerteNonReception.AVerifier,
                _ => StatutAlerteNonReception.AnomalieSurReception
            };

        return new AlerteNonReceptionLigneResponse(
            date,
            numeroOF,
            codeArticle,
            codeTypeMatiere,
            ctrlEmrPieces,
            rebutEmrPieces,
            nombreDePieces,
            quantiteReceptionnee,
            quantiteNonReceptionnee,
            statut,
            anomalies);
    }

    private static string Normaliser(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
}
