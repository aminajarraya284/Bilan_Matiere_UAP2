using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Application.Options;
using Microsoft.Extensions.Options;

namespace BilanMatiereUAP2.Application.Services;

public sealed class ReceptionUsinage2ContextService(
    IX3ImportRepository x3Repository,
    IManufacturingReferencesRepository referencesRepository,
    ITypeMatiereRepository typeMatiereRepository,
    IReportMachineService reportMachineService,
    IControlePoidsFonderieLookupService controlePoidsService,
    IReceptionUsinage2Repository receptionRepository,
    IOptions<ManufacturingReferencesOptions> options) : IReceptionUsinage2ContextService
{
    public async Task<ReceptionU2ContextResponse?> GetAsync(string numeroOF, DateOnly dateReception, CancellationToken cancellationToken = default)
    {
        var normalizedOF = numeroOF.Trim().ToUpperInvariant();
        var order = await x3Repository.GetOrdreContexteAsync(normalizedOF, cancellationToken);
        if (order is null) return null;

        var warnings = new List<string>();
        var article = order.ArticlePrincipal?.Trim().ToUpperInvariant();
        var articleStatus = string.IsNullOrWhiteSpace(article)
            ? order.StatutArticle == "PlusieursArticlesPrincipaux" ? "ArticleAmbigu" : "ArticleIntrouvable"
            : "Trouve";
        if (string.IsNullOrWhiteSpace(article))
        {
            warnings.Add(articleStatus == "ArticleAmbigu"
                ? "Plusieurs articles distincts sont associés à cet OF."
                : "Aucun article unique n'est disponible pour cet OF.");
            return new ReceptionU2ContextResponse(
                order.NumeroOF,
                null,
                order.DesignationPrincipale,
                order.SituationOF,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                0,
                0,
                articleStatus,
                "AtelierIntrouvable",
                "MouvementAbsent",
                "NomenclatureIntrouvable",
                "PoidsPhysiqueManquant",
                "RapportMachineIntrouvable",
                warnings);
        }

        var numeroOperation = options.Value.NumeroOperationOrigineUsinage2.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var gammes = (await referencesRepository.GetGammesActivesByArticleAsync(article, numeroOperation, cancellationToken))
            .Where(gamme => gamme.EstValideLe(dateReception))
            .ToArray();
        var distinctAteliers = gammes.Select(gamme => gamme.CodeAtelier).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var atelierStatus = distinctAteliers.Length switch
        {
            0 => "AtelierIntrouvable",
            1 => "Trouve",
            _ => "AtelierAmbigu"
        };
        string? atelier = distinctAteliers.Length == 1 ? distinctAteliers[0] : null;
        if (atelierStatus != "Trouve") warnings.Add(atelierStatus == "AtelierAmbigu" ? "Plusieurs ateliers distincts sont possibles." : "Aucune gamme valide ne correspond à l'opération U2 configurée.");

        var activeTypes = await typeMatiereRepository.GetAllAsync(true, cancellationToken);
        var activeTypeCodes = activeTypes.Select(type => type.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var materialGroups = order.Consommations
            .Where(consumption => activeTypeCodes.Contains(consumption.CodeTypeMatiere))
            .GroupBy(consumption => consumption.CodeTypeMatiere, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var materialStatus = materialGroups.Length switch
        {
            0 => "MouvementAbsent",
            1 => "Trouve",
            _ => "MatiereAmbigue"
        };
        string? codeTypeMatiere = materialGroups.Length == 1 ? materialGroups[0].Key : null;
        if (materialGroups.Length == 1)
        {
            var movementUnits = materialGroups[0]
                .Select(consumption => consumption.Unite.Trim().ToUpperInvariant())
                .Distinct()
                .ToArray();
            if (movementUnits.Length != 1 || ConversionKg(movementUnits[0]) is null)
            {
                materialStatus = "UniteIncompatible";
            }
            else if (materialGroups[0].Sum(consumption => consumption.SommeQuantiteX3Signee) == 0)
            {
                materialStatus = "QuantiteNetteNulle";
            }
        }
        if (materialStatus != "Trouve") warnings.Add($"Statut matière : {materialStatus}.");

        decimal? quantiteLien = null;
        string? uniteQuantiteLien = null;
        decimal? poidsTheoriqueUnitaireKg = null;
        var nomenclatureStatus = "NomenclatureIntrouvable";
        if (materialStatus == "Trouve" && codeTypeMatiere is not null)
        {
            var nomenclatures = await referencesRepository.GetNomenclaturesByArticleAndComponentAsync(article, codeTypeMatiere, cancellationToken);
            var validQuantities = nomenclatures.Where(n => n.QuantiteLien is not null).ToArray();
            var distinctQuantities = validQuantities.Select(n => n.QuantiteLien!.Value).Distinct().ToArray();
            if (distinctQuantities.Length == 0)
            {
                nomenclatureStatus = "NomenclatureIntrouvable";
            }
            else if (distinctQuantities.Length > 1)
            {
                nomenclatureStatus = "NomenclatureAmbigue";
            }
            else
            {
                var units = validQuantities.Select(n => n.UniteQuantite?.Trim().ToUpperInvariant() ?? string.Empty).Distinct().ToArray();
                var unit = units.Single();
                var factor = ConversionKg(unit);
                if (units.Length != 1 || factor is null)
                {
                    nomenclatureStatus = "UniteIncompatible";
                }
                else
                {
                    nomenclatureStatus = "Trouve";
                    quantiteLien = distinctQuantities[0];
                    uniteQuantiteLien = unit;
                    poidsTheoriqueUnitaireKg = quantiteLien * factor.Value;
                }
            }
        }
        if (nomenclatureStatus != "Trouve") warnings.Add($"Statut nomenclature : {nomenclatureStatus}.");

        var physicalWeight = materialStatus == "Trouve" && codeTypeMatiere is not null
            ? await controlePoidsService.ResolvePoidsPhysiqueAsync(normalizedOF, article, codeTypeMatiere, dateReception, cancellationToken)
            : new ControlePoidsFonderieLookupResult("PoidsPhysiqueManquant", null, null, null, [], null);
        if (physicalWeight.Statut != "Trouve") warnings.Add("Le contrôle poids Fonderie validé est indisponible.");

        var report = await reportMachineService.AggregateAsync(normalizedOF, article, cancellationToken: cancellationToken);
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

        // Quantite non receptionnee = CTRL EMR - pieces receptionnees U2 (receptions non annulees
        // du meme mois, OF et article). Ne jamais additionner SCI + EMR + TRN : seule l'origine EMR
        // alimente cette comparaison.
        var receptionsDuMois = await receptionRepository.GetValidesByOfAndArticleAsync(
            normalizedOF, article, dateReception.Year, dateReception.Month, cancellationToken);
        decimal? piecesReceptionneesU2 = receptionsDuMois.Count > 0
            ? receptionsDuMois.Sum(r => r.NombrePiecesReceptionnees)
            : null;
        decimal? quantiteNonReceptionnee = reportStatus == "Trouve" && piecesReceptionneesU2 is not null
            ? report.TotalCtrlEmr - report.TotalRebutEmr - piecesReceptionneesU2
            : null;

        return new ReceptionU2ContextResponse(
            order.NumeroOF,
            article,
            order.DesignationPrincipale,
            order.SituationOF,
            atelier,
            codeTypeMatiere,
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
            warnings);
    }

    private static decimal? ConversionKg(string unit)
        => unit switch
        {
            "KG" or "KGM" => 1m,
            "G" or "GR" => 0.001m,
            "T" => 1000m,
            _ => null
        };
}
