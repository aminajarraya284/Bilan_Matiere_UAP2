using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Application.Features.Imports;
using BilanMatiereUAP2.Contracts.Imports;
using BilanMatiereUAP2.Contracts.MouvementsStock;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.EntreeOf;

public sealed record ImportEntreeOfResponse(
    Guid ImportBatchId,
    bool Duplicate,
    int Inserted,
    int Updated,
    int Unchanged,
    int Rejected,
    int RowsRead,
    int RowsValid,
    string Statut,
    string? ArchiveErrorMessage);

public sealed record EntreeOfResolutionResult(string Statut, decimal? QuantiteNetteEntreeOfPieces, string? CodeArticle, string? Message);

public interface IEntreeOfImportService
{
    Task<ImportEntreeOfResponse> ImportAsync(Stream fileStream, string sourceSystem, string nomFichierOriginal, CancellationToken cancellationToken = default);
    Task<EntreeOfResolutionResult> ResolveAsync(string numeroOF, int annee, int mois, string? codeArticleAttendu, CancellationToken cancellationToken = default);
    Task<MouvementsStockConsultationResponse> QueryMouvementsAsync(
        MouvementStockConsultationQuery query,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task<ImportTrackingResponse> GetImportTrackingAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

public sealed class EntreeOfImportService(
    IMouvementStockX3Reader reader,
    IEntreeOfRepository repository,
    IImportArchiveService archiveService,
    IImportDependencyRefreshService dependencyRefreshService,
    ICurrentUserService currentUser,
    IImportHashCalculator hashCalculator,
    IParametreApplicationProvider parametres) : IEntreeOfImportService
{
    private const string UniteAttendue = "UN";

    public async Task<ImportEntreeOfResponse> ImportAsync(Stream fileStream, string sourceSystem, string nomFichierOriginal, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAllBytesAsync(fileStream, cancellationToken);
        var hash = hashCalculator.CalculateSha256(bytes);
        var existing = await repository.GetCompletedBatchByHashAsync(hash, cancellationToken);
        var batch = ImportBatch.Demarrer(
            hash,
            TypeImport.MvtEntreeOF,
            "MVT_ENTREE_OF_V1",
            currentUser.UserId,
            currentUser.UserName,
            DateTime.UtcNow);
        await repository.AddBatchAsync(batch, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        if (existing is not null)
        {
            batch.TerminerImport(StatutImport.Duplicate, DateTime.UtcNow, 0, 0, 0, 0, 0, 0, 0);
            await repository.UpdateBatchAsync(batch, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await ArchiverEtTracerErreurAsync(batch, bytes, nomFichierOriginal, cancellationToken);
            return ToResponse(batch, true);
        }

        IDisposable? tx = null;
        try
        {
            tx = await repository.BeginTransactionAsync(cancellationToken);

            var file = new X3ImportFileContent(nomFichierOriginal, bytes.Length, bytes);
            var readResult = await reader.ReadAsync(file, cancellationToken);
            var now = DateTime.UtcNow;

            var transactionEntreeOf = X3ImportParsing.NormaliserEntete(
                await parametres.GetValueAsync(ParametresApplicationCles.TransactionEntreeOf, cancellationToken));
            var mouvementsBruts = readResult.Rows
                .Select(row => X3MouvementStock.Creer(
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
                    DeterminerNature(row, transactionEntreeOf),
                    now))
                .ToArray();
            if (mouvementsBruts.Length > 0)
                await repository.AddMouvementsBrutsAsync(mouvementsBruts, cancellationToken);

            // Meme reader generique que MVTSTOTRSVCR (Sortie OF) : seul le filtre de TypeTransaction
            // change. Les variantes d'accent sont normalisees en amont.
            var entreeRows = readResult.Rows
                .Where(row => X3ImportParsing.NormaliserEntete(row.Transaction) == transactionEntreeOf)
                .Where(row => !string.IsNullOrWhiteSpace(row.NumeroOFNormalise) && X3ImportParsing.EstNumeroOF(row.NumeroOFNormalise))
                .Where(row => !string.IsNullOrWhiteSpace(row.Code) && !string.IsNullOrWhiteSpace(row.Unite))
                .ToArray();

            var grouped = entreeRows
                .GroupBy(row => new { row.NumeroOFNormalise, Code = row.Code!.Trim().ToUpperInvariant(), Unite = row.Unite!.Trim().ToUpperInvariant(), row.DateImputation })
                .Select(group => new
                {
                    CleMetier = EntreeOfComposant.GenererCleMetier(group.Key.NumeroOFNormalise, group.Key.Code, group.Key.Unite, group.Key.DateImputation),
                    group.Key.NumeroOFNormalise,
                    group.Key.Code,
                    group.Key.Unite,
                    group.Key.DateImputation,
                    Designation = group.First().Designation,
                    QuantiteNette = group.Sum(row => row.QuantiteX3Signee)
                })
                .ToArray();

            var existingByKey = await repository.GetByCleMetiersAsync(grouped.Select(g => g.CleMetier).ToArray(), cancellationToken);
            var toAdd = new List<EntreeOfComposant>();
            var inserted = 0;
            var updated = 0;
            var unchanged = 0;

            foreach (var g in grouped)
            {
                if (existingByKey.TryGetValue(g.CleMetier, out var existingRow))
                {
                    if (existingRow.EstEquivalent(g.Designation, g.QuantiteNette))
                    {
                        unchanged++;
                    }
                    else
                    {
                        existingRow.MettreAJour(g.Designation, g.QuantiteNette, batch.Id, now);
                        updated++;
                    }
                }
                else
                {
                    toAdd.Add(EntreeOfComposant.Creer(batch.Id, g.DateImputation, g.NumeroOFNormalise, g.Code, g.Designation, g.Unite, g.QuantiteNette, now));
                    inserted++;
                }
            }

            if (toAdd.Count > 0) await repository.AddRangeAsync(toAdd, cancellationToken);

            var rejected = readResult.Errors.Count;
            var valid = inserted + updated + unchanged;
            batch.TerminerImport(
                StatutImport.Termine,
                now,
                valid + rejected,
                valid,
                inserted,
                updated,
                unchanged,
                rejected,
                readResult.Errors.Count);
            await repository.UpdateBatchAsync(batch, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await repository.CommitTransactionAsync(tx, cancellationToken);
            tx = null;
            await dependencyRefreshService.RefreshAfterMvtEntreeOfAsync(cancellationToken);
            await ArchiverEtTracerErreurAsync(batch, bytes, nomFichierOriginal, cancellationToken);
            return ToResponse(batch, false);
        }
        catch
        {
            if (tx is not null)
            {
                try { await repository.RollbackTransactionAsync(tx, cancellationToken); } catch { }
            }

            try { await repository.MarkBatchAsEchecAsync(batch.Id, DateTime.UtcNow, cancellationToken); } catch { }
            throw;
        }
    }

    private async Task ArchiverEtTracerErreurAsync(
        ImportBatch batch,
        byte[] bytes,
        string nomFichierOriginal,
        CancellationToken cancellationToken)
    {
        try
        {
            await archiveService.ArchiveAsync(
                batch.Id,
                TypeImport.MvtEntreeOF,
                "MVT_ENTREE_OF_V1",
                nomFichierOriginal,
                bytes,
                currentUser.UserId,
                currentUser.UserName,
                cancellationToken);
        }
        catch (Exception exception)
        {
            batch.SignalerErreurArchive(exception.Message);
            await repository.UpdateBatchAsync(batch, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
    }

    private static ImportEntreeOfResponse ToResponse(ImportBatch batch, bool duplicate)
        => new(
            batch.Id,
            duplicate,
            batch.InsertedCount,
            batch.UpdatedCount,
            batch.UnchangedCount,
            batch.RejectedCount,
            batch.ReadCount,
            batch.ValidCount,
            batch.Statut.ToString(),
            batch.ArchiveErrorMessage);

    public async Task<EntreeOfResolutionResult> ResolveAsync(string numeroOF, int annee, int mois, string? codeArticleAttendu, CancellationToken cancellationToken = default)
    {
        var normalizedOF = numeroOF.Trim().ToUpperInvariant();
        var rows = await repository.GetByOfAndPeriodeAsync(normalizedOF, annee, mois, cancellationToken);
        var expectedArticle = string.IsNullOrWhiteSpace(codeArticleAttendu)
            ? null
            : codeArticleAttendu.Trim().ToUpperInvariant();
        if (expectedArticle is not null)
        {
            rows = rows
                .Where(row => string.Equals(row.CodeArticle, expectedArticle, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (rows.Count == 0)
        {
            return new EntreeOfResolutionResult("MouvementAbsent", null, expectedArticle, "Aucun mouvement Entrée OF pour cet OF, cet article et cette période.");
        }

        var distinctArticles = rows.Select(r => r.CodeArticle).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (distinctArticles.Length > 1)
        {
            return new EntreeOfResolutionResult("Ambigu", null, null, $"Plusieurs articles distincts en Entrée OF pour cet OF : {string.Join(", ", distinctArticles)}.");
        }

        var article = distinctArticles[0];
        var distinctUnits = rows.Select(r => r.Unite).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (distinctUnits.Length != 1 || !string.Equals(distinctUnits[0], UniteAttendue, StringComparison.OrdinalIgnoreCase))
        {
            return new EntreeOfResolutionResult("UniteIncompatible", null, article, $"Unité(s) rencontrée(s) : {string.Join(", ", distinctUnits)}. Unité attendue : {UniteAttendue}.");
        }

        var quantiteNette = rows.Sum(r => r.QuantiteNetteSignee);
        if (quantiteNette == 0)
        {
            return new EntreeOfResolutionResult("Trouve", 0m, article, null);
        }

        return new EntreeOfResolutionResult("Trouve", quantiteNette, article, null);
    }

    public async Task<MouvementsStockConsultationResponse> QueryMouvementsAsync(
        MouvementStockConsultationQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
            throw Validation(nameof(query.Page), "La page doit être supérieure ou égale à 1.");
        if (query.PageSize is < 1 or > 100)
            throw Validation(nameof(query.PageSize), "La taille de page doit être comprise entre 1 et 100.");
        if (query.DateDebut is not null && query.DateFin is not null && query.DateDebut > query.DateFin)
            throw Validation(nameof(query.DateFin), "La date de fin doit être postérieure ou égale à la date de début.");

        var result = await repository.QueryMouvementsAsync(
            new MouvementStockConsultationFilter(
                query.DateDebut,
                query.DateFin,
                NormalizeOptional(query.NumeroOF),
                NormalizeOptional(query.CodeArticle),
                NormalizeOptional(query.Designation),
                NormalizeOptional(query.Transaction),
                NormalizeOptional(query.TypePiece),
                NormalizeOptional(query.PieceOrigine),
                ParseNature(query.Nature),
                query.ImportBatchId,
                query.Page,
                query.PageSize,
                ParseSort(query.SortBy),
                ParseDescending(query.SortDirection)),
            cancellationToken);

        return new MouvementsStockConsultationResponse(
            result.Items.Select(ToMouvementResponse).ToArray(),
            result.Total,
            query.Page,
            query.PageSize,
            ToTrackingResponse(result.Imports));
    }

    public async Task<ImportTrackingResponse> GetImportTrackingAsync(CancellationToken cancellationToken = default)
        => ToTrackingResponse(await repository.GetImportTrackingAsync(TypeImport.MvtEntreeOF, cancellationToken));

    private static NatureMouvementStock DeterminerNature(
        MouvementStockX3Row row,
        string transactionEntreeOf)
    {
        var transaction = X3ImportParsing.NormaliserEntete(row.Transaction);
        if (!string.IsNullOrEmpty(transactionEntreeOf) && transaction == transactionEntreeOf)
            return NatureMouvementStock.EntreeOF;

        return transaction == "sortieof" &&
               X3ImportParsing.NormaliserEntete(row.TypePiece) == "suivimatiere" &&
               X3ImportParsing.EstNumeroOF(row.PieceOrigine)
            ? NatureMouvementStock.SortieMP
            : NatureMouvementStock.NonClassee;
    }

    private static MouvementStockResponse ToMouvementResponse(X3MouvementStock row)
        => new(
            row.Id,
            row.DateImputation,
            string.IsNullOrWhiteSpace(row.NumeroOFNormalise) ? null : row.NumeroOFNormalise,
            row.Code,
            row.Designation,
            row.QuantiteX3Signee,
            row.Unite,
            row.Transaction,
            row.TypePiece,
            row.PieceOrigine,
            row.Nature.ToString(),
            row.ImportBatchId);

    private static ImportTrackingResponse ToTrackingResponse(ImportTrackingSnapshot tracking)
        => new(
            ToImportExecutionResponse(tracking.DerniereTentative),
            ToImportExecutionResponse(tracking.DernierImportReussi));

    private static ImportExecutionSummaryResponse? ToImportExecutionResponse(ImportBatchSnapshot? snapshot)
    {
        if (snapshot is null) return null;
        var batch = snapshot.Batch;
        var duplicate = batch.Statut == StatutImport.Duplicate;
        return new ImportExecutionSummaryResponse(
            batch.Id,
            batch.Statut switch
            {
                StatutImport.EnCours => "En cours",
                StatutImport.Termine => "Importé",
                StatutImport.Partiel => "Partiel",
                StatutImport.Echec => "Échec",
                StatutImport.Duplicate => "Fichier identique",
                StatutImport.TermineAvecAvertissementArchive => "Importé avec avertissement",
                _ => batch.Statut.ToString()
            },
            duplicate
                ? "Ce fichier a déjà été importé. Aucune donnée n’a été réécrite."
                : batch.ArchiveErrorMessage,
            snapshot.NomFichier,
            batch.DateCreationUtc,
            batch.DateFinUtc,
            batch.ReadCount,
            batch.ValidCount,
            batch.RejectedCount,
            batch.InsertedCount,
            batch.UpdatedCount,
            batch.UnchangedCount);
    }

    private static NatureMouvementStock? ParseNature(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToUpperInvariant() switch
        {
            "ENTREEOF" => NatureMouvementStock.EntreeOF,
            "SORTIEMP" => NatureMouvementStock.SortieMP,
            "NONCLASSEE" => NatureMouvementStock.NonClassee,
            _ => throw Validation(nameof(MouvementStockConsultationQuery.Nature), "La nature doit être EntreeOF, SortieMP ou NonClassee.")
        };
    }

    private static string ParseSort(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? "date";
        return normalized is "date" or "numeroof" or "article" or "designation" or "quantite" or
            "transaction" or "typepiece" or "pieceorigine" or "nature" or "lotimport"
            ? normalized
            : throw Validation(nameof(MouvementStockConsultationQuery.SortBy), "Le champ de tri est invalide.");
    }

    private static bool ParseDescending(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase)) return false;
        throw Validation(nameof(MouvementStockConsultationQuery.SortDirection), "Le sens de tri doit être asc ou desc.");
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static ValidationException Validation(string propertyName, string message)
        => new([new ValidationFailure(propertyName, message)]);

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}
