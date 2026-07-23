using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class X3ImportRepository(BilanMatiereUAP2DbContext context) : IX3ImportRepository
{
    public async Task<ImportX3Batch?> GetCompletedBatchByPairHashAsync(
        string pairHash,
        CancellationToken cancellationToken)
    {
        return await context.ImportX3Batches
            .AsNoTracking()
            .Include(b => b.Fichiers)
            .Where(b => b.HashCoupleSha256 == pairHash &&
                        (b.Statut == StatutRapprochementX3.Rapproche ||
                         b.Statut == StatutRapprochementX3.ImportPartiel))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ImportX3Batch?> GetBatchByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await context.ImportX3Batches
            .AsNoTracking()
            .Include(b => b.Fichiers)
            .Include(b => b.Anomalies)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ImportX3Anomaly>> GetAnomaliesAsync(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        return await context.ImportX3Anomalies
            .AsNoTracking()
            .Where(a => a.ImportBatchId == batchId)
            .OrderBy(a => a.NumeroLigneSource)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, X3OrdreFabrication>> GetOrdresByNumeroAsync(
        IReadOnlyCollection<string> numerosOF,
        CancellationToken cancellationToken)
    {
        if (numerosOF.Count == 0)
            return new Dictionary<string, X3OrdreFabrication>(StringComparer.OrdinalIgnoreCase);

        var ordres = await context.OrdresFabrication
            .Include(o => o.Operations)
            .Where(o => numerosOF.Contains(o.NumeroOF))
            .ToListAsync(cancellationToken);

        return ordres.ToDictionary(o => o.NumeroOF, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<X3OrdreFabrication?> GetOrdreContexteAsync(
        string numeroOF,
        CancellationToken cancellationToken)
    {
        return await context.OrdresFabrication
            .AsNoTracking()
            .Include(o => o.Operations)
            .Include(o => o.Consommations)
            .FirstOrDefaultAsync(o => o.NumeroOF == numeroOF, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, ConsommationComposant>> GetConsommationsByCleMetierAsync(
        IReadOnlyCollection<string> clesMetier,
        CancellationToken cancellationToken)
    {
        if (clesMetier.Count == 0)
            return new Dictionary<string, ConsommationComposant>(StringComparer.OrdinalIgnoreCase);

        var consommations = await context.ConsommationsComposants
            .Where(c => clesMetier.Contains(c.CleMetier))
            .ToListAsync(cancellationToken);

        return consommations.ToDictionary(c => c.CleMetier, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<PagedResult<ConsommationComposant>> GetConsommationsAsync(
        ConsommationsComposantsFilter filter,
        CancellationToken cancellationToken)
    {
        var query = context.ConsommationsComposants.AsNoTracking().AsQueryable();

        if (filter.DateDebutIncluse is not null)
            query = query.Where(c => c.DateImputation >= filter.DateDebutIncluse.Value);
        if (filter.DateFinExclusive is not null)
            query = query.Where(c => c.DateImputation < filter.DateFinExclusive.Value);
        if (!string.IsNullOrWhiteSpace(filter.NumeroOF))
            query = query.Where(c => c.NumeroOF == filter.NumeroOF);
        if (!string.IsNullOrWhiteSpace(filter.Article))
            query = query.Where(c => c.ArticlePrincipal == filter.Article);
        if (!string.IsNullOrWhiteSpace(filter.CodeComposant))
            query = query.Where(c => c.CodeTypeMatiere == filter.CodeComposant);
        if (filter.Statut is not null)
            query = query.Where(c => c.Statut == filter.Statut.Value);

        var total = await query.CountAsync(cancellationToken);

        query = filter.SortBy switch
        {
            "numeroof" => filter.Descending ? query.OrderByDescending(c => c.NumeroOF) : query.OrderBy(c => c.NumeroOF),
            "article" => filter.Descending ? query.OrderByDescending(c => c.ArticlePrincipal) : query.OrderBy(c => c.ArticlePrincipal),
            "matiere" => filter.Descending ? query.OrderByDescending(c => c.CodeTypeMatiere) : query.OrderBy(c => c.CodeTypeMatiere),
            "sortiemp" => filter.Descending ? query.OrderByDescending(c => c.Consommation) : query.OrderBy(c => c.Consommation),
            "statut" => filter.Descending ? query.OrderByDescending(c => c.Statut) : query.OrderBy(c => c.Statut),
            _ => filter.Descending ? query.OrderByDescending(c => c.DateImputation) : query.OrderBy(c => c.DateImputation)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ConsommationComposant>(items, total);
    }

    public async Task AddBatchAsync(ImportX3Batch batch, CancellationToken cancellationToken)
    {
        await context.ImportX3Batches.AddAsync(batch, cancellationToken);
    }

    public async Task AddOrdresAsync(IEnumerable<X3OrdreFabrication> ordres, CancellationToken cancellationToken)
    {
        await context.OrdresFabrication.AddRangeAsync(ordres, cancellationToken);
    }

    public async Task AddConsommationsAsync(IEnumerable<ConsommationComposant> consommations, CancellationToken cancellationToken)
    {
        await context.ConsommationsComposants.AddRangeAsync(consommations, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
