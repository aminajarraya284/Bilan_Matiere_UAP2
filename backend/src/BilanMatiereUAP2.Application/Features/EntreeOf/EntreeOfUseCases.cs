using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Application.Features.Imports;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

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
}

public sealed class EntreeOfImportService(
    IMouvementStockX3Reader reader,
    IEntreeOfRepository repository,
    IImportArchiveService archiveService,
    IImportDependencyRefreshService dependencyRefreshService,
    ICurrentUserService currentUser) : IEntreeOfImportService
{
    private const string UniteAttendue = "UN";

    public async Task<ImportEntreeOfResponse> ImportAsync(Stream fileStream, string sourceSystem, string nomFichierOriginal, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAllBytesAsync(fileStream, cancellationToken);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
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

            // Meme reader generique que MVTSTOTRSVCR (Sortie OF) : seul le filtre de TypeTransaction
            // change. Les variantes d'accent (Entrée OF / Entree OF) sont normalisees en amont.
            var entreeRows = readResult.Rows
                .Where(row => X3ImportParsing.NormaliserEntete(row.Transaction) == "entreeof")
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
            var now = DateTime.UtcNow;

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

        if (quantiteNette < 0)
        {
            return new EntreeOfResolutionResult("ValeurInvalide", quantiteNette, article, "La quantité nette Entrée OF est négative.");
        }

        return new EntreeOfResolutionResult("Trouve", quantiteNette, article, null);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}
