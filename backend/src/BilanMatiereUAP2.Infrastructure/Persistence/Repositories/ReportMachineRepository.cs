using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class ReportMachineRepository(BilanMatiereUAP2DbContext context) : IReportMachineRepository
{
    public Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, string profilImport, CancellationToken cancellationToken = default)
        => context.ImportBatches.AsNoTracking()
            .Where(batch => batch.HashFichierSha256 == hashSha256 &&
                            batch.TypeImport == TypeImport.RapportMachine &&
                            batch.ProfilImport == profilImport.Trim() &&
                            (batch.Statut == StatutImport.Termine ||
                             batch.Statut == StatutImport.TermineAvecAvertissementArchive))
            .OrderByDescending(batch => batch.DateCreationUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default)
        => context.ImportBatches.AddAsync(batch, cancellationToken).AsTask();

    public async Task AddAnomaliesAsync(IEnumerable<ImportReferenceAnomaly> anomalies, CancellationToken cancellationToken = default)
        => await context.ImportReferenceAnomalies.AddRangeAsync(anomalies, cancellationToken);

    public async Task<IReadOnlyList<X3RapportMachine>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
    {
        var result = new List<X3RapportMachine>();
        foreach (var chunk in cleMetiers.Distinct().Chunk(1000))
        {
            result.AddRange(await context.X3RapportsMachine.Where(row => chunk.Contains(row.CleMetier)).ToListAsync(cancellationToken));
        }

        return result;
    }

    public Task AddRowsAsync(IEnumerable<X3RapportMachine> rows, CancellationToken cancellationToken = default)
        => context.X3RapportsMachine.AddRangeAsync(rows, cancellationToken);

    public async Task<IReadOnlyList<X3RapportMachine>> GetByOfAndArticleAsync(string numeroOF, string codeArticle, CancellationToken cancellationToken = default)
        => await context.X3RapportsMachine.AsNoTracking()
            .Where(row => row.IsActive && row.NumeroOF == numeroOF && row.CodeArticle == codeArticle)
            .OrderBy(row => row.DateProduction)
            .ThenBy(row => row.NumeroOperation)
            .ThenBy(row => row.CodePoste)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<X3RapportMachine>> GetByOfArticleAndPeriodeAsync(string numeroOF, string codeArticle, int annee, int mois, CancellationToken cancellationToken = default)
        => await context.X3RapportsMachine.AsNoTracking()
            .Where(row => row.IsActive
                && row.NumeroOF == numeroOF
                && row.CodeArticle == codeArticle
                && row.DateProduction.Year == annee
                && row.DateProduction.Month == mois)
            .OrderBy(row => row.DateProduction)
            .ThenBy(row => row.NumeroOperation)
            .ThenBy(row => row.CodePoste)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<X3RapportMachine>> GetByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
        => await context.X3RapportsMachine.AsNoTracking()
            .Where(row => row.IsActive && row.DateProduction.Year == annee && row.DateProduction.Month == mois)
            .OrderBy(row => row.NumeroOF)
            .ThenBy(row => row.CodeArticle)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<X3RapportMachine>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        => await context.X3RapportsMachine.AsNoTracking()
            .Where(row => row.IsActive && row.DateProduction == date)
            .OrderBy(row => row.NumeroOF)
            .ThenBy(row => row.CodePoste)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<X3RapportMachine>> QueryAsync(string? numeroOF = null, string? codeArticle = null, string? codePoste = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = context.X3RapportsMachine.AsNoTracking().Where(row => row.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(numeroOF)) query = query.Where(row => row.NumeroOF == numeroOF);
        if (!string.IsNullOrWhiteSpace(codeArticle)) query = query.Where(row => row.CodeArticle == codeArticle);
        if (!string.IsNullOrWhiteSpace(codePoste)) query = query.Where(row => row.CodePoste == codePoste);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(row => row.DateProduction).ThenBy(row => row.NumeroOF).ThenBy(row => row.CodeArticle)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<X3RapportMachine>(items, total);
    }

    public async Task<RapportMachineConsultationResult> QueryConsultationAsync(
        RapportMachineConsultationFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.X3RapportsMachine
            .AsNoTracking()
            .Where(row => row.IsActive)
            .AsQueryable();

        if (filter.DateDebut is not null)
            query = query.Where(row => row.DateProduction >= filter.DateDebut.Value);
        if (filter.DateFin is not null)
            query = query.Where(row => row.DateProduction <= filter.DateFin.Value);
        if (!string.IsNullOrWhiteSpace(filter.NumeroOF))
            query = query.Where(row => row.NumeroOF.Contains(filter.NumeroOF));
        if (!string.IsNullOrWhiteSpace(filter.CodeArticle))
            query = query.Where(row => row.CodeArticle.Contains(filter.CodeArticle));
        if (!string.IsNullOrWhiteSpace(filter.CodePoste))
            query = query.Where(row => row.CodePoste.Contains(filter.CodePoste));
        if (filter.Processus is not null)
            query = query.Where(row => row.OrigineProcessus == filter.Processus.Value);
        if (filter.ImportBatchId is not null)
            query = query.Where(row => row.ImportBatchId == filter.ImportBatchId.Value);

        var total = await query.CountAsync(cancellationToken);
        var nombreOf = await query.Select(row => row.NumeroOF).Distinct().CountAsync(cancellationToken);
        var processCounts = await query
            .GroupBy(row => row.OrigineProcessus)
            .Select(group => new { Processus = group.Key, Nombre = group.Count() })
            .ToDictionaryAsync(item => item.Processus, item => item.Nombre, cancellationToken);

        var lignesRejetees = filter.ImportBatchId is not null
            ? await context.ImportBatches
                .AsNoTracking()
                .Where(batch => batch.Id == filter.ImportBatchId.Value &&
                                batch.TypeImport == TypeImport.RapportMachine)
                .SumAsync(batch => (int?)batch.RejectedCount, cancellationToken) ?? 0
            : await context.ImportBatches
                .AsNoTracking()
                .Where(batch => batch.TypeImport == TypeImport.RapportMachine &&
                                query.Select(row => row.ImportBatchId).Distinct().Contains(batch.Id))
                .SumAsync(batch => (int?)batch.RejectedCount, cancellationToken) ?? 0;

        var ordered = filter.SortBy switch
        {
            "numeroof" => filter.Descending
                ? query.OrderByDescending(row => row.NumeroOF)
                : query.OrderBy(row => row.NumeroOF),
            "article" => filter.Descending
                ? query.OrderByDescending(row => row.CodeArticle)
                : query.OrderBy(row => row.CodeArticle),
            "poste" => filter.Descending
                ? query.OrderByDescending(row => row.CodePoste)
                : query.OrderBy(row => row.CodePoste),
            "operation" => filter.Descending
                ? query.OrderByDescending(row => row.NumeroOperation)
                : query.OrderBy(row => row.NumeroOperation),
            "processus" => filter.Descending
                ? query.OrderByDescending(row => row.OrigineProcessus)
                : query.OrderBy(row => row.OrigineProcessus),
            "quantitetotale" => filter.Descending
                ? query.OrderByDescending(row => row.QuantiteTotale)
                : query.OrderBy(row => row.QuantiteTotale),
            "quantiterebut" => filter.Descending
                ? query.OrderByDescending(row => row.QuantiteRebut)
                : query.OrderBy(row => row.QuantiteRebut),
            "quantitenette" => filter.Descending
                ? query.OrderByDescending(row => row.QuantiteTotale - row.QuantiteRebut)
                : query.OrderBy(row => row.QuantiteTotale - row.QuantiteRebut),
            "lotimport" => filter.Descending
                ? query.OrderByDescending(row => row.ImportBatchId)
                : query.OrderBy(row => row.ImportBatchId),
            _ => filter.Descending
                ? query.OrderByDescending(row => row.DateProduction)
                : query.OrderBy(row => row.DateProduction)
        };

        var items = await ordered
            .ThenBy(row => row.NumeroOF)
            .ThenBy(row => row.CodeArticle)
            .ThenBy(row => row.CodePoste)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new RapportMachineConsultationResult(
            items,
            total,
            new RapportMachineKpiResult(
                total,
                nombreOf,
                processCounts.GetValueOrDefault(OrigineProcessus.Ctrl),
                processCounts.GetValueOrDefault(OrigineProcessus.Sci),
                processCounts.GetValueOrDefault(OrigineProcessus.Emr),
                processCounts.GetValueOrDefault(OrigineProcessus.Trn),
                processCounts.GetValueOrDefault(OrigineProcessus.Inconnue),
                lignesRejetees),
            await GetImportTrackingAsync(TypeImport.RapportMachine, cancellationToken));
    }

    public async Task<ImportTrackingSnapshot> GetImportTrackingAsync(
        TypeImport typeImport,
        CancellationToken cancellationToken = default)
    {
        var batches = context.ImportBatches
            .AsNoTracking()
            .Where(batch => batch.TypeImport == typeImport);

        var derniereTentative = await batches
            .OrderByDescending(batch => batch.DateCreationUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var dernierImportReussi = await batches
            .Where(batch => batch.Statut == StatutImport.Termine ||
                            batch.Statut == StatutImport.TermineAvecAvertissementArchive)
            .OrderByDescending(batch => batch.DateCreationUtc)
            .FirstOrDefaultAsync(cancellationToken);

        async Task<ImportBatchSnapshot?> ToSnapshotAsync(ImportBatch? batch)
        {
            if (batch is null) return null;

            var nomFichier = await context.ImportSourceFiles
                .AsNoTracking()
                .Where(file => file.ImportBatchId == batch.Id && file.TypeImport == typeImport)
                .OrderByDescending(file => file.DateUploadUtc)
                .Select(file => file.NomFichierOriginal)
                .FirstOrDefaultAsync(cancellationToken);
            return new ImportBatchSnapshot(batch, nomFichier);
        }

        return new ImportTrackingSnapshot(
            await ToSnapshotAsync(derniereTentative),
            await ToSnapshotAsync(dernierImportReussi));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default)
    {
        if (transaction is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction efTransaction)
        {
            await efTransaction.CommitAsync(cancellationToken);
            efTransaction.Dispose();
            return;
        }

        transaction.Dispose();
    }

    public async Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default)
    {
        if (transaction is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction efTransaction)
        {
            await efTransaction.RollbackAsync(cancellationToken);
            efTransaction.Dispose();
            return;
        }

        transaction.Dispose();
    }

    public Task MarkBatchAsFailedAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default)
        => context.ImportBatches.Where(batch => batch.Id == batchId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(batch => batch.Statut, StatutImport.Echec)
                .SetProperty(batch => batch.DateFinUtc, dateFinUtc), cancellationToken);

    public async Task<IReadOnlyList<X3RapportMachine>> GetAllActiveTrackedAsync(CancellationToken cancellationToken = default)
        => await context.X3RapportsMachine.Where(row => row.IsActive).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PosteNonMappeSummary>> GetPostesNonMappesAsync(CancellationToken cancellationToken = default)
    {
        // Projection minimale puis agregation en memoire : Min/Max sur DateOnly converti ne se
        // traduit pas de facon fiable sur tous les providers EF (ex. Sqlite utilise en tests).
        var rows = await context.X3RapportsMachine.AsNoTracking()
            .Where(row => row.IsActive && row.OrigineProcessus == OrigineProcessus.Inconnue)
            .Select(row => new { row.CodePoste, row.QuantiteTotale, row.QuantiteRebut, row.DateProduction, row.CodeArticle, row.NumeroOF })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.CodePoste)
            .Select(group => new PosteNonMappeSummary(
                group.Key,
                group.Count(),
                group.Sum(row => row.QuantiteTotale),
                group.Sum(row => row.QuantiteRebut),
                group.Min(row => row.DateProduction),
                group.Max(row => row.DateProduction),
                group.OrderByDescending(row => row.DateProduction).Select(row => row.CodeArticle).Distinct().Take(3).ToArray(),
                group.OrderByDescending(row => row.DateProduction).Select(row => row.NumeroOF).Distinct().Take(3).ToArray()))
            .OrderByDescending(summary => summary.NombreLignes)
            .ToArray();
    }
}
