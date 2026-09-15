using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Contracts.Dashboard;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(
        string periode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Orchestre les projections métier existantes. Aucun écart n'est recalculé ici :
/// le service conserve les valeurs, statuts et unités fournis par chaque bilan.
/// </summary>
public sealed class DashboardService(
    IBilanFonderieService bilanFonderieService,
    IBilanUsinage2Service bilanUsinage2Service,
    IBilanUap2Service bilanUap2Service,
    IDashboardReadRepository readRepository,
    IDateTimeProvider dateTimeProvider) : IDashboardService
{
    public async Task<DashboardResponse> GetAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        var premierJour = ParsePeriode(periode);

        // Tous ces services et repositories partagent le même DbContext scoped.
        // Les appels sont donc volontairement séquentiels.
        var fonderie = await bilanFonderieService.GetAsync(periode, cancellationToken);
        var usinage2 = await bilanUsinage2Service.GetAsync(periode, cancellationToken);
        var uap2 = await bilanUap2Service.GetAsync(periode, cancellationToken);
        var operationsData = await readRepository.GetOperationsAsync(premierJour, cancellationToken);
        var importsData = await readRepository.GetImportsAsync(premierJour, cancellationToken);

        var resumeFonderie = ConstruireFonderie(fonderie);
        var detailFonderie = ConstruireDetailFonderie(fonderie);
        var resumeUsinage2 = ConstruireUsinage2(usinage2);
        var resumeUap2 = ConstruireUap2(uap2);

        var conversionsImpossibles = fonderie.Lignes.Sum(ligne => ligne.Anomalies.Count(
            anomalie => anomalie.StartsWith(
                BilanFonderieService.AnomalieSortiesUnites,
                StringComparison.OrdinalIgnoreCase)));
        var donneesManquantes = resumeFonderie.NombreDonneesManquantes
            + resumeUsinage2.NombreDonneesManquantes
            + resumeUap2.NombreDonneesManquantes;
        var anomalies = resumeFonderie.NombreAnomalies
            + resumeUsinage2.NombreAnomalies
            + resumeUap2.NombreAnomalies
            + importsData.NombreAnomalies;

        var qualiteStatut = DeterminerStatutQualite(
            anomalies,
            donneesManquantes,
            importsData.Echoues,
            resumeFonderie,
            resumeUsinage2,
            resumeUap2);
        var qualite = new DashboardDataQualitySummary(
            anomalies,
            donneesManquantes,
            conversionsImpossibles,
            usinage2.NombreReferencesManquantes,
            qualiteStatut);

        var operations = new DashboardOperationsSummary(
            operationsData.InventairesBrouillon,
            operationsData.InventairesValides,
            usinage2.NombreOfNonReceptionnes,
            operationsData.ReceptionsValidees,
            null,
            usinage2.NombreReferencesManquantes);
        var imports = new DashboardImportSummary(
            importsData.Reussis,
            importsData.Partiels,
            importsData.Echoues,
            importsData.FichiersIdentiques,
            importsData.DernierImportUtc);

        var avertissements = ConstruireAvertissements(
            resumeFonderie,
            resumeUsinage2,
            resumeUap2,
            conversionsImpossibles,
            usinage2.NombreReferencesManquantes,
            importsData);
        var statutGlobal = DeterminerStatutGlobal(
            [resumeFonderie.Statut, resumeUsinage2.Statut, resumeUap2.Statut, qualite.StatutGlobal]);

        return new DashboardResponse(
            periode,
            dateTimeProvider.UtcNow,
            statutGlobal,
            resumeFonderie,
            detailFonderie,
            resumeUsinage2,
            resumeUap2,
            operations,
            imports,
            qualite,
            avertissements);
    }

    private static DashboardFonderieSummary ConstruireDetailFonderie(BilanFonderieResponse bilan)
    {
        var types = bilan.Lignes.Select(ligne => new DashboardFonderieTypeSummary(
            ligne.CodeTypeMatiere,
            ligne.StockDebutKg,
            ligne.SortiesMagasinX3Kg,
            ligne.InputKg,
            ligne.StockFinKg,
            ligne.DechetsFonderieKg,
            ligne.ReceptionsUsinage2Kg,
            ligne.OutputKg,
            ligne.EcartKg,
            ligne.TauxEcartPourcent,
            ligne.Anomalies,
            ligne.Statut)).ToArray();
        var totalInput = bilan.TotalInputKg;
        var totalOutput = bilan.TotalOutputKg;
        var totalEcart = bilan.TotalEcartKg;
        decimal? taux = totalInput is null || totalEcart is null || totalInput == 0m
            ? null
            : Math.Round(totalEcart.Value / totalInput.Value * 100m, 2, MidpointRounding.AwayFromZero);

        return new DashboardFonderieSummary(
            totalInput,
            totalOutput,
            totalEcart,
            taux,
            types.Count(type => type.Statut == "Complet" && type.Anomalies.Count == 0),
            types.Count(type => type.Statut != "Complet" || type.Anomalies.Count > 0),
            types);
    }

    private static DashboardBilanSummary ConstruireFonderie(BilanFonderieResponse bilan)
    {
        var statut = bilan.NombreLignes switch
        {
            0 => DashboardStatuts.NonDisponible,
            _ when bilan.TotalEcartKg is null || bilan.NombreLignesDonneesManquantes > 0 => DashboardStatuts.Partiel,
            _ when bilan.NombreAnomalies > 0 => DashboardStatuts.AVerifier,
            _ => DashboardStatuts.Ok
        };

        return new DashboardBilanSummary(
            "FONDERIE",
            bilan.NombreLignes == 0 ? null : bilan.TotalEcartKg,
            "kg",
            bilan.NombreLignes,
            null,
            bilan.Lignes.Count(ligne => ligne.Statut == "Complet" && ligne.Anomalies.Count == 0),
            bilan.Lignes.Count(ligne => ligne.EcartKg is not null && ligne.Anomalies.Count > 0),
            bilan.NombreAnomalies,
            bilan.NombreLignesDonneesManquantes,
            statut);
    }

    private static DashboardBilanSummary ConstruireUsinage2(BilanUsinage2Response bilan)
    {
        var statut = bilan.NombreLignes switch
        {
            0 => DashboardStatuts.NonDisponible,
            _ when bilan.TotalBilanUsinage2Pieces is null || bilan.NombreLignesDonneesManquantes > 0
                => DashboardStatuts.Partiel,
            _ when bilan.NombreLignesAVerifier > 0 || bilan.NombreAnomalies > 0 => DashboardStatuts.AVerifier,
            _ => DashboardStatuts.Ok
        };

        return new DashboardBilanSummary(
            "USINAGE2",
            bilan.NombreLignes == 0 ? null : bilan.TotalBilanUsinage2Pieces,
            "pieces",
            bilan.NombreLignes,
            bilan.NombreOf,
            bilan.NombreLignesOk,
            bilan.NombreLignesAVerifier,
            bilan.NombreAnomalies,
            bilan.NombreLignesDonneesManquantes,
            statut);
    }

    private static DashboardBilanSummary ConstruireUap2(BilanUap2Response bilan)
    {
        var nombreOk = bilan.Lignes.Count(ligne => ligne.Statut == "OK");
        var nombreAVerifier = bilan.Lignes.Count(ligne => ligne.Statut == "AVerifier");
        var statut = bilan.NombreLignes switch
        {
            0 => DashboardStatuts.NonDisponible,
            _ when bilan.NombreLignesDonneesManquantes > 0 => DashboardStatuts.Partiel,
            _ when nombreAVerifier > 0 || bilan.NombreAnomalies > 0 => DashboardStatuts.AVerifier,
            _ => DashboardStatuts.Ok
        };

        return new DashboardBilanSummary(
            "UAP2",
            bilan.NombreLignes == 0 ? null : bilan.TotalEcartUap2Pieces,
            "pieces",
            bilan.NombreLignes,
            bilan.Lignes.Select(ligne => ligne.NumeroOF).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            nombreOk,
            nombreAVerifier,
            bilan.NombreAnomalies,
            bilan.NombreLignesDonneesManquantes,
            statut);
    }

    private static string DeterminerStatutQualite(
        int anomalies,
        int donneesManquantes,
        int importsEchoues,
        params DashboardBilanSummary[] bilans)
    {
        if (importsEchoues > 0) return DashboardStatuts.Anomalie;
        if (donneesManquantes > 0) return DashboardStatuts.Partiel;
        if (anomalies > 0) return DashboardStatuts.AVerifier;
        return bilans.All(bilan => bilan.Statut == DashboardStatuts.NonDisponible)
            ? DashboardStatuts.NonDisponible
            : DashboardStatuts.Ok;
    }

    /// <summary>
    /// Priorité technique documentée : aucune source, Anomalie, Partiel,
    /// À vérifier, puis OK. Une source manquante ne peut donc pas être masquée
    /// par une simple demande de vérification.
    /// </summary>
    internal static string DeterminerStatutGlobal(IReadOnlyCollection<string> statuts)
    {
        if (statuts.All(statut => statut == DashboardStatuts.NonDisponible))
        {
            return DashboardStatuts.NonDisponible;
        }

        if (statuts.Contains(DashboardStatuts.Anomalie)) return DashboardStatuts.Anomalie;
        if (statuts.Contains(DashboardStatuts.Partiel) || statuts.Contains(DashboardStatuts.NonDisponible))
        {
            return DashboardStatuts.Partiel;
        }

        if (statuts.Contains(DashboardStatuts.AVerifier)) return DashboardStatuts.AVerifier;
        return DashboardStatuts.Ok;
    }

    private static IReadOnlyCollection<DashboardWarning> ConstruireAvertissements(
        DashboardBilanSummary fonderie,
        DashboardBilanSummary usinage2,
        DashboardBilanSummary uap2,
        int conversionsImpossibles,
        int referencesManquantes,
        DashboardImportsData imports)
    {
        var avertissements = new List<DashboardWarning>();
        AjouterAvertissementBilan(avertissements, fonderie, "Bilan Fonderie");
        AjouterAvertissementBilan(avertissements, usinage2, "Bilan Usinage 2");
        AjouterAvertissementBilan(avertissements, uap2, "Bilan UAP2");

        if (conversionsImpossibles > 0)
        {
            avertissements.Add(new DashboardWarning(
                "SORTIES_X3_NON_CONVERTIBLES",
                "Fonderie",
                $"{conversionsImpossibles} ligne(s) X3 non exprimée(s) en kg ont été exclues des totaux.",
                DashboardStatuts.AVerifier));
        }

        if (referencesManquantes > 0)
        {
            avertissements.Add(new DashboardWarning(
                "REFERENCES_OF_MANQUANTES",
                "Usinage2",
                $"{referencesManquantes} OF ne disposent pas d'une référence article exploitable.",
                DashboardStatuts.Partiel));
        }

        if (imports.Echoues > 0)
        {
            avertissements.Add(new DashboardWarning(
                "IMPORTS_ECHOUES",
                "Imports",
                $"{imports.Echoues} import(s) ont échoué sur la période.",
                DashboardStatuts.Anomalie));
        }

        if (imports.EnCours > 0)
        {
            avertissements.Add(new DashboardWarning(
                "IMPORTS_EN_COURS",
                "Imports",
                $"{imports.EnCours} import(s) sont encore en cours.",
                DashboardStatuts.AVerifier));
        }

        avertissements.Add(new DashboardWarning(
            "CONTROLES_POIDS_AMBIGUS_NON_AGREGES",
            "ControlesPoids",
            "Le nombre de contrôles poids ambigus n'est pas disponible sous forme d'agrégat mensuel.",
            DashboardStatuts.NonDisponible));

        return avertissements;
    }

    private static void AjouterAvertissementBilan(
        ICollection<DashboardWarning> avertissements,
        DashboardBilanSummary bilan,
        string libelle)
    {
        if (bilan.Statut == DashboardStatuts.Ok) return;

        var message = bilan.Statut switch
        {
            DashboardStatuts.NonDisponible => $"{libelle} ne contient aucune source exploitable pour la période.",
            DashboardStatuts.Partiel => $"{libelle} est partiel : des données nécessaires sont manquantes.",
            DashboardStatuts.Anomalie => $"{libelle} contient une anomalie.",
            _ => $"{libelle} contient des lignes à vérifier."
        };
        avertissements.Add(new DashboardWarning(
            $"{bilan.Code}_STATUT_{bilan.Statut.ToUpperInvariant()}",
            bilan.Code,
            message,
            bilan.Statut));
    }

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
                new ValidationFailure("periode", "La période doit respecter le format YYYY-MM.")
            ]);
        }

        return new DateOnly(parsed.Year, parsed.Month, 1);
    }
}
