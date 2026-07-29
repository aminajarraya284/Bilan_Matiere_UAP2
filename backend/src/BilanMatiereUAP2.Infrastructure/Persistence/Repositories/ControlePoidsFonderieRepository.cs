using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class ControlePoidsFonderieRepository(BilanMatiereUAP2DbContext dbContext) : IControlePoidsFonderieRepository
{
    public Task<ControlePoidsFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.ControlesPoidsFonderie
            .Include(entity => entity.TypeMatiere)
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public async Task<PagedResult<ControlePoidsFonderie>> GetPagedAsync(
        ControlesPoidsFonderieFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ControlesPoidsFonderie
            .AsNoTracking()
            .Include(entity => entity.TypeMatiere)
            .AsQueryable();

        if (filter.DateControle is not null)
        {
            query = query.Where(entity => entity.DateControle == filter.DateControle);
        }

        if (!string.IsNullOrWhiteSpace(filter.NumeroOF))
        {
            var numeroOF = filter.NumeroOF.Trim().ToUpperInvariant();
            query = query.Where(entity => entity.NumeroOF == numeroOF);
        }

        if (!string.IsNullOrWhiteSpace(filter.CodeArticle))
        {
            var codeArticle = filter.CodeArticle.Trim().ToUpperInvariant();
            query = query.Where(entity => entity.CodeArticle == codeArticle);
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
            .OrderByDescending(entity => entity.DateControle)
            .ThenByDescending(entity => entity.CreeLeUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ControlePoidsFonderie>(items, total);
    }

    public async Task<IReadOnlyList<ControlePoidsFonderie>> FindCandidatesAsync(
        string codeArticle,
        Guid typeMatiereId,
        string? numeroOF,
        DateOnly dateReception,
        CancellationToken cancellationToken)
    {
        var normalizedArticle = codeArticle.Trim().ToUpperInvariant();
        var normalizedOF = string.IsNullOrWhiteSpace(numeroOF) ? null : numeroOF.Trim().ToUpperInvariant();

        return await dbContext.ControlesPoidsFonderie
            .AsNoTracking()
            .Include(entity => entity.TypeMatiere)
            .Where(entity =>
                entity.CodeArticle == normalizedArticle &&
                entity.TypeMatiereId == typeMatiereId &&
                entity.DateControle <= dateReception &&
                (normalizedOF == null || entity.NumeroOF == normalizedOF) &&
                entity.Statut != StatutControlePoidsFonderie.Annule)
            .OrderByDescending(entity => entity.DateControle)
            .ThenByDescending(entity => entity.CreeLeUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ControlePoidsFonderie controle, CancellationToken cancellationToken)
    {
        await dbContext.ControlesPoidsFonderie.AddAsync(controle, cancellationToken);
    }
}
