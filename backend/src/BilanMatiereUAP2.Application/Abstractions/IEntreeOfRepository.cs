using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public interface IEntreeOfRepository
{
    Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, CancellationToken cancellationToken = default);
    Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default);
    Task UpdateBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default);
    Task MarkBatchAsEchecAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, EntreeOfComposant>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<EntreeOfComposant> items, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntreeOfComposant>> GetByOfAndPeriodeAsync(string numeroOF, int annee, int mois, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntreeOfComposant>> GetByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntreeOfComposant>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task AddMouvementsBrutsAsync(
        IEnumerable<X3MouvementStock> mouvements,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
    Task<MouvementStockConsultationResult> QueryMouvementsAsync(
        MouvementStockConsultationFilter filter,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task<ImportTrackingSnapshot> GetImportTrackingAsync(
        TypeImport typeImport,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
