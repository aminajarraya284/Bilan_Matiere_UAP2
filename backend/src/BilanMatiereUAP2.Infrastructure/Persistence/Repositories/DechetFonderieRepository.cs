using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class DechetFonderieRepository(BilanMatiereUAP2DbContext dbContext) : IDechetFonderieRepository
{
    public Task<DechetFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.DechetsFonderie
            .Include(entity => entity.TypeMatiere)
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public async Task<PagedResult<DechetFonderie>> GetPagedAsync(
        DechetsFonderieFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DechetsFonderie
            .AsNoTracking()
            .Include(entity => entity.TypeMatiere)
            .Where(entity =>
                entity.DateDeclaration >= filter.DateDebutIncluse &&
                entity.DateDeclaration < filter.DateFinExclusive);

        if (filter.TypeMatiereId is not null)
        {
            query = query.Where(entity => entity.TypeMatiereId == filter.TypeMatiereId);
        }

        if (filter.TypeDechet is not null)
        {
            query = query.Where(entity => entity.TypeDechet == filter.TypeDechet);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entity => entity.DateDeclaration)
            .ThenByDescending(entity => entity.DateCreationUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DechetFonderie>(items, total);
    }

    public async Task<IReadOnlyList<ResumeDechetsFonderieData>> GetResumeAsync(
        DateOnly dateDebutIncluse,
        DateOnly dateFinExclusive,
        CancellationToken cancellationToken)
    {
        var periodQuery = dbContext.DechetsFonderie
            .AsNoTracking()
            .Where(entity =>
                entity.DateDeclaration >= dateDebutIncluse &&
                entity.DateDeclaration < dateFinExclusive);

        // EF Core 8 SQLite cannot aggregate decimal directly. This branch is used only by
        // the isolated integration-test provider; SQL Server keeps exact decimal aggregation.
        if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            var sqliteData = await periodQuery
                .GroupBy(entity => new { entity.TypeMatiereId, entity.TypeMatiere.Code })
                .Select(group => new
                {
                    group.Key.TypeMatiereId,
                    TypeMatiereCode = group.Key.Code,
                    TotalBavureKg = group.Where(entity => entity.TypeDechet == TypeDechet.Bavure)
                        .Sum(entity => (double)entity.PoidsKg),
                    TotalPoudreKg = group.Where(entity => entity.TypeDechet == TypeDechet.Poudre)
                        .Sum(entity => (double)entity.PoidsKg)
                })
                .OrderBy(item => item.TypeMatiereCode)
                .ToListAsync(cancellationToken);

            return sqliteData.Select(item => new ResumeDechetsFonderieData(
                item.TypeMatiereId,
                item.TypeMatiereCode,
                decimal.Round(Convert.ToDecimal(item.TotalBavureKg), 3),
                decimal.Round(Convert.ToDecimal(item.TotalPoudreKg), 3))).ToArray();
        }

        return await periodQuery
            .GroupBy(entity => new { entity.TypeMatiereId, entity.TypeMatiere.Code })
            .Select(group => new ResumeDechetsFonderieData(
                group.Key.TypeMatiereId,
                group.Key.Code,
                group.Where(entity => entity.TypeDechet == TypeDechet.Bavure)
                    .Sum(entity => (decimal?)entity.PoidsKg) ?? 0,
                group.Where(entity => entity.TypeDechet == TypeDechet.Poudre)
                    .Sum(entity => (decimal?)entity.PoidsKg) ?? 0))
            .OrderBy(item => item.TypeMatiereCode)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DechetFonderie dechet, CancellationToken cancellationToken)
    {
        await dbContext.DechetsFonderie.AddAsync(dechet, cancellationToken);
    }
}
