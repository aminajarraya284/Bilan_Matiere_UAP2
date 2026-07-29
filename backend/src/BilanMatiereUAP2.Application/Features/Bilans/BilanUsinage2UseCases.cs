using System.Globalization;
using System.Text;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.EntreeOf;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Bilans;

public interface IBilanUsinage2Service
{
    Task<BilanUsinage2Response> GetAsync(string periode, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(string periode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bilan Usinage 2, calcule EN PIECES pour une periode et par cle « Numero OF + Code article ».
///
/// Base de calcul : les lignes YRJMCH de la periode classees EMR (le processus vient des
/// TROIS PREMIERS caracteres du code poste, voir OrigineProcessusRules). Les cles n'ayant
/// que des receptions validees
/// sans ligne EMR sont egalement retenues, afin que la quantite totale receptionnee affichee
/// ici corresponde exactement a celle de l'ecran Receptions U2 ; elles portent alors
/// l'anomalie RAPPORT_MACHINE_EMR_INTROUVABLE.
///
/// Le controle des poids (kg, kg/piece) est calcule separement et n'entre jamais dans le
/// bilan en pieces.
/// </summary>
public sealed class BilanUsinage2Service(
    IReceptionUsinage2Repository receptionRepository,
    IRebutUsinage2Repository rebutRepository,
    IEntreeOfImportService entreeOfService,
    IReportMachineService reportMachineService,
    IReportMachineRepository reportMachineRepository,
    ISortieMpMaterialRepository sortieMpMaterialRepository) : IBilanUsinage2Service
{
    private readonly record struct CleBilan(string NumeroOF, string CodeArticle);

    public async Task<BilanUsinage2Response> GetAsync(string periode, CancellationToken cancellationToken = default)
    {
        var (annee, mois) = ParsePeriode(periode);

        var receptions = await receptionRepository.GetValidesByPeriodeAsync(annee, mois, cancellationToken);
        var rebuts = await rebutRepository.GetValidesByPeriodeAsync(annee, mois, cancellationToken);
        var lignesRapport = await reportMachineRepository.GetByPeriodeAsync(annee, mois, cancellationToken);

        // Cles portees par au moins une ligne YRJMCH classee EMR sur la periode.
        var clesEmr = lignesRapport
            .Where(ligne => OrigineProcessusRules.DeterminerOrigineProcessus(ligne.CodePoste) == OrigineProcessus.Emr)
            .Select(ligne => new CleBilan(NormaliserCle(ligne.NumeroOF), NormaliserCle(ligne.CodeArticle)))
            .ToHashSet();

        var receptionsParCle = receptions
            .Where(reception => !string.IsNullOrWhiteSpace(reception.CodeArticle))
            .GroupBy(reception => new CleBilan(NormaliserCle(reception.NumeroOF), NormaliserCle(reception.CodeArticle)))
            .ToDictionary(groupe => groupe.Key, groupe => groupe.ToArray());

        var rebutsParCle = rebuts
            .GroupBy(rebut => new CleBilan(NormaliserCle(rebut.NumeroOF), NormaliserCle(rebut.CodeArticle)))
            .ToDictionary(groupe => groupe.Key, groupe => groupe.Sum(rebut => rebut.QuantiteRebutPieces));

        var cles = clesEmr
            .Union(receptionsParCle.Keys)
            .OrderBy(cle => cle.NumeroOF, StringComparer.Ordinal)
            .ThenBy(cle => cle.CodeArticle, StringComparer.Ordinal)
            .ToArray();
        var liensMatiere = await sortieMpMaterialRepository.GetByPeriodeAsync(
            annee,
            mois,
            cles.Select(cle => new SortieMpMaterialKey(cle.NumeroOF, cle.CodeArticle)).ToArray(),
            cancellationToken);
        var typesMatiereParCle = liensMatiere
            .GroupBy(lien => new CleBilan(
                NormaliserCle(lien.NumeroOF),
                NormaliserCle(lien.CodeArticle)))
            .ToDictionary(
                groupe => groupe.Key,
                groupe => (IReadOnlyList<string>)groupe
                    .Select(lien => NormaliserCle(lien.CodeTypeMatiere))
                    .Where(code => code.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(code => code, StringComparer.Ordinal)
                    .ToArray());

        var lignes = new List<BilanUsinage2LigneResponse>();
        foreach (var cle in cles)
        {
            var anomalies = new List<string>();
            var receptionsDeLaCle = receptionsParCle.TryGetValue(cle, out var trouvees)
                ? trouvees
                : [];

            lignes.Add(await ConstruireLigneAsync(
                cle,
                periode,
                annee,
                mois,
                receptionsDeLaCle,
                rebutsParCle.TryGetValue(cle, out var rebut) ? rebut : 0m,
                typesMatiereParCle.TryGetValue(cle, out var typesMatiere)
                    ? typesMatiere
                    : [],
                anomalies,
                cancellationToken));
        }

        return new BilanUsinage2Response(
            periode,
            lignes,
            lignes.Sum(ligne => ligne.QuantiteReceptionneePieces),
            lignes.Count,
            lignes.Count(ligne =>
                ligne.StatutReception == "DonneesManquantes" ||
                ligne.StatutPoids == "DonneesManquantes"));
    }

    private async Task<BilanUsinage2LigneResponse> ConstruireLigneAsync(
        CleBilan cle,
        string periode,
        int annee,
        int mois,
        IReadOnlyList<ReceptionUsinage2> receptions,
        decimal rebutUsinage2Pieces,
        IReadOnlyList<string> typesMatiereSortieMp,
        List<string> anomalies,
        CancellationToken cancellationToken)
    {
        // --- Quantites en pieces ------------------------------------------------
        var quantiteReceptionneePieces = receptions.Sum(reception => reception.NombrePiecesReceptionnees);

        var entree = await entreeOfService.ResolveAsync(cle.NumeroOF, annee, mois, cle.CodeArticle, cancellationToken);
        if (entree.QuantiteNetteEntreeOfPieces is null)
        {
            anomalies.Add($"ENTREE_OF_{entree.Statut.ToUpperInvariant()}");
        }
        if (!string.IsNullOrWhiteSpace(entree.Message) && entree.Statut != "Trouve")
        {
            anomalies.Add(entree.Message);
        }

        // Agregat restreint a la periode : cumuler tout l'historique de l'OF face a des
        // receptions d'un seul mois comparerait deux grandeurs heterogenes.
        var rapport = await reportMachineService.AggregateAsync(cle.NumeroOF, cle.CodeArticle, annee, mois, cancellationToken);
        var rapportEmrTrouve = rapport.Trouve && rapport.NombreLignesEmr > 0;
        if (!rapportEmrTrouve) anomalies.Add("RAPPORT_MACHINE_EMR_INTROUVABLE");
        if (rapport.NombreLignesNonMappees > 0)
        {
            anomalies.Add($"POSTES_PREFIXE_INCONNU:{rapport.NombreLignesNonMappees}");
        }

        decimal? quantiteCtrlEmrPieces = rapportEmrTrouve ? rapport.TotalCtrlEmr : null;
        decimal? rebutEmrPieces = rapportEmrTrouve ? rapport.TotalRebutEmr : null;
        decimal? nombreDePieces = rapportEmrTrouve ? rapport.TotalCtrlEmr - rapport.TotalRebutEmr : null;

        // Alerte de non-reception : ni Math.Abs, ni remise a zero d'une valeur negative.
        decimal? quantiteNonReceptionneePieces = nombreDePieces - quantiteReceptionneePieces;
        var statutReception = quantiteNonReceptionneePieces switch
        {
            null => "DonneesManquantes",
            0 => "OK",
            > 0 => "AVerifier",
            _ => "Anomalie"
        };

        // Bilan Usinage 2 : entierement en pieces, le controle des poids n'y entre jamais.
        decimal? bilanUsinage2Pieces = entree.QuantiteNetteEntreeOfPieces is null
            ? null
            : quantiteReceptionneePieces - rebutUsinage2Pieces - entree.QuantiteNetteEntreeOfPieces.Value;

        // Les taux sont de simples pourcentages : ils ne participent pas au bilan en pieces.
        var tauxReceptionPourcent = Pourcentage(quantiteReceptionneePieces, nombreDePieces);
        var tauxNonReceptionPourcent = Pourcentage(quantiteNonReceptionneePieces, nombreDePieces);

        // --- Controle des poids (kg et kg/piece), bloc separe -------------------
        var designation = ValeurUnique(receptions.Select(r => r.DesignationArticle), "DESIGNATION_ARTICLE_AMBIGUE", anomalies);
        var atelier = ValeurUnique(receptions.Select(r => r.AtelierOrigine), "ATELIER_ORIGINE_AMBIGU", anomalies);
        if (atelier is null && receptions.Count > 0 && receptions.All(r => r.StatutAtelier == "AtelierIntrouvable"))
        {
            anomalies.Add("ATELIER_ORIGINE_INTROUVABLE");
        }

        var typeMatiere = ValeurUnique(
            typesMatiereSortieMp,
            "TYPE_MATIERE_SORTIE_MP_AMBIGUE",
            anomalies);
        if (typeMatiere is null && typesMatiereSortieMp.Count == 0)
        {
            anomalies.Add("TYPE_MATIERE_SORTIE_MP_INTROUVABLE");
        }

        decimal? poidsReceptionneTotalKg = receptions.Count > 0
            ? receptions.Sum(r => r.PoidsTotalReceptionneKg)
            : null;

        var poidsPhysiqueUnitaireKg = ValeurUnique(receptions.Select(r => r.PoidsPhysiqueUnitaireKg), "POIDS_PHYSIQUE_AMBIGU", anomalies);
        var poidsTheoriqueUnitaireKg = ValeurUnique(receptions.Select(r => r.PoidsTheoriqueUnitaireKg), "POIDS_THEORIQUE_AMBIGU", anomalies);
        if (poidsPhysiqueUnitaireKg is null && receptions.Count > 0) anomalies.Add("POIDS_PHYSIQUE_MANQUANT");
        if (poidsTheoriqueUnitaireKg is null && receptions.Count > 0) anomalies.Add("POIDS_THEORIQUE_INTROUVABLE");

        decimal? ecartPoidsUnitaireKg = poidsPhysiqueUnitaireKg - poidsTheoriqueUnitaireKg;
        decimal? tauxEcartPoidsPourcent = ecartPoidsUnitaireKg is not null && poidsTheoriqueUnitaireKg is > 0
            ? Math.Abs(ecartPoidsUnitaireKg.Value) / poidsTheoriqueUnitaireKg.Value * 100m
            : null;
        var statutPoids = poidsPhysiqueUnitaireKg is null || poidsTheoriqueUnitaireKg is null
            ? "DonneesManquantes"
            : "Trouve";

        if (receptions.Count == 0) anomalies.Add("AUCUNE_RECEPTION_U2_VALIDEE");

        return new BilanUsinage2LigneResponse(
            cle.NumeroOF,
            cle.CodeArticle,
            designation,
            atelier,
            typeMatiere,
            periode,
            quantiteCtrlEmrPieces,
            rebutEmrPieces,
            nombreDePieces,
            quantiteReceptionneePieces,
            quantiteNonReceptionneePieces,
            rebutUsinage2Pieces,
            entree.QuantiteNetteEntreeOfPieces,
            bilanUsinage2Pieces,
            tauxReceptionPourcent,
            tauxNonReceptionPourcent,
            poidsReceptionneTotalKg,
            poidsPhysiqueUnitaireKg,
            poidsTheoriqueUnitaireKg,
            ecartPoidsUnitaireKg,
            tauxEcartPoidsPourcent,
            statutReception,
            statutPoids,
            anomalies.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    /// <summary>Pourcentage protege : un denominateur nul ou absent rend le taux non calculable.</summary>
    private static decimal? Pourcentage(decimal? numerateur, decimal? denominateur)
        => numerateur is null || denominateur is null || denominateur.Value == 0m
            ? null
            : numerateur.Value / denominateur.Value * 100m;

    public async Task<byte[]> ExportCsvAsync(string periode, CancellationToken cancellationToken = default)
    {
        var bilan = await GetAsync(periode, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine(
            "NumeroOF;CodeArticle;DesignationArticle;AtelierOrigine;TypeMatiere;Periode;" +
            "QuantiteCtrlEmrPieces;RebutEmrPieces;NombreDePieces;QuantiteReceptionneePieces;" +
            "QuantiteNonReceptionneePieces;RebutUsinage2Pieces;EntreeOfPieces;BilanUsinage2Pieces;" +
            "TauxReceptionPourcent;TauxNonReceptionPourcent;" +
            "PoidsReceptionneTotalKg;PoidsPhysiqueUnitaireKg;PoidsTheoriqueUnitaireKg;EcartPoidsUnitaireKg;TauxEcartPoidsPourcent;" +
            "StatutReception;StatutPoids;Anomalies");
        foreach (var ligne in bilan.Lignes)
        {
            sb.AppendLine(string.Join(';',
                ligne.NumeroOF,
                ligne.CodeArticle,
                ligne.DesignationArticle ?? string.Empty,
                ligne.AtelierOrigine ?? string.Empty,
                ligne.TypeMatiere ?? string.Empty,
                ligne.Periode,
                DecimalCsv(ligne.QuantiteCtrlEmrPieces),
                DecimalCsv(ligne.RebutEmrPieces),
                DecimalCsv(ligne.NombreDePieces),
                DecimalCsv(ligne.QuantiteReceptionneePieces),
                DecimalCsv(ligne.QuantiteNonReceptionneePieces),
                DecimalCsv(ligne.RebutUsinage2Pieces),
                DecimalCsv(ligne.EntreeOfPieces),
                DecimalCsv(ligne.BilanUsinage2Pieces),
                DecimalCsv(ligne.TauxReceptionPourcent),
                DecimalCsv(ligne.TauxNonReceptionPourcent),
                DecimalCsv(ligne.PoidsReceptionneTotalKg),
                DecimalCsv(ligne.PoidsPhysiqueUnitaireKg),
                DecimalCsv(ligne.PoidsTheoriqueUnitaireKg),
                DecimalCsv(ligne.EcartPoidsUnitaireKg),
                DecimalCsv(ligne.TauxEcartPoidsPourcent),
                ligne.StatutReception,
                ligne.StatutPoids,
                string.Join(" | ", ligne.Anomalies)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    internal static (int Annee, int Mois) ParsePeriode(string periode)
    {
        if (!DateTime.TryParseExact(periode, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ValidationException([new ValidationFailure("periode", "La période doit respecter le format YYYY-MM.")]);
        }

        return (parsed.Year, parsed.Month);
    }

    private static string NormaliserCle(string? value)
        => value?.Trim().ToUpperInvariant() ?? string.Empty;

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

    private static string DecimalCsv(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
}
