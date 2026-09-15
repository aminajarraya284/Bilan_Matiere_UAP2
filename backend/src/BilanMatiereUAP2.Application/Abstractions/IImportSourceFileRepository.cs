using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public interface IImportSourceFileRepository
{
    Task<IReadOnlyList<ImportSourceFile>> GetByBatchIdAsync(Guid importBatchId, CancellationToken cancellationToken = default);
    Task<ImportSourceFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ImportSourceFile?> GetLatestByProfileAsync(string profilImport, CancellationToken cancellationToken = default);
    Task AddAsync(ImportSourceFile file, CancellationToken cancellationToken = default);
}
