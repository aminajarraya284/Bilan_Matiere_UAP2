using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class InventaireFonderieRepository(BilanMatiereUAP2DbContext dbContext)
    : IInventaireFonderieRepository
{
    public Task<InventaireFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.InventairesFonderie
            .Include(entity => entity.TypeMatiere)
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<PagedResult<InventaireFonderie>> GetPagedAsync(
        InventairesFonderieFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.InventairesFonderie
            .AsNoTracking()
            .Include(entity => entity.TypeMatiere)
            .AsQueryable();

        if (filter.Periode is not null)
        {
            query = query.Where(entity => entity.Periode == filter.Periode);
        }

        if (filter.TypeMatiereId is not null)
        {
            query = query.Where(entity => entity.TypeMatiereId == filter.TypeMatiereId);
        }

        if (filter.Statut is not null)
        {
            query = query.Where(entity => entity.Statut == filter.Statut);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entity => entity.Periode)
            .ThenBy(entity => entity.TypeMatiere.Code)
            .ThenByDescending(entity => entity.CreeLeUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<InventaireFonderie>(items, total);
    }

    public Task<bool> ExistsActiveAsync(
        DateOnly periode,
        Guid typeMatiereId,
        Guid? excludeId,
        CancellationToken cancellationToken)
        => dbContext.InventairesFonderie.AsNoTracking().AnyAsync(entity =>
            entity.Periode == periode &&
            entity.TypeMatiereId == typeMatiereId &&
            entity.Statut != StatutInventaireFonderie.Annule &&
            (!excludeId.HasValue || entity.Id != excludeId.Value),
            cancellationToken);

    public async Task AddAsync(InventaireFonderie inventaire, CancellationToken cancellationToken)
        => await dbContext.InventairesFonderie.AddAsync(inventaire, cancellationToken);
}
