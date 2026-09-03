using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository(BilanMatiereUAP2DbContext context) : IAuditRepository
{
    public async Task AddAsync(TraceAudit trace, CancellationToken cancellationToken = default)
        => await context.AuditTraces.AddAsync(trace, cancellationToken);

    public Task<TraceAudit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.AuditTraces.AsNoTracking().FirstOrDefaultAsync(trace => trace.Id == id, cancellationToken);

    public async Task<AuditSearchResult> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = context.AuditTraces.AsNoTracking().AsQueryable();
        if (criteria.DateDepuisUtc is not null) query = query.Where(trace => trace.DateUtc >= criteria.DateDepuisUtc.Value);
        if (criteria.DateJusquaExclusiveUtc is not null) query = query.Where(trace => trace.DateUtc < criteria.DateJusquaExclusiveUtc.Value);
        if (!string.IsNullOrWhiteSpace(criteria.Utilisateur))
        {
            var value = criteria.Utilisateur;
            query = query.Where(trace => trace.UtilisateurId.Contains(value) || trace.UtilisateurNom.Contains(value) ||
                (trace.UtilisateurEmail != null && trace.UtilisateurEmail.Contains(value)));
        }
        if (!string.IsNullOrWhiteSpace(criteria.Role)) query = query.Where(trace => trace.Role.Contains(criteria.Role));
        if (criteria.Action is not null) query = query.Where(trace => trace.Action == criteria.Action.Value);
        if (criteria.Domaine is not null) query = query.Where(trace => trace.Domaine == criteria.Domaine.Value);
        if (!string.IsNullOrWhiteSpace(criteria.Entite)) query = query.Where(trace => trace.Entite == criteria.Entite);
        if (!string.IsNullOrWhiteSpace(criteria.ReferenceMetier)) query = query.Where(trace => trace.ReferenceMetier != null && trace.ReferenceMetier.Contains(criteria.ReferenceMetier));
        if (!string.IsNullOrWhiteSpace(criteria.CorrelationId)) query = query.Where(trace => trace.CorrelationId == criteria.CorrelationId);
        if (!string.IsNullOrWhiteSpace(criteria.Recherche))
        {
            var value = criteria.Recherche;
            query = query.Where(trace =>
                trace.UtilisateurNom.Contains(value) || trace.Entite.Contains(value) || trace.EntiteId.Contains(value) ||
                trace.Source.Contains(value) || (trace.ReferenceMetier != null && trace.ReferenceMetier.Contains(value)) ||
                (trace.Resume != null && trace.Resume.Contains(value)));
        }

        var total = await query.CountAsync(cancellationToken);
        var last = await query.Select(trace => (DateTime?)trace.DateUtc).MaxAsync(cancellationToken);
        var items = await query
            .OrderByDescending(trace => trace.DateUtc)
            .ThenByDescending(trace => trace.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);
        return new AuditSearchResult(items, total, last);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
