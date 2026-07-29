using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Imports;
using BilanMatiereUAP2.Contracts.ImportsX3;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;

namespace BilanMatiereUAP2.Application.Features.ImportsX3;

public sealed record ImportSortiesMatiereX3Command(
    X3ImportFileContent? EncoursProductFile,
    X3ImportFileContent? MouvementsStockFile);

public sealed record GetImportX3ByIdQuery(Guid Id);
public sealed record GetImportX3AnomaliesQuery(Guid Id);
public sealed record GetOrdreFabricationContexteQuery(string NumeroOF);
public sealed record GetSortiesMatiereQuery(
    string? Periode,
    DateOnly? DateDebut,
    DateOnly? DateFin,
    string? NumeroOF,
    string? Article,
    string? Matiere,
    string? Statut,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null);

public interface IX3ImportService
{
    Task<ImportX3Response> ImportAsync(
        ImportSortiesMatiereX3Command command,
        CancellationToken cancellationToken);
    Task<ImportX3Response> GetByIdAsync(GetImportX3ByIdQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<ImportX3AnomalyResponse>> GetAnomaliesAsync(
        GetImportX3AnomaliesQuery query,
        CancellationToken cancellationToken);
    Task<PagedConsommationsComposantsResponse> GetSortiesAsync(
        GetSortiesMatiereQuery query,
        CancellationToken cancellationToken);
    Task<OrdreFabricationContexteResponse> GetOrdreContexteAsync(
        GetOrdreFabricationContexteQuery query,
        CancellationToken cancellationToken);
}

public sealed class X3ImportService(
    IEncoursProductReader encoursReader,
    IMouvementStockX3Reader mouvementReader,
    IX3ImportRepository repository,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider,
    IValidator<ImportSortiesMatiereX3Command> importValidator,
    IOptions<ImportX3Options> importOptions,
    IImportArchiveService archiveService) : IX3ImportService
{
    // Regles de rapprochement externalisees en configuration (section "ImportX3").
    private readonly HashSet<string> _typesMatiereAutorises =
        new(importOptions.Value.TypesMatiereAutorises, StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unitesConservees =
        new(importOptions.Value.UnitesConservees, StringComparer.OrdinalIgnoreCase);

    public async Task<ImportX3Response> ImportAsync(
        ImportSortiesMatiereX3Command command,
        CancellationToken cancellationToken)
    {
        await importValidator.ValidateAndThrowAsync(command, cancellationToken);
        var encoursFile = command.EncoursProductFile!;
        var mouvementsFile = command.MouvementsStockFile!;
        var encoursHash = X3ImportParsing.CalculerSha256(encoursFile.Content);
        var mouvementsHash = X3ImportParsing.CalculerSha256(mouvementsFile.Content);
        var pairHash = X3ImportParsing.CalculerHashCouple(encoursHash, mouvementsHash);

        // Check for existing successful batch with the same pair hash (idempotency)
        var existingBatch = await repository.GetCompletedBatchByPairHashAsync(pairHash, cancellationToken);
        if (existingBatch is not null)
        {
            return ToImportResponse(existingBatch, true);
        }

        var encoursRead = await encoursReader.ReadAsync(encoursFile, cancellationToken);
        var mouvementsRead = await mouvementReader.ReadAsync(mouvementsFile, cancellationToken);
        var now = dateTimeProvider.UtcNow;
        var batch = ImportX3Batch.Demarrer(pairHash, currentUser.UserId, currentUser.UserName, now);
        batch.AjouterFichier(ImportX3File.Creer(
            batch.Id,
            TypeFichierImportX3.EncoursProduct,
            encoursFile.FileName,
            Path.GetExtension(encoursFile.FileName),
            encoursFile.Length,
            encoursHash,
            now));
        batch.AjouterFichier(ImportX3File.Creer(
            batch.Id,
            TypeFichierImportX3.MouvementsStock,
            mouvementsFile.FileName,
            Path.GetExtension(mouvementsFile.FileName),
            mouvementsFile.Length,
            mouvementsHash,
            now));
        AjouterErreursLecture(batch, TypeFichierImportX3.EncoursProduct, encoursRead.Errors, now);
        AjouterErreursLecture(batch, TypeFichierImportX3.MouvementsStock, mouvementsRead.Errors, now);

        var encoursGroups = encoursRead.Rows
            .GroupBy(row => row.NumeroOF, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var existingOrdres = await repository.GetOrdresByNumeroAsync(
            encoursGroups.Select(g => g.Key).ToArray(),
            cancellationToken);

        var ordersByNumber = new Dictionary<string, X3OrdreFabrication>(StringComparer.OrdinalIgnoreCase);
        var newOrdres = new List<X3OrdreFabrication>();
        var articleStatuses = new Dictionary<string, StatutRapprochementX3>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in encoursGroups)
        {
            var rows = group.OrderBy(row => row.NumeroLigneSource).ToArray();
            var articles = rows
                .Select(row => row.Article?.Trim())
                .Where(article => !string.IsNullOrWhiteSpace(article))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToArray();
            var articleStatus = articles.Length switch
            {
                0 => StatutRapprochementX3.ArticlePrincipalIntrouvable,
                > 1 => StatutRapprochementX3.PlusieursArticlesPrincipaux,
                _ => StatutRapprochementX3.Rapproche
            };
            articleStatuses[group.Key] = articleStatus;

            if (!existingOrdres.TryGetValue(group.Key, out var order))
            {
                order = X3OrdreFabrication.Creer(group.Key, batch.Id, now);
                newOrdres.Add(order);
            }
            ordersByNumber.Add(group.Key, order);

            var contextRow = rows[0];
            var mainArticle = articles.SingleOrDefault();
            var mainDesignation = mainArticle is null
                ? null
                : rows
                    .Where(row => string.Equals(row.Article?.Trim(), mainArticle, StringComparison.OrdinalIgnoreCase))
                    .Select(row => row.DesignationArticle?.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
            order.MettreAJourContexte(
                mainArticle,
                mainDesignation,
                contextRow.SituationOF,
                contextRow.Atelier,
                contextRow.DateObjectifOF,
                contextRow.Statut,
                batch.Id,
                now,
                articleStatus.ToString());

            foreach (var row in rows)
            {
                order.AjouterOuMettreAJourOperation(
                    row.NumeroLigneSource,
                    row.NumeroOperation,
                    row.CodePoste,
                    row.TypePoste,
                    row.NumeroPoste,
                    row.EffectifPoste,
                    row.Cad,
                    row.EffectifOperation,
                    row.QuantitePrevue,
                    row.QuantiteRestante,
                    row.DateOperation,
                    batch.Id,
                    now);
            }

            if (articleStatus != StatutRapprochementX3.Rapproche)
            {
                var message = articleStatus == StatutRapprochementX3.ArticlePrincipalIntrouvable
                    ? $"Aucun article principal n'est defini pour l'OF {group.Key}."
                    : $"Plusieurs articles principaux sont presents pour l'OF {group.Key}: {string.Join(", ", articles)}.";
                batch.AjouterAnomalie(ImportX3Anomaly.Creer(
                    batch.Id,
                    TypeFichierImportX3.EncoursProduct,
                    rows[0].NumeroLigneSource,
                    articleStatus,
                    message,
                    group.Key,
                    null,
                    now));
            }
        }

        var matchedMovements = new List<(MouvementStockX3Row Row, X3OrdreFabrication Order, string CodeTypeMatiere)>();

        foreach (var row in mouvementsRead.Rows)
        {
            if (!EstSortieMatiere(row) || !EstUniteConservee(row.Unite))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.NumeroOFNormalise) ||
                !ordersByNumber.TryGetValue(row.NumeroOFNormalise, out var order))
            {
                continue;
            }

            var normalizedCode = X3ImportParsing.NormaliserCode(row.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode) || !EstTypeMatiereAutorise(normalizedCode))
            {
                continue;
            }

            var articleStatus = articleStatuses.GetValueOrDefault(
                row.NumeroOFNormalise,
                StatutRapprochementX3.ArticlePrincipalIntrouvable);
            if (articleStatus != StatutRapprochementX3.Rapproche)
            {
                continue;
            }

            matchedMovements.Add((row, order, normalizedCode));
        }

        // Group matched movements by business key: (NumeroOF, CodeTypeMatiere, Unite, DateImputation)
        var groupedConsommations = matchedMovements
            .GroupBy(m => new
            {
                m.Order.NumeroOF,
                m.CodeTypeMatiere,
                Unite = m.Row.Unite!,
                DateImputation = m.Row.DateImputation
            })
            .Select(g => new
            {
                CleMetier = ConsommationComposant.GenererCleMetier(g.Key.NumeroOF, g.Key.CodeTypeMatiere, g.Key.Unite, g.Key.DateImputation),
                g.Key.DateImputation,
                Order = g.First().Order,
                g.Key.CodeTypeMatiere,
                DesignationComposant = g.First().Row.Designation,
                g.Key.Unite,
                SommeQuantite = g.Sum(x => x.Row.QuantiteX3Signee)
            })
            .ToArray();

        var existingConsommations = await repository.GetConsommationsByCleMetierAsync(
            groupedConsommations.Select(c => c.CleMetier).ToArray(),
            cancellationToken);

        var newConsommations = new List<ConsommationComposant>();
        var allConsommationsForBatch = new List<ConsommationComposant>();

        foreach (var c in groupedConsommations)
        {
            if (existingConsommations.TryGetValue(c.CleMetier, out var existingConsom))
            {
                existingConsom.MettreAJour(
                    c.Order,
                    c.DesignationComposant,
                    c.SommeQuantite,
                    batch.Id,
                    now);
                allConsommationsForBatch.Add(existingConsom);
            }
            else
            {
                var newConsom = ConsommationComposant.Creer(
                    batch.Id,
                    c.DateImputation,
                    c.Order,
                    c.CodeTypeMatiere,
                    c.DesignationComposant,
                    c.Unite,
                    c.SommeQuantite,
                    now);
                newConsommations.Add(newConsom);
                allConsommationsForBatch.Add(newConsom);
            }
        }

        batch.Terminer(
            encoursRead.TotalRows,
            encoursRead.Rows.Count,
            mouvementsRead.TotalRows,
            mouvementsRead.Rows.Count,
            dateTimeProvider.UtcNow,
            allConsommationsForBatch.Count,
            batch.Anomalies.Count);

        // Save to SQL Database
        await repository.AddBatchAsync(batch, cancellationToken);
        if (newOrdres.Count > 0)
        {
            await repository.AddOrdresAsync(newOrdres, cancellationToken);
        }
        if (newConsommations.Count > 0)
        {
            await repository.AddConsommationsAsync(newConsommations, cancellationToken);
        }
        await repository.SaveChangesAsync(cancellationToken);

        // Best-effort : l'archivage des deux fichiers sources ne doit jamais faire echouer un
        // import metier deja reussi (donnees deja persistees).
        try
        {
            await archiveService.ArchiveAsync(batch.Id, TypeImport.SortiesMatiereX3, "ENCOURSPRODUCT_V1", encoursFile.FileName, encoursFile.Content, currentUser.UserId, currentUser.UserName, cancellationToken);
            await archiveService.ArchiveAsync(batch.Id, TypeImport.SortiesMatiereX3, "MOUVEMENTSSTOCK_V1", mouvementsFile.FileName, mouvementsFile.Content, currentUser.UserId, currentUser.UserName, cancellationToken);
        }
        catch
        {
            // best-effort : ne pas masquer le succes de l'import metier
        }

        return ToImportResponse(batch, false, allConsommationsForBatch);
    }

    public async Task<ImportX3Response> GetByIdAsync(
        GetImportX3ByIdQuery query,
        CancellationToken cancellationToken)
    {
        var batch = await repository.GetBatchByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException("Import X3 introuvable.");
        return ToImportResponse(batch, false);
    }

    public async Task<IReadOnlyList<ImportX3AnomalyResponse>> GetAnomaliesAsync(
        GetImportX3AnomaliesQuery query,
        CancellationToken cancellationToken)
    {
        if (await repository.GetBatchByIdAsync(query.Id, cancellationToken) is null)
        {
            throw new NotFoundException("Import X3 introuvable.");
        }

        var anomalies = await repository.GetAnomaliesAsync(query.Id, cancellationToken);
        return anomalies.Select(ToAnomalyResponse).ToArray();
    }

    public async Task<PagedConsommationsComposantsResponse> GetSortiesAsync(
        GetSortiesMatiereQuery query,
        CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var (start, end) = ParseDateRange(query.Periode, query.DateDebut, query.DateFin);
        var status = ParseStatus(query.Statut);
        var sortBy = ParseSortBy(query.SortBy);
        var descending = !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        var result = await repository.GetConsommationsAsync(
            new ConsommationsComposantsFilter(
                start,
                end,
                NormalizeOptional(query.NumeroOF, X3ImportParsing.NormaliserNumeroOF),
                NormalizeOptional(query.Article, value => value.Trim()),
                NormalizeOptional(query.Matiere, X3ImportParsing.NormaliserCode),
                status,
                query.Page,
                query.PageSize,
                sortBy,
                descending),
            cancellationToken);

        return new PagedConsommationsComposantsResponse(
            result.Items.Select(ToConsommationResponse).ToArray(),
            result.Total,
            query.Page,
            query.PageSize);
    }

    public async Task<OrdreFabricationContexteResponse> GetOrdreContexteAsync(
        GetOrdreFabricationContexteQuery query,
        CancellationToken cancellationToken)
    {
        var numeroOF = X3ImportParsing.NormaliserNumeroOF(query.NumeroOF);
        if (string.IsNullOrWhiteSpace(numeroOF))
        {
            throw Validation("numeroOF", "Le numero d'OF est obligatoire.");
        }

        var order = await repository.GetOrdreContexteAsync(numeroOF, cancellationToken)
            ?? throw new NotFoundException("Ordre de fabrication introuvable.");
        return new OrdreFabricationContexteResponse(
            order.Id,
            order.NumeroOF,
            order.ArticlePrincipal,
            order.DesignationPrincipale,
            order.SituationOF,
            order.Atelier,
            order.DateObjectifOF,
            order.StatutX3,
            order.Operations
                .OrderBy(item => item.NumeroOperation)
                .ThenBy(item => item.CodePoste)
                .Select(item => new X3OperationOFResponse(
                    item.Id,
                    item.NumeroLigneSource,
                    item.NumeroOperation,
                    item.CodePoste,
                    item.TypePoste,
                    item.NumeroPoste,
                    item.QuantitePrevue,
                    item.QuantiteRestante,
                    item.DateOperation))
                .ToArray(),
            Array.Empty<X3MouvementStockResponse>(),
            order.Consommations.Select(ToConsommationResponse).ToArray());
    }

    private static void AjouterErreursLecture(
        ImportX3Batch batch,
        TypeFichierImportX3 fileType,
        IEnumerable<X3ReadError> errors,
        DateTime now)
    {
        foreach (var error in errors)
        {
            batch.AjouterAnomalie(ImportX3Anomaly.Creer(
                batch.Id,
                fileType,
                error.NumeroLigneSource,
                error.Statut,
                error.Message,
                error.NumeroOF,
                error.CodeTypeMatiere,
                now,
                error.NomFeuilleSource));
        }
    }

    private static bool EstSortieMatiere(MouvementStockX3Row row)
    {
        return X3ImportParsing.NormaliserEntete(row.Transaction) == "sortieof" &&
               X3ImportParsing.NormaliserEntete(row.TypePiece) == "suivimatiere" &&
               X3ImportParsing.EstNumeroOF(row.PieceOrigine);
    }

    private bool EstUniteConservee(string? unite)
    {
        var normalized = X3ImportParsing.NormaliserCode(unite);
        return !string.IsNullOrEmpty(normalized) && _unitesConservees.Contains(normalized);
    }

    private bool EstTypeMatiereAutorise(string? code)
    {
        var normalized = X3ImportParsing.NormaliserCode(code);
        return !string.IsNullOrEmpty(normalized) && _typesMatiereAutorises.Contains(normalized);
    }

    private static ImportX3Response ToImportResponse(
        ImportX3Batch batch,
        bool duplicate,
        IReadOnlyCollection<ConsommationComposant>? consommationsOverride = null)
    {
        var consommationsList = consommationsOverride ?? (IReadOnlyCollection<ConsommationComposant>)batch.Consommations;
        return new ImportX3Response(
            batch.Id,
            batch.Statut.ToString(),
            duplicate,
            new ImportX3StatisticsResponse(
                batch.LignesEncoursLues,
                batch.LignesEncoursValides,
                batch.LignesEncoursLues - batch.LignesEncoursValides),
            new ImportX3StatisticsResponse(
                batch.LignesMouvementsLues,
                batch.LignesMouvementsValides,
                batch.LignesMouvementsLues - batch.LignesMouvementsValides),
            batch.NombreSortiesGenerees,
            batch.NombreAnomalies,
            batch.Fichiers
                .OrderBy(file => file.TypeFichier)
                .Select(file => new ImportX3FileResponse(
                    file.TypeFichier.ToString(),
                    file.NomFichier,
                    file.Extension,
                    file.TailleOctets,
                    file.HashSha256))
                .ToArray(),
            consommationsList.OrderBy(item => item.DateImputation).ThenBy(item => item.NumeroOF)
                .Select(ToConsommationResponse)
                .ToArray(),
            batch.Anomalies.OrderBy(item => item.NumeroLigneSource).Select(ToAnomalyResponse).ToArray(),
            batch.DateCreationUtc,
            batch.DateFinUtc);
    }

    private static ConsommationComposantResponse ToConsommationResponse(ConsommationComposant entity)
    {
        return new ConsommationComposantResponse(
            entity.Id,
            entity.DateImputation,
            entity.NumeroOF,
            entity.ArticlePrincipal,
            entity.DesignationPrincipale,
            entity.CodeTypeMatiere,
            entity.DesignationComposant,
            entity.Unite,
            entity.SommeQuantiteX3Signee,
            entity.Consommation,
            entity.Statut.ToString(),
            entity.ImportBatchId);
    }

    private static ImportX3AnomalyResponse ToAnomalyResponse(ImportX3Anomaly entity)
    {
        return new ImportX3AnomalyResponse(
            entity.Id,
            entity.TypeFichier.ToString(),
            entity.NumeroLigneSource,
            entity.Statut.ToString(),
            entity.Message,
            entity.NumeroOF,
            entity.CodeTypeMatiere);
    }

    private static (DateOnly? Start, DateOnly? End) ParseDateRange(
        string? period,
        DateOnly? start,
        DateOnly? endInclusive)
    {
        if (start is not null || endInclusive is not null)
        {
            if (start is not null && endInclusive is not null && start > endInclusive)
            {
                throw Validation("dateFin", "La date de fin doit etre posterieure a la date de debut.");
            }

            return (start, endInclusive?.AddDays(1));
        }

        if (string.IsNullOrWhiteSpace(period))
        {
            return (null, null);
        }

        if (!DateTime.TryParseExact(period, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw Validation("periode", "La periode doit respecter le format YYYY-MM.");
        }

        var periodStart = new DateOnly(parsed.Year, parsed.Month, 1);
        return (periodStart, periodStart.AddMonths(1));
    }

    private static StatutRapprochementX3? ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<StatutRapprochementX3>(value, true, out var status) || !Enum.IsDefined(status))
        {
            throw Validation("statut", "Le statut de rapprochement est invalide.");
        }

        return status;
    }

    private static string ParseSortBy(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? "date";
        return normalized is "date" or "numeroof" or "article" or "matiere" or "sortiemp" or "statut"
            ? normalized
            : throw Validation("sortBy", "Le champ de tri est invalide.");
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw Validation("page", "La page doit etre superieure ou egale a 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw Validation("pageSize", "La taille de page doit etre comprise entre 1 et 100.");
        }
    }

    private static string? NormalizeOptional(string? value, Func<string, string> normalize)
    {
        return string.IsNullOrWhiteSpace(value) ? null : normalize(value);
    }

    private static ValidationException Validation(string propertyName, string message)
    {
        return new ValidationException([new ValidationFailure(propertyName, message)]);
    }
}
