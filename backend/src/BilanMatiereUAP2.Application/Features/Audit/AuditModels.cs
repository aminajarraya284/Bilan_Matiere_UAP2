using BilanMatiereUAP2.Contracts.Audit;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Features.Audit;

public sealed record AuditEntry(
    ActionAudit Action,
    DomaineAudit Domaine,
    string Entite,
    string EntiteId,
    string? ReferenceMetier,
    string Source,
    string? Resume,
    IReadOnlyDictionary<string, object?>? AnciennesValeurs = null,
    IReadOnlyDictionary<string, object?>? NouvellesValeurs = null,
    IReadOnlyDictionary<string, object?>? Informations = null);

public sealed record GetAuditQuery(
    DateOnly? DateDepuis,
    DateOnly? DateJusqua,
    string? Utilisateur,
    string? Role,
    string? Action,
    string? Domaine,
    string? Entite,
    string? ReferenceMetier,
    string? CorrelationId,
    string? Recherche,
    int Page = 1,
    int PageSize = 50);

public interface IAuditDataSanitizer
{
    IReadOnlyDictionary<string, object?> Sanitize(IReadOnlyDictionary<string, object?> values);
}

public interface IAuditService
{
    Task EnregistrerAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}

public interface IAuditQueryService
{
    Task<PagedAuditResponse> SearchAsync(GetAuditQuery query, CancellationToken cancellationToken = default);
    Task<AuditDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
