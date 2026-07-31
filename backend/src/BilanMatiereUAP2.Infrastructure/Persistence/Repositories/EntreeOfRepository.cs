using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class EntreeOfRepository(BilanMatiereUAP2DbContext context) : IEntreeOfRepository
{
    public Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, CancellationToken cancellationToken = default)
        => context.ImportBatches.AsNoTracking()
            .Where(batch => batch.HashFichierSha256 == hashSha256 &&
                            batch.TypeImport == TypeImport.MvtEntreeOF &&
                            (batch.Statut == StatutImport.Termine ||
                             batch.Statut == StatutImport.TermineAvecAvertissementArchive))
            .OrderByDescending(batch => batch.DateCreationUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default)
        => await context.ImportBatches.AddAsync(batch, cancellationToken);

    public Task UpdateBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default)
    {
        context.ImportBatches.Update(batch);
        return Task.CompletedTask;
    }

    public Task MarkBatchAsEchecAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default)
        => context.ImportBatches.Where(batch => batch.Id == batchId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(batch => batch.Statut, StatutImport.Echec)
                .SetProperty(batch => batch.DateFinUtc, dateFinUtc), cancellationToken);

    public async Task<IReadOnlyDictionary<string, EntreeOfComposant>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, EntreeOfComposant>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in cleMetiers.Distinct().Chunk(1000))
        {
            var rows = await context.EntreesOfComposants.Where(row => chunk.Contains(row.CleMetier)).ToListAsync(cancellationToken);
            foreach (var row in rows) result[row.CleMetier] = row;
        }

        return result;
    }

    public async Task AddRangeAsync(IEnumerable<EntreeOfComposant> items, CancellationToken cancellationToken = default)
        => await context.EntreesOfComposants.AddRangeAsync(items, cancellationToken);

    public async Task<IReadOnlyList<EntreeOfComposant>> GetByOfAndPeriodeAsync(string numeroOF, int annee, int mois, CancellationToken cancellationToken = default)
    {
        var debut = new DateOnly(annee, mois, 1);
        var fin = debut.AddMonths(1);
        return await context.EntreesOfComposants.AsNoTracking()
            .Where(row => row.NumeroOF == numeroOF && row.DateImputation >= debut && row.DateImputation < fin)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EntreeOfComposant>> GetByPeriodeAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default)
    {
        var debut = new DateOnly(annee, mois, 1);
        var fin = debut.AddMonths(1);
        return await context.EntreesOfComposants
            .AsNoTracking()
            .Where(row => row.DateImputation >= debut && row.DateImputation < fin)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EntreeOfComposant>> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
        => await context.EntreesOfComposants
            .AsNoTracking()
            .Where(row => row.DateImputation == date)
            .ToListAsync(cancellationToken);

    public async Task AddMouvementsBrutsAsync(
        IEnumerable<X3MouvementStock> mouvements,
        CancellationToken cancellationToken = default)
        => await context.X3MouvementsStock.AddRangeAsync(mouvements, cancellationToken);

    public async Task<MouvementStockConsultationResult> QueryMouvementsAsync(
        MouvementStockConsultationFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.X3MouvementsStock.AsNoTracking().AsQueryable();

        if (filter.DateDebut is not null)
            query = query.Where(row => row.DateImputation >= filter.DateDebut.Value);
        if (filter.DateFin is not null)
            query = query.Where(row => row.DateImputation <= filter.DateFin.Value);
        if (!string.IsNullOrWhiteSpace(filter.NumeroOF))
            query = query.Where(row => row.NumeroOFNormalise.Contains(filter.NumeroOF));
        if (!string.IsNullOrWhiteSpace(filter.CodeArticle))
            query = query.Where(row => row.Code != null && row.Code.Contains(filter.CodeArticle));
        if (!string.IsNullOrWhiteSpace(filter.Designation))
            query = query.Where(row => row.Designation != null && row.Designation.Contains(filter.Designation));
        if (!string.IsNullOrWhiteSpace(filter.Transaction))
            query = query.Where(row => row.Transaction != null && row.Transaction.Contains(filter.Transaction));
        if (!string.IsNullOrWhiteSpace(filter.TypePiece))
            query = query.Where(row => row.TypePiece != null && row.TypePiece.Contains(filter.TypePiece));
        if (!string.IsNullOrWhiteSpace(filter.PieceOrigine))
            query = query.Where(row => row.PieceOrigine != null && row.PieceOrigine.Contains(filter.PieceOrigine));
        if (filter.Nature is not null)
            query = query.Where(row => row.Nature == filter.Nature.Value);
        if (filter.ImportBatchId is not null)
            query = query.Where(row => row.ImportBatchId == filter.ImportBatchId.Value);

        var total = await query.CountAsync(cancellationToken);
        var ordered = filter.SortBy switch
        {
            "numeroof" => filter.Descending
                ? query.OrderByDescending(row => row.NumeroOFNormalise)
                : query.OrderBy(row => row.NumeroOFNormalise),
            "article" => filter.Descending
                ? query.OrderByDescending(row => row.Code)
                : query.OrderBy(row => row.Code),
            "designation" => filter.Descending
                ? query.OrderByDescending(row => row.Designation)
                : query.OrderBy(row => row.Designation),
            "quantite" => filter.Descending
                ? query.OrderByDescending(row => row.QuantiteX3Signee)
                : query.OrderBy(row => row.QuantiteX3Signee),
            "transaction" => filter.Descending
                ? query.OrderByDescending(row => row.Transaction)
                : query.OrderBy(row => row.Transaction),
            "typepiece" => filter.Descending
                ? query.OrderByDescending(row => row.TypePiece)
                : query.OrderBy(row => row.TypePiece),
            "pieceorigine" => filter.Descending
                ? query.OrderByDescending(row => row.PieceOrigine)
                : query.OrderBy(row => row.PieceOrigine),
            "nature" => filter.Descending
                ? query.OrderByDescending(row => row.Nature)
                : query.OrderBy(row => row.Nature),
            "lotimport" => filter.Descending
                ? query.OrderByDescending(row => row.ImportBatchId)
                : query.OrderBy(row => row.ImportBatchId),
            _ => filter.Descending
                ? query.OrderByDescending(row => row.DateImputation)
                : query.OrderBy(row => row.DateImputation)
        };

        var items = await ordered
            .ThenBy(row => row.NumeroLigneSource)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new MouvementStockConsultationResult(
            items,
            total,
            await GetImportTrackingAsync(TypeImport.MvtEntreeOF, cancellationToken));
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
