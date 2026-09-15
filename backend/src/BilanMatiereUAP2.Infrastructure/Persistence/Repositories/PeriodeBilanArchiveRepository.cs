using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class PeriodeBilanArchiveRepository(BilanMatiereUAP2DbContext context)
    : IPeriodeBilanArchiveRepository
{
    public Task<bool> ExistsAsync(string periode, CancellationToken cancellationToken = default)
        => context.PeriodesBilansArchivees.AsNoTracking()
            .AnyAsync(x => x.Periode == periode, cancellationToken);

    public Task AddAsync(PeriodeBilanArchivee archive, CancellationToken cancellationToken = default)
        => context.PeriodesBilansArchivees.AddAsync(archive, cancellationToken).AsTask();

    public Task<PeriodeBilanArchivee?> GetByPeriodeAsync(
        string periode,
        CancellationToken cancellationToken = default)
        => context.PeriodesBilansArchivees.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Periode == periode, cancellationToken);

    public async Task<PagedResult<PeriodeBilanArchivee>> SearchAsync(
        PeriodeBilanArchiveFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.PeriodesBilansArchivees.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Periode)) query = query.Where(x => x.Periode == filter.Periode);
        if (!string.IsNullOrWhiteSpace(filter.Statut)) query = query.Where(x => x.StatutGlobal == filter.Statut);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.Periode)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedResult<PeriodeBilanArchivee>(items, total);
    }
}

