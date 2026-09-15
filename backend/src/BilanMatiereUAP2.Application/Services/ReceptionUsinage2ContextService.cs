using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts;

namespace BilanMatiereUAP2.Application.Services;

public sealed class ReceptionUsinage2ContextService(
    IX3ImportRepository x3Repository,
    IManufacturingReferencesRepository referencesRepository,
    IOfContextResolver ofContextResolver,
    IReportMachineService reportMachineService,
    IControlePoidsFonderieLookupService controlePoidsService,
    IReceptionUsinage2Repository receptionRepository) : IReceptionUsinage2ContextService
{
    public async Task<ReceptionU2ContextResponse?> GetAsync(string numeroOF, DateOnly dateReception, CancellationToken cancellationToken = default)
    {
        var normalizedOF = numeroOF.Trim().ToUpperInvariant();
        var resolved = await ofContextResolver.ResolveAsync(normalizedOF, dateReception, cancellationToken);
        var order = await x3Repository.GetOrdreContexteAsync(normalizedOF, cancellationToken);
        var warnings = resolved.Anomalies.ToList();

        var article = resolved.CodeArticle;
        var articleStatus = article is not null
            ? "Trouve"
            : resolved.Anomalies.Contains("PLUSIEURS_ARTICLES_POUR_OF", StringComparer.OrdinalIgnoreCase)
                ? "ArticleAmbigu"
                : "ArticleIntrouvable";
        if (articleStatus != "Trouve")
        {
            warnings.Add(articleStatus == "ArticleAmbigu"
                ? "Plusieurs articles distincts sont associés à cet OF."
                : "Aucun article unique n'est disponible pour cet OF.");
        }

        var atelierStatus = resolved.CodeAtelier is not null
            ? "Trouve"
            : resolved.Anomalies.Contains("ATELIER_UAP2_AMBIGU", StringComparer.OrdinalIgnoreCase)
                ? "AtelierAmbigu"
                : "AtelierIntrouvable";
        if (atelierStatus != "Trouve")
        {
            warnings.Add(atelierStatus == "AtelierAmbigu"
                ? "Plusieurs groupes/ateliers UAP2 distincts sont possibles."
                : "Aucune gamme valide de l'atelier UAP2 n'est disponible.");
        }

        var materialStatus = resolved.TypeMatiere is not null && resolved.SourceTypeMatiere != SourceTypeMatiere.Inconnue
            ? "Trouve"
            : resolved.Anomalies.Any(anomaly => anomaly.Contains("AMBIGUE", StringComparison.OrdinalIgnoreCase))
                ? "MatiereAmbigue"
                : "MouvementAbsent";
        if (materialStatus != "Trouve") warnings.Add($"Statut matière : {materialStatus}.");

        decimal? quantiteLien = null;
        string? uniteQuantiteLien = null;
        decimal? poidsTheoriqueUnitaireKg = null;
        var nomenclatureStatus = "NomenclatureIntrouvable";
        if (article is not null && resolved.TypeMatiere is not null)
        {
            var nomenclatures = await referencesRepository.GetNomenclaturesByArticleAndComponentAsync(article, resolved.TypeMatiere, cancellationToken);
            var validQuantities = nomenclatures.Where(n => n.QuantiteLien is not null).ToArray();
            var distinctQuantities = validQuantities.Select(n => n.QuantiteLien!.Value).Distinct().ToArray();
            if (distinctQuantities.Length > 1)
            {
                nomenclatureStatus = "NomenclatureAmbigue";
            }
            else if (distinctQuantities.Length == 1)
            {
                var units = validQuantities.Select(n => n.UniteQuantite?.Trim().ToUpperInvariant() ?? string.Empty).Distinct().ToArray();
                quantiteLien = distinctQuantities[0];
                uniteQuantiteLien = units.Length == 1 && units[0].Length > 0 ? units[0] : null;
                if (units.Length > 1)
                {
                    nomenclatureStatus = "UniteIncompatible";
                }
                else
                {
                    // La colonne, l'unite et la conversion n'ont pas encore ete
                    // officiellement validees : aucun facteur n'est invente ici.
                    nomenclatureStatus = "RegleMetierAValider";
                }
            }
        }
        if (nomenclatureStatus != "Trouve") warnings.Add($"Statut nomenclature : {nomenclatureStatus}.");
        const string theoreticalWeightMessage = "Poids théorique indisponible — règle métier à valider";
        warnings.Add(theoreticalWeightMessage);

        var physicalWeight = materialStatus == "Trouve" && article is not null && resolved.TypeMatiere is not null
            ? await controlePoidsService.ResolvePoidsPhysiqueAsync(normalizedOF, article, resolved.TypeMatiere, dateReception, cancellationToken)
            : new ControlePoidsFonderieLookupResult("PoidsPhysiqueManquant", null, null, null, [], null);
        if (physicalWeight.Statut != "Trouve") warnings.Add("Le contrôle poids Fonderie validé est indisponible.");

        var report = article is not null
            ? await reportMachineService.AggregateAsync(normalizedOF, article, cancellationToken: cancellationToken)
            : new RapportMachineAggregateResponse(normalizedOF, string.Empty, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, null, false);
        var reportStatus = !report.Trouve
            ? "RapportMachineIntrouvable"
            : report.NombreLignes == report.NombreLignesNonMappees
                ? "PostesNonClasses"
                : "Trouve";
        if (report.Trouve && (report.TotalCtrlSci < 0 || report.TotalCtrlEmr < 0 || report.TotalCtrlTrn < 0)) reportStatus = "ValeurInvalide";
        if (reportStatus != "Trouve") warnings.Add($"Statut rapport machine : {reportStatus}.");
        if (report.Trouve && report.NombreLignesNonMappees > 0)
        {
            warnings.Add($"{report.NombreLignesNonMappees} ligne(s) rapport machine avec un préfixe de poste non classé (exclues des totaux).");
        }

        var receptionsDuMois = article is not null
            ? await receptionRepository.GetValidesByOfAndArticleAsync(normalizedOF, article, dateReception.Year, dateReception.Month, cancellationToken)
            : [];
        decimal? piecesReceptionneesU2 = receptionsDuMois.Count > 0
            ? receptionsDuMois.Sum(r => r.NombrePiecesReceptionnees)
            : null;
        decimal? quantiteNonReceptionnee = reportStatus == "Trouve" && piecesReceptionneesU2 is not null
            ? report.TotalCtrlEmr - report.TotalRebutEmr - piecesReceptionneesU2
            : null;

        return new ReceptionU2ContextResponse(
            normalizedOF,
            article,
            resolved.Designation ?? order?.DesignationPrincipale,
            order?.SituationOF,
            resolved.CodeAtelier,
            resolved.TypeMatiere,
            quantiteLien,
            uniteQuantiteLien,
            poidsTheoriqueUnitaireKg,
            null,
            physicalWeight.PoidsPhysiqueUnitaireKg,
            physicalWeight.ControlePoidsFonderieId,
            physicalWeight.Source,
            reportStatus == "Trouve" ? report.TotalCtrlSci : null,
            reportStatus == "Trouve" ? report.TotalCtrlEmr : null,
            reportStatus == "Trouve" ? report.TotalCtrlTrn : null,
            reportStatus == "Trouve" ? report.TotalRebutSci : null,
            reportStatus == "Trouve" ? report.TotalRebutEmr : null,
            reportStatus == "Trouve" ? report.TotalRebutTrn : null,
            piecesReceptionneesU2,
            quantiteNonReceptionnee,
            report.NombreLignes,
            report.NombreLignesNonMappees,
            articleStatus,
            atelierStatus,
            materialStatus,
            nomenclatureStatus,
            physicalWeight.Statut,
            reportStatus,
            warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            resolved.SourceTypeMatiere switch
            {
                SourceTypeMatiere.SortieMp => "Sortie MP",
                SourceTypeMatiere.Nomenclature => "Nomenclature",
                _ => "Non résolue"
            },
            resolved.EligibleCalculUap2,
            resolved.StatutRapprochement,
            resolved.ExisteDansYrjmch,
            resolved.ExisteDansEntreeOf,
            resolved.ExisteDansSortieMp,
            resolved.ExisteDansEncoursProduct,
            physicalWeight.Candidats.Select(candidate => new ReceptionControlePoidsCandidateResponse(
                candidate.Id,
                candidate.DateControle,
                candidate.NumeroOF,
                candidate.CodeArticle,
                candidate.CodeTypeMatiere,
                candidate.PoidsPhysiqueUnitaireKg,
                candidate.Statut)).ToArray(),
            physicalWeight.Message,
            theoreticalWeightMessage);
    }
}
