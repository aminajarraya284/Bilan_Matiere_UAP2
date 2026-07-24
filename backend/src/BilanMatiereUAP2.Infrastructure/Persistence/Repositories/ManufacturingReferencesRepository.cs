using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class ManufacturingReferencesRepository : IManufacturingReferencesRepository
{
    private readonly BilanMatiereUAP2DbContext _dbContext;

    public ManufacturingReferencesRepository(BilanMatiereUAP2DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ImportBatch?> GetBatchByHashAsync(string hashSha256, CancellationToken cancellationToken = default)
        => _dbContext.ImportBatches.AsNoTracking().SingleOrDefaultAsync(b => b.HashFichierSha256 == hashSha256, cancellationToken);

    public Task<RefGammeFabrication?> GetGammeByCleMetierAsync(string cleMetier, CancellationToken cancellationToken = default)
        => _dbContext.RefGammesFabrication.AsNoTracking().SingleOrDefaultAsync(g => g.CleMetier == cleMetier, cancellationToken);

    public Task<RefNomenclature?> GetNomenclatureByCleMetierAsync(string cleMetier, CancellationToken cancellationToken = default)
        => _dbContext.RefNomenclatures.AsNoTracking().SingleOrDefaultAsync(n => n.CleMetier == cleMetier, cancellationToken);

    // Recherche des gammes actives d'un article, filtrable par numero d'operation (origine Usinage 2).
    // Retourne toutes les correspondances : la resolution 0 / 1 / N est laissee a l'appelant (pas de choix arbitraire).
    public async Task<IReadOnlyList<RefGammeFabrication>> GetGammesActivesByArticleAsync(string codeArticle, string? numeroOperation = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RefGammesFabrication.AsNoTracking()
            .Where(g => g.IsActive && g.CodeArticle == codeArticle);
        if (!string.IsNullOrWhiteSpace(numeroOperation))
        {
            query = query.Where(g => g.NumeroOperation == numeroOperation);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<int> CountNomenclaturesActivesByParentAsync(string codeArticleParent, CancellationToken cancellationToken = default)
        => _dbContext.RefNomenclatures.AsNoTracking()
            .CountAsync(n => n.IsActive && n.CodeArticleParent == codeArticleParent, cancellationToken);

    // Chargement suivi par lot (tracked) : les entites retournees sont attachees au contexte,
    // de sorte que les appels a MettreAJour sont bien persistes lors du SaveChanges.
    // Le decoupage en lots evite de depasser la limite de parametres SQL Server (~2100).
    public async Task<IReadOnlyList<RefGammeFabrication>> GetGammesByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
    {
        var result = new List<RefGammeFabrication>();
        foreach (var chunk in cleMetiers.Distinct().Chunk(1000))
        {
            var found = await _dbContext.RefGammesFabrication
                .Where(g => chunk.Contains(g.CleMetier))
                .ToListAsync(cancellationToken);
            result.AddRange(found);
        }

        return result;
    }

    public async Task<IReadOnlyList<RefNomenclature>> GetNomenclaturesByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
    {
        var result = new List<RefNomenclature>();
        foreach (var chunk in cleMetiers.Distinct().Chunk(1000))
        {
            var found = await _dbContext.RefNomenclatures
                .Where(n => chunk.Contains(n.CleMetier))
                .ToListAsync(cancellationToken);
            result.AddRange(found);
        }

        return result;
    }

    public Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default)
        => _dbContext.ImportBatches.AddAsync(batch, cancellationToken).AsTask();

    public Task UpdateBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default)
    {
        _dbContext.ImportBatches.Update(batch);
        return Task.CompletedTask;
    }

    public async Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        return tx;
    }

    public async Task CommitTransactionAsync(IDisposable tx, CancellationToken cancellationToken = default)
    {
        if (tx is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction efTx)
        {
            await efTx.CommitAsync(cancellationToken);
            efTx.Dispose();
        }
        else
        {
            tx.Dispose();
        }
    }

    public async Task RollbackTransactionAsync(IDisposable tx, CancellationToken cancellationToken = default)
    {
        if (tx is Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction efTx)
        {
            await efTx.RollbackAsync(cancellationToken);
            efTx.Dispose();
        }
        else
        {
            tx.Dispose();
        }
    }

    // Marque le batch en echec via une mise a jour SQL directe (ExecuteUpdate) :
    // le batch a ete valide (EnCours) avant l'ouverture de la transaction metier, donc la ligne existe
    // toujours apres un rollback. Cette voie contourne le change-tracker (potentiellement pollue par
    // les entites de l'import echoue), ce qui garantit que seul le statut du batch est ecrit.
    public Task MarkBatchAsEchecAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default)
        => _dbContext.ImportBatches
            .Where(b => b.Id == batchId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Statut, StatutImport.Echec)
                .SetProperty(b => b.DateFinUtc, dateFinUtc), cancellationToken);

    public async Task AddRefGammesAsync(IEnumerable<RefGammeFabrication> gammes, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefGammesFabrication.AddRangeAsync(gammes, cancellationToken);
    }

    public async Task AddRefNomenclaturesAsync(IEnumerable<RefNomenclature> nomenclatures, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefNomenclatures.AddRangeAsync(nomenclatures, cancellationToken);
    }

    public Task<int> DisableMissingGammesAsync(Guid importBatchId, IEnumerable<string> existingCleMetiers, CancellationToken cancellationToken = default)
    {
        var list = existingCleMetiers.ToList();
        return _dbContext.RefGammesFabrication
            .Where(r => r.ImportBatchId == importBatchId && !list.Contains(r.CleMetier) && r.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, r => false), cancellationToken);
    }

    public Task<int> DisableMissingNomenclaturesAsync(Guid importBatchId, IEnumerable<string> existingCleMetiers, CancellationToken cancellationToken = default)
    {
        var list = existingCleMetiers.ToList();
        return _dbContext.RefNomenclatures
            .Where(r => r.ImportBatchId == importBatchId && !list.Contains(r.CleMetier) && r.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, r => false), cancellationToken);
    }

    public Task<IReadOnlyList<RefGammeFabrication>> QueryGammesAsync(string? codeArticle = null, string? atelier = null, string? numeroOperation = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RefGammesFabrication.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(codeArticle)) query = query.Where(r => r.CodeArticle == codeArticle);
        if (!string.IsNullOrWhiteSpace(atelier)) query = query.Where(r => r.SourceSystem == atelier || r.CodePoste == atelier || r.CodePoste == atelier);
        if (!string.IsNullOrWhiteSpace(numeroOperation)) query = query.Where(r => r.NumeroOperation == numeroOperation);
        return query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<RefGammeFabrication>)t.Result, cancellationToken);
    }

    public Task<IReadOnlyList<RefNomenclature>> QueryNomenclaturesAsync(string? codeArticleParent = null, string? codeComposant = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RefNomenclatures.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(codeArticleParent)) query = query.Where(r => r.CodeArticleParent == codeArticleParent);
        if (!string.IsNullOrWhiteSpace(codeComposant)) query = query.Where(r => r.CodeComposant == codeComposant);
        return query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<RefNomenclature>)t.Result, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
