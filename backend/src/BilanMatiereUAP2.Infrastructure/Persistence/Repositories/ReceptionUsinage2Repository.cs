using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class ReceptionUsinage2Repository(BilanMatiereUAP2DbContext context) : IReceptionUsinage2Repository
{
    public Task<ReceptionUsinage2?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.ReceptionsUsinage2.FirstOrDefaultAsync(reception => reception.Id == id, cancellationToken);

    public async Task<PagedResult<ReceptionUsinage2>> GetPagedAsync(ReceptionsUsinage2Filter filter, CancellationToken cancellationToken = default)
    {
        var query = context.ReceptionsUsinage2.AsNoTracking().AsQueryable();

        if (filter.PeriodeDebut is not null) query = query.Where(r => r.DateReception >= filter.PeriodeDebut.Value);
        if (filter.PeriodeFin is not null) query = query.Where(r => r.DateReception < filter.PeriodeFin.Value);
        if (!string.IsNullOrWhiteSpace(filter.NumeroOF)) query = query.Where(r => r.NumeroOF == filter.NumeroOF);
        if (!string.IsNullOrWhiteSpace(filter.CodeArticle)) query = query.Where(r => r.CodeArticle == filter.CodeArticle);
        if (!string.IsNullOrWhiteSpace(filter.CodeTypeMatiere)) query = query.Where(r => r.CodeTypeMatiere == filter.CodeTypeMatiere);
        if (!string.IsNullOrWhiteSpace(filter.Atelier)) query = query.Where(r => r.AtelierOrigine == filter.Atelier);
        if (!string.IsNullOrWhiteSpace(filter.Statut) && Enum.TryParse<StatutReceptionUsinage2>(filter.Statut, true, out var statut))
        {
            query = query.Where(r => r.StatutReception == statut);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.DateReception)
            .ThenByDescending(r => r.DateCreationUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReceptionUsinage2>(items, total);
    }

    public async Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByOfAndArticleAsync(string numeroOF, string codeArticle, int annee, int mois, CancellationToken cancellationToken = default)
    {
        var debut = new DateOnly(annee, mois, 1);
        var fin = debut.AddMonths(1);
        return await context.ReceptionsUsinage2.AsNoTracking()
            .Where(r => r.NumeroOF == numeroOF &&
                        r.CodeArticle == codeArticle &&
                        r.StatutReception == StatutReceptionUsinage2.Valide &&
                        r.DateReception >= debut && r.DateReception < fin)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
    {
        var debut = new DateOnly(annee, mois, 1);
        var fin = debut.AddMonths(1);
        return await context.ReceptionsUsinage2.AsNoTracking()
            .Where(r => r.StatutReception == StatutReceptionUsinage2.Valide &&
                        r.DateReception >= debut && r.DateReception < fin)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        => await context.ReceptionsUsinage2.AsNoTracking()
            .Where(r => r.StatutReception == StatutReceptionUsinage2.Valide && r.DateReception == date)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ReceptionUsinage2 reception, CancellationToken cancellationToken = default)
        => await context.ReceptionsUsinage2.AddAsync(reception, cancellationToken);
}
