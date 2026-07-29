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
