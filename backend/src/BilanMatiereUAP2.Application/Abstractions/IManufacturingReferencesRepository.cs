using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public interface IManufacturingReferencesRepository
{
    Task<ImportBatch?> GetBatchByHashAsync(string hashSha256, CancellationToken cancellationToken = default);
    Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default);
    Task UpdateBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default);
    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(IDisposable tx, CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(IDisposable tx, CancellationToken cancellationToken = default);
    Task MarkBatchAsEchecAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default);
    Task<RefGammeFabrication?> GetGammeByCleMetierAsync(string cleMetier, CancellationToken cancellationToken = default);
    Task<RefNomenclature?> GetNomenclatureByCleMetierAsync(string cleMetier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefGammeFabrication>> GetGammesActivesByArticleAsync(string codeArticle, string? numeroOperation = null, CancellationToken cancellationToken = default);
    Task<int> CountNomenclaturesActivesByParentAsync(string codeArticleParent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefGammeFabrication>> GetGammesByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefNomenclature>> GetNomenclaturesByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default);
    Task AddRefGammesAsync(IEnumerable<RefGammeFabrication> gammes, CancellationToken cancellationToken = default);
    Task AddRefNomenclaturesAsync(IEnumerable<RefNomenclature> nomenclatures, CancellationToken cancellationToken = default);
    Task<int> DisableMissingGammesAsync(Guid importBatchId, IEnumerable<string> existingCleMetiers, CancellationToken cancellationToken = default);
    Task<int> DisableMissingNomenclaturesAsync(Guid importBatchId, IEnumerable<string> existingCleMetiers, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefGammeFabrication>> QueryGammesAsync(string? codeArticle = null, string? atelier = null, string? numeroOperation = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefNomenclature>> QueryNomenclaturesAsync(string? codeArticleParent = null, string? codeComposant = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}
