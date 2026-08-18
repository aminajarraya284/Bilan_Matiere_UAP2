using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Alertes;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Application.Features.InventairesFonderie;
using BilanMatiereUAP2.Application.Features.ReceptionsUsinage2;
using BilanMatiereUAP2.Contracts.Alertes;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Contracts.Rapports;
using BilanMatiereUAP2.Contracts.ReceptionsUsinage2;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Rapports;

public sealed record RapportExportResult(byte[] Content, string ContentType, string FileName);

public interface IRapportsService
{
    IReadOnlyList<RapportCatalogueItemResponse> GetCatalogue();
    Task<RapportResponse<BilanFonderieResponse>> GetBilanFonderieAsync(string periode, CancellationToken cancellationToken = default);
    Task<RapportResponse<BilanUsinage2Response>> GetBilanUsinage2Async(string periode, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RapportResponse<BilanUap2Response>> GetBilanUap2Async(string periode, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RapportOfDetailResponse> GetOfAsync(string numeroOf, string periode, CancellationToken cancellationToken = default);
    Task<RapportResponse<PagedAlertesResponse>> GetAlertesAsync(string? periode, string? statut, string? severite, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RapportResponse<InventaireSyntheseDto>> GetInventaireAsync(string periode, string? codeTypeMatiere, CancellationToken cancellationToken = default);
    Task<RapportResponse<PagedRapportImportsResponse>> GetImportsAsync(DateOnly? dateDepuis, DateOnly? dateJusqua, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RapportResponse<AlerteNonReceptionResponse>> GetNonReceptionneesAsync(DateOnly date, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RapportExportResult> ExportCsvAsync(string code, string? periode, string? numeroOf, DateOnly? date, string? statut, string? severite, string? codeTypeMatiere, DateOnly? dateDepuis, DateOnly? dateJusqua, CancellationToken cancellationToken = default);
}

public sealed class RapportsService(
    IBilanFonderieService bilanFonderieService,
    IBilanUsinage2Service bilanUsinage2Service,
    IBilanUap2Service bilanUap2Service,
    IInventaireFonderieService inventaireService,
    IAlerteApplicationService alerteService,
    IAlerteNonReceptionService nonReceptionService,
    IOfContextResolver ofContextResolver,
    IRapportsReadRepository readRepository,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider) : IRapportsService
{
    private static readonly Regex PeriodeRegex = new("^(?<annee>\\d{4})-(?<mois>0[1-9]|1[0-2])$", RegexOptions.Compiled);
    private static readonly IReadOnlyDictionary<string, RapportCatalogueItemResponse> Catalogue =
        new RapportCatalogueItemResponse[]
        {
            new(CodesRapports.BilanFonderie, "Bilan Fonderie mensuel", "Projection du bilan Fonderie existant, sans recalcul de formule.", "Fonderie", "kg", ["periode"], ["CSV"], true, null),
            new(CodesRapports.BilanUsinage2, "Bilan Usinage 2 mensuel", "Projection du bilan Usinage 2 existant.", "Usinage 2", "pieces", ["periode"], ["CSV"], true, null),
            new(CodesRapports.BilanUap2, "Bilan UAP2 mensuel", "Projection du bilan UAP2 existant ; CTRL reste la quantite de reference.", "UAP2", "pieces", ["periode"], ["CSV"], true, null),
            new(CodesRapports.DetailOf, "Detail par OF", "Contexte, receptions, processus, mouvements, controles, references, bilans et alertes d'un OF.", "Transverse", null, ["numeroOF", "periode"], ["CSV"], true, null),
            new(CodesRapports.Alertes, "Alertes par periode", "Projection paginee des alertes metier persistantes.", "Alertes", null, ["periode", "statut", "severite"], ["CSV"], true, null),
            new(CodesRapports.InventaireFonderie, "Inventaire Fonderie mensuel", "Synthese reelle de l'inventaire Fonderie par type matiere.", "Inventaire", "kg", ["periode", "codeTypeMatiere"], ["CSV"], true, null),
            new(CodesRapports.HistoriqueImports, "Historique des imports", "Historique SQL des imports de references, rapports machine et rapprochements X3.", "Imports", null, ["dateDepuis", "dateJusqua"], ["CSV"], true, null),
            new(CodesRapports.ReceptionsNonReceptionnees, "Receptions non receptionnees", "Projection journaliere du service de non-reception existant.", "Usinage 2", "pieces", ["date"], ["CSV"], true, null)
        }.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RapportCatalogueItemResponse> GetCatalogue()
        => Catalogue.Values.Where(item => EstAutorise(item.Code)).OrderBy(item => item.Code).ToArray();

    public async Task<RapportResponse<BilanFonderieResponse>> GetBilanFonderieAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        GarantirAutorisation(CodesRapports.BilanFonderie);
        periode = ValiderPeriode(periode);
        var data = await bilanFonderieService.GetAsync(periode, cancellationToken);
        return new RapportResponse<BilanFonderieResponse>(
            Metadata(CodesRapports.BilanFonderie, periode, data.NombreLignesDonneesManquantes > 0,
                data.NombreLignesDonneesManquantes > 0 ? [$"{data.NombreLignesDonneesManquantes} ligne(s) avec donnees manquantes."] : []),
            data);
    }

    public async Task<RapportResponse<BilanUsinage2Response>> GetBilanUsinage2Async(
        string periode,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        GarantirAutorisation(CodesRapports.BilanUsinage2);
        periode = ValiderPeriode(periode);
        ValiderPagination(page, pageSize);
        var data = await bilanUsinage2Service.QueryAsync(
            new GetBilanUsinage2Query(periode, null, null, null, null, null, page, pageSize),
            cancellationToken);
        return new RapportResponse<BilanUsinage2Response>(
            Metadata(CodesRapports.BilanUsinage2, periode, data.NombreLignesDonneesManquantes > 0,
                data.NombreLignesDonneesManquantes > 0 ? [$"{data.NombreLignesDonneesManquantes} ligne(s) avec donnees manquantes."] : []),
            data);
    }

    public async Task<RapportResponse<BilanUap2Response>> GetBilanUap2Async(
        string periode,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        GarantirAutorisation(CodesRapports.BilanUap2);
        periode = ValiderPeriode(periode);
        ValiderPagination(page, pageSize);
        var fullData = await bilanUap2Service.GetAsync(periode, cancellationToken);
        var data = fullData with
        {
            Lignes = fullData.Lignes.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
            Total = fullData.NombreLignes,
            Page = page,
            PageSize = pageSize
        };
        return new RapportResponse<BilanUap2Response>(
            Metadata(CodesRapports.BilanUap2, periode, data.NombreLignesDonneesManquantes > 0,
                data.NombreLignesDonneesManquantes > 0 ? [$"{data.NombreLignesDonneesManquantes} ligne(s) avec donnees manquantes."] : []),
            data);
    }

    public async Task<RapportOfDetailResponse> GetOfAsync(
        string numeroOf,
        string periode,
        CancellationToken cancellationToken = default)
    {
        GarantirAutorisation(CodesRapports.DetailOf);
        numeroOf = ValiderNumeroOf(numeroOf);
        periode = ValiderPeriode(periode);

        var context = await ofContextResolver.ResolveAsync(numeroOf, null, cancellationToken);
        var readData = await readRepository.GetOfAsync(numeroOf, context.CodeArticle, cancellationToken);

        BilanUsinage2LigneResponse? bilanU2 = null;
        if (PeutLire(CodesRapports.BilanUsinage2))
        {
            var bilan = await bilanUsinage2Service.QueryAsync(
                new GetBilanUsinage2Query(periode, numeroOf, null, null, null, null, 1, 200),
                cancellationToken);
            bilanU2 = bilan.Lignes.FirstOrDefault(item => item.NumeroOF.Equals(numeroOf, StringComparison.OrdinalIgnoreCase));
        }

        BilanUap2LigneResponse? bilanUap2 = null;
        if (PeutLire(CodesRapports.BilanUap2))
        {
            var bilan = await bilanUap2Service.GetAsync(periode, cancellationToken);
            bilanUap2 = bilan.Lignes.FirstOrDefault(item => item.NumeroOF.Equals(numeroOf, StringComparison.OrdinalIgnoreCase));
        }

        var alerts = await alerteService.SearchAsync(
            new GetAlertesQuery(periode, null, null, null, numeroOf, null, null, 1, 200),
            cancellationToken);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(context.CodeArticle)) missing.Add("Article introuvable.");
        if (readData.Receptions.Count == 0) missing.Add("Aucune reception trouvee.");
        if (readData.Processus.Count == 0) missing.Add("Aucun rapport machine trouve.");
        if (readData.ControlesPoids.Count == 0) missing.Add("Aucun controle poids applicable trouve.");
        if (readData.References.Count == 0) missing.Add("Aucune reference de fabrication trouvee.");

        var metadata = Metadata(
            CodesRapports.DetailOf,
            periode,
            missing.Count > 0,
            missing,
            new Dictionary<string, string?> { ["periode"] = periode, ["numeroOF"] = numeroOf });
        return new RapportOfDetailResponse(
            metadata,
            new RapportOfContexteResponse(
                context.NumeroOF,
                context.CodeArticle,
                context.Designation,
                context.TypeMatiere,
                context.CodeAtelier,
                context.Groupe,
                context.EligibleCalculUap2,
                context.StatutRapprochement,
                context.Anomalies.ToArray()),
            readData.Receptions,
            readData.Processus,
            readData.Mouvements,
            readData.ControlesPoids,
            readData.References,
            bilanU2,
            bilanUap2,
            alerts.Items,
            missing);
    }

    public async Task<RapportResponse<PagedAlertesResponse>> GetAlertesAsync(
        string? periode,
        string? statut,
        string? severite,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        GarantirAutorisation(CodesRapports.Alertes);
        if (!string.IsNullOrWhiteSpace(periode)) periode = ValiderPeriode(periode);
        ValiderPagination(page, pageSize);
        var data = await alerteService.SearchAsync(
            new GetAlertesQuery(periode, statut, severite, null, null, null, null, page, pageSize),
            cancellationToken);
        return new RapportResponse<PagedAlertesResponse>(
            Metadata(
                CodesRapports.Alertes,
                periode,
                false,
                [],
                new Dictionary<string, string?>
                {
                    ["periode"] = periode,
                    ["statut"] = NormaliserOptionnel(statut),
                    ["severite"] = NormaliserOptionnel(severite),
                    ["page"] = page.ToString(CultureInfo.InvariantCulture),
                    ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
                }),
            data);
    }

    public async Task<RapportResponse<InventaireSyntheseDto>> GetInventaireAsync(
        string periode,
        string? codeTypeMatiere,
        CancellationToken cancellationToken = default)
    {
        GarantirAutorisation(CodesRapports.InventaireFonderie);
        var (annee, mois) = ParsePeriode(periode);
        var data = await inventaireService.GetSyntheseAsync(mois, annee, NormaliserOptionnel(codeTypeMatiere), cancellationToken);
        var warnings = data.Lignes.Count == 0 ? new[] { "Aucune ligne d'inventaire trouvee pour la periode." } : [];
        return new RapportResponse<InventaireSyntheseDto>(
            Metadata(
                CodesRapports.InventaireFonderie,
                periode,
                false,
                warnings,
                new Dictionary<string, string?>
                {
                    ["periode"] = periode,
                    ["codeTypeMatiere"] = NormaliserOptionnel(codeTypeMatiere)
                }),
            data);
    }

    public async Task<RapportResponse<PagedRapportImportsResponse>> GetImportsAsync(
        DateOnly? dateDepuis,
        DateOnly? dateJusqua,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        GarantirAutorisation(CodesRapports.HistoriqueImports);
        ValiderPagination(page, pageSize);
        if (dateDepuis is not null && dateJusqua is not null && dateDepuis > dateJusqua)
            throw Validation("dateJusqua", "La date de fin doit etre posterieure ou egale a la date de debut.");

        var fromUtc = dateDepuis?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = dateJusqua?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var result = await readRepository.SearchImportsAsync(fromUtc, toUtc, page, pageSize, cancellationToken);
        var data = new PagedRapportImportsResponse(result.Items, result.Total, page, pageSize);
        return new RapportResponse<PagedRapportImportsResponse>(
            Metadata(
                CodesRapports.HistoriqueImports,
                null,
                false,
                [],
                new Dictionary<string, string?>
                {
                    ["dateDepuis"] = dateDepuis?.ToString("yyyy-MM-dd"),
                    ["dateJusqua"] = dateJusqua?.ToString("yyyy-MM-dd"),
                    ["page"] = page.ToString(CultureInfo.InvariantCulture),
                    ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
                }),
            data);
    }

    public async Task<RapportResponse<AlerteNonReceptionResponse>> GetNonReceptionneesAsync(
        DateOnly date,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        GarantirAutorisation(CodesRapports.ReceptionsNonReceptionnees);
        ValiderPagination(page, pageSize);
        var data = await nonReceptionService.GetAsync(date, page, pageSize, cancellationToken);
        return new RapportResponse<AlerteNonReceptionResponse>(
            Metadata(
                CodesRapports.ReceptionsNonReceptionnees,
                date.ToString("yyyy-MM-dd"),
                data.NombreDonneesManquantes > 0,
                data.NombreDonneesManquantes > 0 ? [$"{data.NombreDonneesManquantes} ligne(s) avec donnees manquantes."] : [],
                new Dictionary<string, string?>
                {
                    ["date"] = date.ToString("yyyy-MM-dd"),
                    ["page"] = page.ToString(CultureInfo.InvariantCulture),
                    ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
                }),
            data);
    }

    public async Task<RapportExportResult> ExportCsvAsync(
        string code,
        string? periode,
        string? numeroOf,
        DateOnly? date,
        string? statut,
        string? severite,
        string? codeTypeMatiere,
        DateOnly? dateDepuis,
        DateOnly? dateJusqua,
        CancellationToken cancellationToken = default)
    {
        code = NormaliserCode(code);
        string csv;
        string suffix;

        switch (code)
        {
            case CodesRapports.BilanFonderie:
            {
                var report = await GetBilanFonderieAsync(periode ?? string.Empty, cancellationToken);
                csv = CsvFonderie(report.Donnees);
                suffix = report.Donnees.Periode;
                break;
            }
            case CodesRapports.BilanUsinage2:
            {
                var report = await GetBilanUsinage2Async(periode ?? string.Empty, 1, 200, cancellationToken);
                csv = CsvUsinage2(report.Donnees);
                suffix = report.Donnees.Periode;
                break;
            }
            case CodesRapports.BilanUap2:
            {
                var report = await GetBilanUap2Async(periode ?? string.Empty, 1, 200, cancellationToken);
                csv = CsvUap2(report.Donnees);
                suffix = report.Donnees.Periode;
                break;
            }
            case CodesRapports.DetailOf:
            {
                var report = await GetOfAsync(numeroOf ?? string.Empty, periode ?? string.Empty, cancellationToken);
                csv = CsvOf(report);
                suffix = report.Contexte.NumeroOF;
                break;
            }
            case CodesRapports.Alertes:
            {
                var report = await GetAlertesAsync(periode, statut, severite, 1, 200, cancellationToken);
                csv = CsvAlertes(report.Donnees);
                suffix = periode ?? "toutes-periodes";
                break;
            }
            case CodesRapports.InventaireFonderie:
            {
                var report = await GetInventaireAsync(periode ?? string.Empty, codeTypeMatiere, cancellationToken);
                csv = CsvInventaire(report.Donnees);
                suffix = periode ?? string.Empty;
                break;
            }
            case CodesRapports.HistoriqueImports:
            {
                var report = await GetImportsAsync(dateDepuis, dateJusqua, 1, 200, cancellationToken);
                csv = CsvImports(report.Donnees);
                suffix = dateTimeProvider.UtcNow.ToString("yyyy-MM-dd");
                break;
            }
            case CodesRapports.ReceptionsNonReceptionnees:
            {
                if (date is null) throw Validation("date", "La date est obligatoire.");
                var report = await GetNonReceptionneesAsync(date.Value, 1, 200, cancellationToken);
                csv = CsvNonReceptionnees(report.Donnees);
                suffix = date.Value.ToString("yyyy-MM-dd");
                break;
            }
            default:
                throw new NotFoundException("Rapport introuvable.");
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var payload = encoding.GetBytes(csv);
        var content = encoding.GetPreamble().Concat(payload).ToArray();
        return new RapportExportResult(content, "text/csv; charset=utf-8", $"{code.ToLowerInvariant()}-{SafeFilePart(suffix)}.csv");
    }

    private RapportMetadataResponse Metadata(
        string code,
        string? periode,
        bool partial,
        IReadOnlyList<string> warnings,
        IReadOnlyDictionary<string, string?>? filters = null)
    {
        var definition = Catalogue[code];
        var appliedFilters = filters ?? new Dictionary<string, string?> { ["periode"] = periode };
        return new RapportMetadataResponse(
            code,
            definition.Titre,
            dateTimeProvider.UtcNow,
            periode,
            definition.UnitePrincipale,
            partial,
            warnings,
            currentUser.UserName,
            Unites(code),
            Sources(code),
            appliedFilters);
    }

    private static IReadOnlyList<string> Unites(string code) => code switch
    {
        CodesRapports.BilanFonderie or CodesRapports.InventaireFonderie => ["kg"],
        CodesRapports.BilanUsinage2 or CodesRapports.ReceptionsNonReceptionnees => ["pieces", "kg", "kg/piece"],
        CodesRapports.BilanUap2 => ["pieces", "kg", "kg/piece"],
        CodesRapports.DetailOf or CodesRapports.Alertes => ["pieces", "kg", "kg/piece"],
        _ => []
    };

    private static IReadOnlyList<string> Sources(string code) => code switch
    {
        CodesRapports.BilanFonderie => ["Bilan Fonderie"],
        CodesRapports.BilanUsinage2 => ["Bilan Usinage 2"],
        CodesRapports.BilanUap2 => ["Bilan UAP2"],
        CodesRapports.DetailOf => ["Contexte OF", "Receptions U2", "Rapports machine", "Mouvements X3", "Controles poids", "References de fabrication", "Alertes"],
        CodesRapports.Alertes => ["AlertesMetier"],
        CodesRapports.InventaireFonderie => ["Inventaire Fonderie"],
        CodesRapports.HistoriqueImports => ["ImportBatches", "ImportX3Batches"],
        CodesRapports.ReceptionsNonReceptionnees => ["Rapports machine", "Receptions U2", "Entrees OF", "Sorties MP"],
        _ => []
    };

    private bool EstAutorise(string code) => PeutLire(code);

    private bool PeutLire(string code)
    {
        if (string.Equals(currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(currentUser.Role, "ResponsableFonderie", StringComparison.OrdinalIgnoreCase))
            return code is CodesRapports.BilanFonderie or CodesRapports.DetailOf or CodesRapports.Alertes or CodesRapports.InventaireFonderie or CodesRapports.HistoriqueImports;
        if (string.Equals(currentUser.Role, "ResponsableUsinage2", StringComparison.OrdinalIgnoreCase))
            return code is CodesRapports.BilanUsinage2 or CodesRapports.BilanUap2 or CodesRapports.DetailOf or CodesRapports.Alertes or CodesRapports.HistoriqueImports or CodesRapports.ReceptionsNonReceptionnees;
        return false;
    }

    private void GarantirAutorisation(string code)
    {
        if (!PeutLire(code)) throw new ForbiddenException("Ce rapport ne fait pas partie de votre perimetre.");
    }

    private static string ValiderNumeroOf(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized)) throw Validation("numeroOF", "Le numero d'OF est obligatoire.");
        if (normalized.Length > 50) throw Validation("numeroOF", "Le numero d'OF ne peut pas depasser 50 caracteres.");
        return normalized;
    }

    private static string ValiderPeriode(string value)
    {
        var normalized = value?.Trim();
        if (normalized is null || !PeriodeRegex.IsMatch(normalized))
            throw Validation("periode", "La periode doit respecter le format YYYY-MM.");
        return normalized;
    }

    private static (int Annee, int Mois) ParsePeriode(string value)
    {
        var normalized = ValiderPeriode(value);
        return (int.Parse(normalized[..4], CultureInfo.InvariantCulture), int.Parse(normalized[5..], CultureInfo.InvariantCulture));
    }

    private static void ValiderPagination(int page, int pageSize)
    {
        if (page < 1) throw Validation("page", "La page doit etre superieure ou egale a 1.");
        if (pageSize is < 1 or > 200) throw Validation("pageSize", "La taille de page doit etre comprise entre 1 et 200.");
    }

    private static string? NormaliserOptionnel(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormaliserCode(string value)
    {
        var code = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code)) throw Validation("code", "Le code du rapport est obligatoire.");
        return code;
    }

    private static ValidationException Validation(string property, string message)
        => new([new ValidationFailure(property, message)]);

    private static string CsvFonderie(BilanFonderieResponse data)
    {
        var rows = new List<string[]> { new[] { "Periode", "Type matiere", "Stock debut (kg)", "Sorties magasin X3 (kg)", "Input (kg)", "Stock fin (kg)", "Dechets Fonderie (kg)", "Receptions U2 (kg)", "Output (kg)", "Ecart (kg)", "Taux ecart (%)", "Statut", "Anomalies" } };
        rows.AddRange(data.Lignes.Select(item => new[] { item.Periode, item.CodeTypeMatiere, N(item.StockDebutKg), N(item.SortiesMagasinX3Kg), N(item.InputKg), N(item.StockFinKg), N(item.DechetsFonderieKg), N(item.ReceptionsUsinage2Kg), N(item.OutputKg), N(item.EcartKg), N(item.TauxEcartPourcent), item.Statut, string.Join(" | ", item.Anomalies) }));
        return Csv(rows);
    }

    private static string CsvUsinage2(BilanUsinage2Response data)
    {
        var rows = new List<string[]> { new[] { "Periode", "Numero OF", "Article", "Designation", "Atelier", "Type matiere", "CTRL EMR (pieces)", "Rebut EMR (pieces)", "Nombre de pieces", "Receptionnees (pieces)", "Non receptionnees (pieces)", "Rebut U2 (pieces)", "Entree OF (pieces)", "Bilan U2 (pieces)", "Statut", "Anomalies" } };
        rows.AddRange(data.Lignes.Select(item => new[] { item.Periode, item.NumeroOF, item.CodeArticle, item.DesignationArticle ?? string.Empty, item.AtelierOrigine ?? string.Empty, item.TypeMatiere ?? string.Empty, N(item.QuantiteCtrlEmrPieces), N(item.RebutEmrPieces), N(item.NombreDePieces), N(item.QuantiteReceptionneePieces), N(item.QuantiteNonReceptionneePieces), N(item.RebutUsinage2Pieces), N(item.EntreeOfPieces), N(item.BilanUsinage2Pieces), item.Statut, string.Join(" | ", item.Anomalies) }));
        return Csv(rows);
    }

    private static string CsvUap2(BilanUap2Response data)
    {
        var rows = new List<string[]> { new[] { "Periode", "Numero OF", "Article", "Type matiere", "CTRL reference (pieces)", "Rebut SCI (pieces)", "Rebut EMR (pieces)", "Rebut TRN (pieces)", "Sortie MP (pieces)", "Poids physique (kg/piece)", "Sortie MP (kg)", "Entree OF (pieces)", "Rebut U2 (pieces)", "Ecart UAP2 (pieces)", "Taux ecart (%)", "Statut", "Anomalies" } };
        rows.AddRange(data.Lignes.Select(item => new[] { item.Periode, item.NumeroOF, item.CodeArticle, item.CodeTypeMatiere ?? string.Empty, N(item.QuantiteCtrlPieces), N(item.RebutSciPieces), N(item.RebutEmrPieces), N(item.RebutTrnPieces), N(item.QuantiteSortieMpPieces), N(item.PoidsPhysiqueUnitaireKg), N(item.SortieMpKg), N(item.EntreeOfPieces), N(item.RebutUsinage2Pieces), N(item.EcartUap2Pieces), N(item.TauxEcartPourcent), item.Statut, string.Join(" | ", item.Anomalies) }));
        return Csv(rows);
    }

    private static string CsvOf(RapportOfDetailResponse data)
    {
        var rows = new List<string[]> { new[] { "Section", "Date", "Code", "Libelle", "Valeur 1", "Unite 1", "Valeur 2", "Unite 2", "Statut" } };
        rows.AddRange(data.Receptions.Select(item => new[] { "Reception", item.DateReception.ToString("yyyy-MM-dd"), item.CodeArticle ?? string.Empty, item.Id.ToString(), N(item.NombrePiecesReceptionnees), "pieces", N(item.PoidsTotalReceptionneKg), "kg", item.Statut }));
        rows.AddRange(data.Processus.Select(item => new[] { "Processus", item.DateProduction.ToString("yyyy-MM-dd"), item.CodePoste, item.Processus, N(item.QuantiteCtrlPieces), "pieces", N(item.QuantiteRebutPieces), "pieces", string.Empty }));
        rows.AddRange(data.Mouvements.Select(item => new[] { "Mouvement", item.DateImputation.ToString("yyyy-MM-dd"), item.CodeArticle ?? string.Empty, item.Transaction ?? string.Empty, N(item.QuantiteSignee), item.Unite ?? string.Empty, string.Empty, string.Empty, item.StatutRapprochement }));
        rows.AddRange(data.ControlesPoids.Select(item => new[] { "Controle poids", item.DateControle.ToString("yyyy-MM-dd"), item.CodeArticle, item.TypeMatiere, N(item.PoidsPhysiqueUnitaireKg), "kg/piece", string.Empty, string.Empty, item.Statut }));
        rows.AddRange(data.References.Select(item => new[] { "Reference", string.Empty, item.CodeArticle, item.TypeReference, N(item.QuantiteLien), item.UniteQuantite ?? string.Empty, item.NumeroOperation ?? string.Empty, item.CodePoste ?? string.Empty, string.Empty }));
        rows.AddRange(data.Alertes.Select(item => new[] { "Alerte", item.DetecteeLeUtc.ToString("O"), item.Code, item.Titre, string.Empty, string.Empty, string.Empty, string.Empty, item.Statut }));
        return Csv(rows);
    }

    private static string CsvAlertes(PagedAlertesResponse data)
    {
        var rows = new List<string[]> { new[] { "Code", "Titre", "Description", "Source", "Periode", "Numero OF", "Article", "Type matiere", "Severite", "Statut", "Premiere detection UTC", "Derniere detection UTC", "Acquittee UTC", "Acquittee par", "Resolue UTC", "Resolue par", "Ignoree UTC", "Ignoree par", "Commentaire traitement" } };
        rows.AddRange(data.Items.Select(item => new[] { item.Code, item.Titre, item.Description ?? string.Empty, item.Source, item.Periode ?? string.Empty, item.NumeroOF ?? string.Empty, item.CodeArticle ?? string.Empty, item.TypeMatiere ?? string.Empty, item.Severite, item.Statut, item.DetecteeLeUtc.ToString("O"), item.DerniereDetectionLeUtc.ToString("O"), item.AcquitteeLeUtc?.ToString("O") ?? string.Empty, item.AcquitteePar ?? string.Empty, item.ResolueLeUtc?.ToString("O") ?? string.Empty, item.ResoluePar ?? string.Empty, item.IgnoreeLeUtc?.ToString("O") ?? string.Empty, item.IgnoreePar ?? string.Empty, item.CommentaireTraitement ?? string.Empty }));
        return Csv(rows);
    }

    private static string CsvInventaire(InventaireSyntheseDto data)
    {
        var rows = new List<string[]> { new[] { "Type matiere", "Conteneurs (kg)", "Categories (kg)", "Fours hors bilan (kg)", "Total rapport (kg)", "Total bilan matiere (kg)", "Statut" } };
        rows.AddRange(data.Lignes.Select(item => new[] { item.CodeTypeMatiere, N(item.Partie1Kg), N(item.Partie2Kg), N(item.Partie3FourKg), N(item.TotalRapportInventaireKg), N(item.TotalBilanMatiereKg), item.Statut }));
        return Csv(rows);
    }

    private static string CsvImports(PagedRapportImportsResponse data)
    {
        var rows = new List<string[]> { new[] { "Id", "Type import", "Profil", "Statut", "Fichier", "Debut UTC", "Fin UTC", "Lignes lues", "Lignes valides", "Lignes inserees", "Lignes mises a jour", "Lignes importees", "Lignes rejetees", "Anomalies", "Utilisateur", "Erreur archivage" } };
        rows.AddRange(data.Items.Select(item => new[] { item.Id.ToString(), item.TypeImport, item.ProfilImport ?? string.Empty, item.Statut, item.NomFichier ?? string.Empty, item.DateCreationUtc.ToString("O"), item.DateFinUtc?.ToString("O") ?? string.Empty, item.LignesLues.ToString(CultureInfo.InvariantCulture), item.LignesValides.ToString(CultureInfo.InvariantCulture), item.LignesInserees.ToString(CultureInfo.InvariantCulture), item.LignesMisesAJour.ToString(CultureInfo.InvariantCulture), item.LignesImportees.ToString(CultureInfo.InvariantCulture), item.LignesRejetees.ToString(CultureInfo.InvariantCulture), item.NombreAnomalies.ToString(CultureInfo.InvariantCulture), item.CreePar ?? string.Empty, item.ArchivageEnErreur ? "Oui" : "Non" }));
        return Csv(rows);
    }

    private static string CsvNonReceptionnees(AlerteNonReceptionResponse data)
    {
        var rows = new List<string[]> { new[] { "Date", "Numero OF", "Article", "Designation", "Type matiere", "Total EMR (pieces)", "Rebut EMR (pieces)", "Net EMR (pieces)", "Receptionnees U2 (pieces)", "Non receptionnees (pieces)", "Statut", "Anomalies" } };
        rows.AddRange(data.Lignes.Select(item => new[] { item.Date.ToString("yyyy-MM-dd"), item.NumeroOF, item.CodeArticle ?? string.Empty, item.DesignationArticle ?? string.Empty, item.CodeTypeMatiere ?? string.Empty, N(item.TotalEmrPieces), N(item.RebutEmrPieces), N(item.NetEmrPieces), N(item.QuantiteReceptionneeU2Pieces), N(item.PiecesNonReceptionnees), item.Statut, string.Join(" | ", item.Anomalies) }));
        return Csv(rows);
    }

    private static string Csv(IEnumerable<string[]> rows)
        => string.Join("\r\n", rows.Select(row => string.Join(';', row.Select(CsvCell))));

    private static string CsvCell(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@') safe = "'" + safe;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private static string N(decimal? value)
        => value?.ToString("0.############################", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string SafeFilePart(string value)
    {
        var safe = new string(value.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "export" : safe;
    }
}
