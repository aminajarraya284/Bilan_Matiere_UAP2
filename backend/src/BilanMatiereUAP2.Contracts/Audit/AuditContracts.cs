using System.Text.Json;

namespace BilanMatiereUAP2.Contracts.Audit;

public sealed record AuditListItemResponse(
    Guid Id,
    DateTime DateUtc,
    string Action,
    string Domaine,
    string Entite,
    string EntiteId,
    string? ReferenceMetier,
    string UtilisateurNom,
    string Role,
    string Source,
    string CorrelationId,
    string? Resume);

public sealed record AuditDetailResponse(
    Guid Id,
    DateTime DateUtc,
    string Action,
    string Domaine,
    string Entite,
    string EntiteId,
    string? ReferenceMetier,
    string UtilisateurId,
    string UtilisateurNom,
    string? UtilisateurEmail,
    string Role,
    string Source,
    string CorrelationId,
    string? Resume,
    JsonElement? AnciennesValeurs,
    JsonElement? NouvellesValeurs,
    JsonElement? Informations);

public sealed record PagedAuditResponse(
    IReadOnlyList<AuditListItemResponse> Items,
    int Total,
    int Page,
    int PageSize,
    DateTime? DerniereActiviteUtc);
