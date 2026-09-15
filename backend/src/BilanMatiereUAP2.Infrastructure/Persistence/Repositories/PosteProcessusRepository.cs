using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class PosteProcessusRepository(
    BilanMatiereUAP2DbContext context) : IPosteProcessusRepository
{
    public async Task<PagedResult<RefPosteProcessus>> GetPagedAsync(
        PostesProcessusFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<RefPosteProcessus>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Poste))
        {
            query = query.Where(entity => entity.CodePoste.Contains(filter.Poste));
        }
        if (filter.Origine is not null)
        {
            query = query.Where(entity => entity.OrigineProcessus == filter.Origine);
        }
        if (filter.Actif is not null)
        {
            query = query.Where(entity => entity.IsActive == filter.Actif);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(entity => entity.CodePoste)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<RefPosteProcessus>(items, total);
    }

    public async Task<IReadOnlyList<RefPosteProcessus>> GetAllAsync(
        bool? actif,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<RefPosteProcessus>().AsNoTracking().AsQueryable();
        if (actif is not null)
        {
            query = query.Where(entity => entity.IsActive == actif);
        }
        return await query.OrderBy(entity => entity.CodePoste).ToListAsync(cancellationToken);
    }

    public Task<RefPosteProcessus?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => context.Set<RefPosteProcessus>()
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<RefPosteProcessus?> GetByCodeAsync(
        string codePoste,
        CancellationToken cancellationToken = default)
        => context.Set<RefPosteProcessus>()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.CodePoste == codePoste, cancellationToken);

    public async Task<IReadOnlyList<RefPosteProcessusHistorique>> GetHistoryAsync(
        Guid posteProcessusId,
        CancellationToken cancellationToken = default)
        => await context.Set<RefPosteProcessusHistorique>()
            .AsNoTracking()
            .Where(history => history.PosteProcessusId == posteProcessusId)
            .OrderByDescending(history => history.DateUtc)
            .ToListAsync(cancellationToken);

    public Task AddAsync(RefPosteProcessus entity, CancellationToken cancellationToken = default)
        => context.Set<RefPosteProcessus>().AddAsync(entity, cancellationToken).AsTask();

    public Task AddHistoryAsync(
        RefPosteProcessusHistorique history,
        CancellationToken cancellationToken = default)
        => context.Set<RefPosteProcessusHistorique>().AddAsync(history, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
