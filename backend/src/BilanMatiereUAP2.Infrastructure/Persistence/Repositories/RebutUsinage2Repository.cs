using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class RebutUsinage2Repository(BilanMatiereUAP2DbContext dbContext) : IRebutUsinage2Repository
{
    public async Task AddAsync(RebutUsinage2 rebut, CancellationToken cancellationToken = default)
        => await dbContext.RebutsUsinage2.AddAsync(rebut, cancellationToken);

    public Task<RebutUsinage2?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.RebutsUsinage2.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<RebutUsinage2> Items, int Total)> QueryAsync(
        int? annee,
        int? mois,
        string? numeroOF,
        string? codeArticle,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.RebutsUsinage2.AsNoTracking().AsQueryable();

        if (annee is not null) query = query.Where(entity => entity.Annee == annee);
        if (mois is not null) query = query.Where(entity => entity.Mois == mois);

        if (!string.IsNullOrWhiteSpace(numeroOF))
        {
            var normalized = numeroOF.Trim().ToUpperInvariant();
            query = query.Where(entity => entity.NumeroOF == normalized);
        }

        if (!string.IsNullOrWhiteSpace(codeArticle))
        {
            var normalized = codeArticle.Trim().ToUpperInvariant();
            query = query.Where(entity => entity.CodeArticle == normalized);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entity => entity.DateDeclaration)
            .ThenByDescending(entity => entity.CreeLeUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<RebutUsinage2>> GetValidesByPeriodeAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default)
        => await dbContext.RebutsUsinage2
            .AsNoTracking()
            .Where(entity =>
                entity.Annee == annee &&
                entity.Mois == mois &&
                entity.Statut == StatutRebutUsinage2.Valide)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
