using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public interface INomenclatureReader
{
    Task<IEnumerable<RefNomenclature>> ReadAsync(Stream excelStream, string sourceSystem, Guid importBatchId, CancellationToken cancellationToken = default);
}
