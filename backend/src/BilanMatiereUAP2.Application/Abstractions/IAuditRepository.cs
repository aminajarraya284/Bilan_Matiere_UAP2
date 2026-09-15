using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record AuditSearchCriteria(
    DateTime? DateDepuisUtc,
    DateTime? DateJusquaExclusiveUtc,
    string? Utilisateur,
    string? Role,
    ActionAudit? Action,
    DomaineAudit? Domaine,
    string? Entite,
    string? ReferenceMetier,
    string? CorrelationId,
    string? Recherche,
    int Page,
    int PageSize);

public sealed record AuditSearchResult(
    IReadOnlyList<TraceAudit> Items,
    int Total,
    DateTime? DerniereActiviteUtc);

public interface IAuditRepository
{
    Task AddAsync(TraceAudit trace, CancellationToken cancellationToken = default);
    Task<TraceAudit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AuditSearchResult> SearchAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
