using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Bilans;

public sealed record GetBilanUap2Query(string Periode);

public interface IBilanUap2Service
{
    Task<BilanUap2Response> GetAsync(string periode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bilan matiere UAP2 mensuel, une ligne par « Numero OF + Code article ».
///
/// PARTIE 1 — sortie matiere (RM-UAP2-001), seule partie exprimee en kilogrammes :
///   QuantiteSortieMpPieces = QuantiteCtrl(H) - Σ Rebuts(I) des processus SCI, EMR, TRN
///   SortieMpKg             = QuantiteSortieMpPieces × PoidsPhysiqueUnitaireKg
///
/// PARTIE 2 — ecart (RM-UAP2-002), INTEGRALEMENT EN PIECES :
///   EcartUap2Pieces = QuantiteCtrl - (RebutSci + RebutEmr + RebutTrn + EntreeOf + RebutU2)
///   TauxEcart       = EcartUap2Pieces / QuantiteCtrl × 100
///
/// POINT CLE : « CTRL » est un code poste EXACT (OrigineProcessusRules), donc la
/// quantite de reference — pas un quatrieme processus dont les quantites s'ajouteraient
/// a SCI, EMR et TRN. Confondre les deux lectures donne des resultats sans commune mesure.
/// </summary>
public sealed class BilanUap2Service(
    IBilanUap2Repository repository,
    IParametreApplicationProvider parametres) : IBilanUap2Service
{
    /// <summary>Seuil de repli si <c>SEUIL_ECART_UAP2</c> n'est pas parametre (note SOPAL : 1,5 %).</summary>
    public const decimal SeuilEcartParDefautPourcent = 1.5m;

    public const string AnomaliePoidsPhysique = "POIDS_PHYSIQUE_NON_RESOLU";
    public const string AnomalieCtrlAbsent = "QUANTITE_CTRL_ABSENTE";
    public const string AnomalieHorsSeuil = "ECART_HORS_SEUIL";
    public const string AnomalieParametreSeuil = "PARAMETRE_APPLICATION_MANQUANT:SEUIL_ECART_UAP2";

    public async Task<BilanUap2Response> GetAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        var (annee, mois) = ParsePeriode(periode);

        var seuilParametre = await parametres.GetDecimalAsync(
            ParametresApplicationCles.SeuilEcartUap2,
            cancellationToken);
        var seuil = seuilParametre ?? SeuilEcartParDefautPourcent;

        var rapports = await repository.GetRapportsMachineAsync(annee, mois, cancellationToken);
        var entrees = await repository.GetEntreesOfAsync(annee, mois, cancellationToken);
        var rebuts = await repository.GetRebutsValidesAsync(annee, mois, cancellationToken);
        var receptions = await repository.GetReceptionsValidesAsync(annee, mois, cancellationToken);

        var cles = rapports
            .Select(rapport => new CleOf(rapport.NumeroOF, rapport.CodeArticle))
            .Concat(entrees.Select(entree => new CleOf(entree.NumeroOF, entree.CodeArticle)))
            .Concat(rebuts.Select(rebut => new CleOf(rebut.NumeroOF, rebut.CodeArticle)))
            .Select(Normaliser)
            .Distinct()
            .OrderBy(cle => cle.NumeroOF, StringComparer.OrdinalIgnoreCase)
            .ThenBy(cle => cle.CodeArticle, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var rapportsParCle = Regrouper(rapports, r => new CleOf(r.NumeroOF, r.CodeArticle));
        var entreesParCle = Regrouper(entrees, e => new CleOf(e.NumeroOF, e.CodeArticle));
        var rebutsParCle = Regrouper(rebuts, r => new CleOf(r.NumeroOF, r.CodeArticle));
        var receptionsParCle = receptions
            .GroupBy(r => Normaliser(new CleOf(r.NumeroOF, r.CodeArticle ?? string.Empty)))
            .ToDictionary(g => g.Key, g => g.ToArray());

        var lignes = cles
            .Select(cle => ConstruireLigne(
                cle,
                periode,
                rapportsParCle.GetValueOrDefault(cle) ?? [],
                entreesParCle.GetValueOrDefault(cle) ?? [],
                rebutsParCle.GetValueOrDefault(cle) ?? [],
                receptionsParCle.GetValueOrDefault(cle) ?? [],
                seuil,
                seuilParametre is null))
            .ToArray();

        return new BilanUap2Response(
            periode,
            lignes,
            lignes.Sum(ligne => ligne.QuantiteCtrlPieces),
            lignes.Sum(ligne => ligne.EcartUap2Pieces),
            // Une seule ligne sans poids physique rend le total en kg non exhaustif.
            lignes.Any(ligne => ligne.SortieMpKg is null)
                ? null
                : lignes.Sum(ligne => ligne.SortieMpKg!.Value),
            seuil,
            lignes.Length,
            lignes.Count(ligne => ligne.Anomalies.Contains(AnomalieHorsSeuil)),
            lignes.Count(ligne => ligne.Statut == "DonneesManquantes"),
            lignes.Sum(ligne => ligne.Anomalies.Count),
            lignes.Length,
            1,
            lignes.Length);
    }

    private static BilanUap2LigneResponse ConstruireLigne(
        CleOf cle,
        string periode,
        IReadOnlyList<X3RapportMachine> rapports,
        IReadOnlyList<EntreeOfComposant> entrees,
        IReadOnlyList<RebutUsinage2> rebuts,
        IReadOnlyList<ReceptionUsinage2> receptions,
        decimal seuil,
        bool seuilParDefaut)
    {
        var anomalies = new List<string>();
        if (seuilParDefaut) anomalies.Add(AnomalieParametreSeuil);

        // « CTRL » est un code poste exact : sa colonne H porte la quantite de reference.
        var quantiteCtrl = rapports
            .Where(rapport => Classifier(rapport) == OrigineProcessus.Ctrl)
            .Sum(rapport => rapport.QuantiteTotale);
        if (quantiteCtrl == 0m) anomalies.Add(AnomalieCtrlAbsent);

        // Colonne I des processus : « rebuts » en partie 1, « resultats » en partie 2,
        // c'est la meme grandeur dans la source.
        var rebutSci = SommeRebuts(rapports, OrigineProcessus.Sci);
        var rebutEmr = SommeRebuts(rapports, OrigineProcessus.Emr);
        var rebutTrn = SommeRebuts(rapports, OrigineProcessus.Trn);
        var totalRebutsProcessus = rebutSci + rebutEmr + rebutTrn;

        var quantiteSortieMpPieces = quantiteCtrl - totalRebutsProcessus;

        var poidsPhysiqueUnitaireKg = receptions
            .Select(reception => reception.PoidsPhysiqueUnitaireKg)
            .FirstOrDefault(poids => poids is not null);
        if (poidsPhysiqueUnitaireKg is null) anomalies.Add(AnomaliePoidsPhysique);

        // Sans poids unitaire identifie, la conversion est refusee, jamais approximee
        // (RM-COM-005).
        var sortieMpKg = poidsPhysiqueUnitaireKg is null
            ? (decimal?)null
            : quantiteSortieMpPieces * poidsPhysiqueUnitaireKg.Value;

        var entreeOfPieces = entrees.Sum(entree => entree.QuantiteNetteSignee);
        var rebutUsinage2Pieces = rebuts.Sum(rebut => rebut.QuantiteRebutPieces);

        // [A CONFIRMER] Le signe du Rebut U2 suit la lecture litterale de la note SOPAL :
        // il figure DANS la parenthese soustraite, au meme titre que l'Entree OF. Voir
        // docs/ReglesMetier.md, RM-UAP2-002, point ouvert.
        var ecartUap2Pieces = quantiteCtrl
            - (totalRebutsProcessus + entreeOfPieces + rebutUsinage2Pieces);

        var tauxEcartPourcent = quantiteCtrl == 0m
            ? (decimal?)null
            : Math.Round(ecartUap2Pieces / quantiteCtrl * 100m, 2, MidpointRounding.AwayFromZero);

        if (tauxEcartPourcent is not null && Math.Abs(tauxEcartPourcent.Value) > seuil)
        {
            anomalies.Add(AnomalieHorsSeuil);
        }

        return new BilanUap2LigneResponse(
            cle.NumeroOF,
            cle.CodeArticle,
            rapports.Select(r => r.DesignationArticle).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)),
            receptions.Select(r => r.CodeTypeMatiere).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)),
            periode,
            quantiteCtrl,
            rebutSci,
            rebutEmr,
            rebutTrn,
            quantiteSortieMpPieces,
            poidsPhysiqueUnitaireKg,
            sortieMpKg,
            entreeOfPieces,
            rebutUsinage2Pieces,
            ecartUap2Pieces,
            tauxEcartPourcent,
            anomalies,
            DeterminerStatut(anomalies, tauxEcartPourcent, seuil));
    }

    /// <summary>Statuts existants uniquement : aucun nouvel enumere (RM-UAP2-003).</summary>
    private static string DeterminerStatut(
        IReadOnlyList<string> anomalies,
        decimal? tauxEcartPourcent,
        decimal seuil)
    {
        if (anomalies.Contains(AnomaliePoidsPhysique) || anomalies.Contains(AnomalieCtrlAbsent))
        {
            return "DonneesManquantes";
        }

        if (tauxEcartPourcent is null) return "DonneesManquantes";
        return Math.Abs(tauxEcartPourcent.Value) > seuil ? "AVerifier" : "OK";
    }

    private static decimal SommeRebuts(IReadOnlyList<X3RapportMachine> rapports, OrigineProcessus origine)
        => rapports.Where(rapport => Classifier(rapport) == origine).Sum(rapport => rapport.QuantiteRebut);

    private static OrigineProcessus Classifier(X3RapportMachine rapport)
        => OrigineProcessusRules.DeterminerOrigineProcessus(rapport.CodePoste);

    private static Dictionary<CleOf, T[]> Regrouper<T>(IEnumerable<T> source, Func<T, CleOf> selecteur)
        => source
            .GroupBy(item => Normaliser(selecteur(item)))
            .ToDictionary(groupe => groupe.Key, groupe => groupe.ToArray());

    private static CleOf Normaliser(CleOf cle)
        => new(BilanMetierRules.NormaliserCle(cle.NumeroOF), BilanMetierRules.NormaliserCle(cle.CodeArticle));

    private static (int Annee, int Mois) ParsePeriode(string periode)
    {
        if (!DateTime.TryParseExact(
                periode,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            throw new ValidationException([
                new ValidationFailure(nameof(GetBilanUap2Query.Periode), "La période doit respecter le format YYYY-MM.")
            ]);
        }

        return (parsed.Year, parsed.Month);
    }

    private readonly record struct CleOf(string NumeroOF, string CodeArticle);
}
