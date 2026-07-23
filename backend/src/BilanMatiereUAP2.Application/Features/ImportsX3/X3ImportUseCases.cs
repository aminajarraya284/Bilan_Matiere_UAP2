using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.ImportsX3;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

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
    IValidator<ImportSortiesMatiereX3Command> importValidator) : IX3ImportService
{
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
        // First X3 import step: read, reconcile and return JSON without persistence.
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
        var ordersByNumber = new Dictionary<string, X3OrdreFabrication>(StringComparer.OrdinalIgnoreCase);
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

            var order = X3OrdreFabrication.Creer(group.Key, batch.Id, now);
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
                now);

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

        var componentsByCode = new Dictionary<string, Composant>(StringComparer.OrdinalIgnoreCase);
        var matchedMovements = new List<(X3MouvementStock Movement, X3OrdreFabrication Order, Composant Component)>();

        foreach (var row in mouvementsRead.Rows)
        {
            if (!EstSortieMatiere(row) || !EstUniteConservee(row.Unite))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.NumeroOFNormalise) ||
                !ordersByNumber.ContainsKey(row.NumeroOFNormalise))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(X3ImportParsing.NormaliserCode(row.Code)))
            {
                continue;
            }

            var movement = X3MouvementStock.Creer(
                batch.Id,
                row.NumeroLigneSource,
                row.NomFeuilleSource,
                row.DateImputation,
                row.Code,
                row.Designation,
                row.DateCreation,
                row.HeureCreation,
                row.Utilisateur,
                row.QuantiteX3Signee,
                row.Unite,
                row.Transaction,
                row.TypePiece,
                row.Lot,
                row.PieceOrigine,
                row.NumeroOFNormalise,
                row.DesignationMouvement,
                row.Emplacement,
                row.Statut,
                now);
            if (!EstSortieMatiere(row))
            {
                movement.DefinirRapprochement(null, null, StatutRapprochementX3.MouvementInvalide);
                batch.AjouterAnomalie(ImportX3Anomaly.Creer(
                    batch.Id,
                    TypeFichierImportX3.MouvementsStock,
                    movement.NumeroLigneSource,
                    StatutRapprochementX3.MouvementInvalide,
                    "Le mouvement ne respecte pas le filtre Transaction = Sortie OF et Type Pce = Suivi matière.",
                    movement.NumeroOFNormalise,
                    movement.Code,
                    now,
                    movement.NomFeuilleSource));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.NumeroOFNormalise) ||
                !ordersByNumber.TryGetValue(row.NumeroOFNormalise, out var order))
            {
                AjouterAnomalieMouvement(
                    batch,
                    movement,
                    StatutRapprochementX3.OFIntrouvable,
                    string.IsNullOrWhiteSpace(row.NumeroOFNormalise)
                        ? "La piece origine du mouvement est vide."
                        : $"L'OF {row.NumeroOFNormalise} est introuvable dans EncoursProduct.",
                    now);
                continue;
            }

            var normalizedCode = X3ImportParsing.NormaliserCode(row.Code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                AjouterAnomalieMouvement(
                    batch,
                    movement,
                    StatutRapprochementX3.ComposantIntrouvable,
                    "Le code composant du mouvement est obligatoire.",
                    now,
                    order.Id);
                continue;
            }

            if (!componentsByCode.TryGetValue(normalizedCode, out var component))
            {
                component = Composant.Creer(normalizedCode, row.Designation, now);
                componentsByCode.Add(component.Code, component);
            }
            else
            {
                component.MettreAJourDesignation(row.Designation, now);
            }

            var articleStatus = articleStatuses.GetValueOrDefault(
                row.NumeroOFNormalise,
                StatutRapprochementX3.ArticlePrincipalIntrouvable);
            if (articleStatus != StatutRapprochementX3.Rapproche)
            {
                movement.DefinirRapprochement(order.Id, component.Id, articleStatus);
                continue;
            }

            movement.DefinirRapprochement(order.Id, component.Id, StatutRapprochementX3.Rapproche);
            matchedMovements.Add((movement, order, component));
        }

        foreach (var group in matchedMovements.GroupBy(item => new
                 {
                      item.Movement.DateImputation,
                      OrderId = item.Order.Id,
                      ComponentId = item.Component.Id,
                      item.Movement.Unite
                 }))
        {
            var first = group.First();
            var signedTotal = group.Sum(item => item.Movement.QuantiteX3Signee);
            batch.AjouterConsommation(ConsommationComposant.Creer(
                batch.Id,
                group.Key.DateImputation,
                first.Order,
                first.Component,
                first.Movement.Unite!,
                signedTotal,
                now));
        }

        batch.Terminer(
            encoursRead.TotalRows,
            encoursRead.Rows.Count,
            mouvementsRead.TotalRows,
            mouvementsRead.Rows.Count,
            dateTimeProvider.UtcNow);
        return ToImportResponse(batch, false);
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
            order.Mouvements
                .Select(item => new X3MouvementStockResponse(
                    item.Id,
                    item.NumeroLigneSource,
                    item.DateImputation,
                    item.Code,
                    item.QuantiteX3Signee,
                    item.Unite,
                    item.Transaction,
                    item.TypePiece,
                    item.PieceOrigine,
                    item.StatutRapprochement.ToString()))
                .ToArray(),
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

    private static void AjouterAnomalieMouvement(
        ImportX3Batch batch,
        X3MouvementStock movement,
        StatutRapprochementX3 status,
        string message,
        DateTime now,
        Guid? orderId = null)
    {
        movement.DefinirRapprochement(orderId, null, status);
        batch.AjouterAnomalie(ImportX3Anomaly.Creer(
            batch.Id,
            TypeFichierImportX3.MouvementsStock,
            movement.NumeroLigneSource,
            status,
            message,
            movement.NumeroOFNormalise,
            movement.Code,
            now,
            movement.NomFeuilleSource));
    }

    private static bool EstSortieMatiere(MouvementStockX3Row row)
    {
        return X3ImportParsing.NormaliserEntete(row.Transaction) == "sortieof" &&
               X3ImportParsing.NormaliserEntete(row.TypePiece) == "suivimatiere";
    }

    private static bool EstUniteConservee(string? unite)
    {
        return X3ImportParsing.NormaliserCode(unite) is "UN" or "KG" or "BA" or "M" or "LI";
    }

    private static ImportX3Response ToImportResponse(ImportX3Batch batch, bool duplicate)
    {
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
            batch.Consommations.OrderBy(item => item.DateImputation).ThenBy(item => item.NumeroOF)
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
            entity.ComposantId,
            entity.CodeComposant,
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
