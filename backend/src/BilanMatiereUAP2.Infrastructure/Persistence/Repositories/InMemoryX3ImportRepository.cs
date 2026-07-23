using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class InMemoryX3ImportRepository : IX3ImportRepository
{
    private readonly Dictionary<string, ImportX3Batch> _batchesByHash = new();
    private readonly Dictionary<Guid, ImportX3Batch> _batchesById = new();
    private readonly Dictionary<string, X3OrdreFabrication> _ordresByNumero = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Composant> _composantsByCode = new(StringComparer.OrdinalIgnoreCase);

    public Task<ImportX3Batch?> GetBatchByPairHashAsync(string pairHash, CancellationToken cancellationToken)
    {
        _batchesByHash.TryGetValue(pairHash, out var batch);
        return Task.FromResult(batch);
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

    public Task<IReadOnlyDictionary<string, Composant>> GetComposantsByCodeAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken)
    {
        var result = codes
            .Where(code => _composantsByCode.ContainsKey(code))
            .ToDictionary(code => code, code => _composantsByCode[code], StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyDictionary<string, Composant>>(result);
    }

    public Task<PagedResult<ConsommationComposant>> GetConsommationsAsync(
        ConsommationsComposantsFilter filter,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new PagedResult<ConsommationComposant>([], 0));
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

    public Task AddComposantsAsync(IEnumerable<Composant> composants, CancellationToken cancellationToken)
    {
        foreach (var comp in composants)
        {
            _composantsByCode[comp.Code] = comp;
        }

        return Task.CompletedTask;
    }
}
