using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public interface IGammeFabricationReader
{
    Task<IEnumerable<RefGammeFabrication>> ReadAsync(Stream excelStream, string sourceSystem, Guid importBatchId, CancellationToken cancellationToken = default);
}
