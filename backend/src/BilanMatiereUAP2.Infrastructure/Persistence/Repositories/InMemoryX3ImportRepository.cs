using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class InMemoryX3ImportRepository : IX3ImportRepository
{
    private readonly Dictionary<string, ImportX3Batch> _batchesByHash = new();
    private readonly Dictionary<Guid, ImportX3Batch> _batchesById = new();
    private readonly Dictionary<string, X3OrdreFabrication> _ordresByNumero = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConsommationComposant> _consommationsByCle = new(StringComparer.OrdinalIgnoreCase);

    public Task<ImportX3Batch?> GetCompletedBatchByPairHashAsync(string pairHash, CancellationToken cancellationToken)
    {
        _batchesByHash.TryGetValue(pairHash, out var batch);
        if (batch is not null && (batch.Statut == StatutRapprochementX3.Rapproche || batch.Statut == StatutRapprochementX3.ImportPartiel))
        {
            return Task.FromResult<ImportX3Batch?>(batch);
        }
        return Task.FromResult<ImportX3Batch?>(null);
    }

    public Task<ImportX3Batch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _batchesById.TryGetValue(id, out var batch);
        return Task.FromResult(batch);
    }

    public Task<IReadOnlyList<ImportX3Anomaly>> GetAnomaliesAsync(Guid batchId, CancellationToken cancellationToken)
    {
        if (_batchesById.TryGetValue(batchId, out var batch))
        {
            return Task.FromResult<IReadOnlyList<ImportX3Anomaly>>(batch.Anomalies.ToArray());
        }

        return Task.FromResult<IReadOnlyList<ImportX3Anomaly>>([]);
    }

    public Task<IReadOnlyDictionary<string, X3OrdreFabrication>> GetOrdresByNumeroAsync(
        IReadOnlyCollection<string> numerosOF,
        CancellationToken cancellationToken)
    {
        var result = numerosOF
            .Where(num => _ordresByNumero.ContainsKey(num))
            .ToDictionary(num => num, num => _ordresByNumero[num], StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyDictionary<string, X3OrdreFabrication>>(result);
    }

    public Task<X3OrdreFabrication?> GetOrdreContexteAsync(string numeroOF, CancellationToken cancellationToken)
    {
        _ordresByNumero.TryGetValue(numeroOF, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyDictionary<string, ConsommationComposant>> GetConsommationsByCleMetierAsync(
        IReadOnlyCollection<string> clesMetier,
        CancellationToken cancellationToken)
    {
        var result = clesMetier
            .Where(cle => _consommationsByCle.ContainsKey(cle))
            .ToDictionary(cle => cle, cle => _consommationsByCle[cle], StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyDictionary<string, ConsommationComposant>>(result);
    }

    public Task<PagedResult<ConsommationComposant>> GetConsommationsAsync(
        ConsommationsComposantsFilter filter,
        CancellationToken cancellationToken)
    {
        var items = _consommationsByCle.Values.ToList();
        return Task.FromResult(new PagedResult<ConsommationComposant>(items, items.Count));
    }

    public Task AddBatchAsync(ImportX3Batch batch, CancellationToken cancellationToken)
    {
        _batchesByHash[batch.HashCoupleSha256] = batch;
        _batchesById[batch.Id] = batch;
        return Task.CompletedTask;
    }

    public Task AddOrdresAsync(IEnumerable<X3OrdreFabrication> ordres, CancellationToken cancellationToken)
    {
        foreach (var ordre in ordres)
        {
            _ordresByNumero[ordre.NumeroOF] = ordre;
        }

        return Task.CompletedTask;
    }

    public Task AddConsommationsAsync(IEnumerable<ConsommationComposant> consommations, CancellationToken cancellationToken)
    {
        foreach (var consom in consommations)
        {
            _consommationsByCle[consom.CleMetier] = consom;
        }

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
