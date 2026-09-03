using System.Text.Json;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.Audit;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Audit;

public sealed class AuditService(
    IAuditRepository repository,
    IAuditDataSanitizer sanitizer,
    ICurrentUserContext currentUser,
    ICorrelationIdAccessor correlationIdAccessor,
    IDateTimeProvider dateTimeProvider) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task EnregistrerAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var system = !currentUser.IsAuthenticated;
        var roles = currentUser.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var trace = TraceAudit.Creer(
            dateTimeProvider.UtcNow,
            entry.Action,
            entry.Domaine,
            entry.Entite,
            entry.EntiteId,
            entry.ReferenceMetier,
            system ? "SYSTEM" : currentUser.UserId,
            system ? "Système" : currentUser.DisplayName,
            system ? null : currentUser.Email,
            system ? "System" : roles.Length > 0 ? string.Join(",", roles) : "NonRenseigne",
            entry.Source,
            correlationIdAccessor.CorrelationId,
            entry.Resume,
            Serialize(entry.AnciennesValeurs),
            Serialize(entry.NouvellesValeurs),
            Serialize(entry.Informations));

        await repository.AddAsync(trace, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private string? Serialize(IReadOnlyDictionary<string, object?>? values)
        => values is null ? null : JsonSerializer.Serialize(sanitizer.Sanitize(values), JsonOptions);
}

public sealed class AuditQueryService(IAuditRepository repository) : IAuditQueryService
{
    public async Task<PagedAuditResponse> SearchAsync(
        GetAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        Validate(query);
        var result = await repository.SearchAsync(
            new AuditSearchCriteria(
                query.DateDepuis?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                query.DateJusqua?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Optional(query.Utilisateur),
                Optional(query.Role),
                ParseEnum<ActionAudit>(query.Action, "action"),
                ParseEnum<DomaineAudit>(query.Domaine, "domaine"),
                Optional(query.Entite),
                Optional(query.ReferenceMetier),
                Optional(query.CorrelationId),
                Optional(query.Recherche),
                query.Page,
                query.PageSize),
            cancellationToken);

        return new PagedAuditResponse(
            result.Items.Select(ToListItem).ToArray(),
            result.Total,
            query.Page,
            query.PageSize,
            result.DerniereActiviteUtc);
    }

    public async Task<AuditDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var trace = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Trace d'audit introuvable.");
        return new AuditDetailResponse(
            trace.Id, trace.DateUtc, trace.Action.ToString(), trace.Domaine.ToString(),
            trace.Entite, trace.EntiteId, trace.ReferenceMetier, trace.UtilisateurId,
            trace.UtilisateurNom, trace.UtilisateurEmail, trace.Role, trace.Source,
            trace.CorrelationId, trace.Resume, Json(trace.AnciennesValeursJson),
            Json(trace.NouvellesValeursJson), Json(trace.InformationsJson));
    }

    private static AuditListItemResponse ToListItem(TraceAudit trace)
        => new(
            trace.Id, trace.DateUtc, trace.Action.ToString(), trace.Domaine.ToString(),
            trace.Entite, trace.EntiteId, trace.ReferenceMetier, trace.UtilisateurNom,
            trace.Role, trace.Source, trace.CorrelationId, trace.Resume);

    private static JsonElement? Json(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        using var document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private static void Validate(GetAuditQuery query)
    {
        var errors = new List<ValidationFailure>();
        if (query.Page < 1) errors.Add(new("page", "La page doit etre superieure ou egale a 1."));
        if (query.PageSize is < 1 or > 200) errors.Add(new("pageSize", "La taille de page doit etre comprise entre 1 et 200."));
        if (query.DateDepuis is not null && query.DateJusqua is not null && query.DateDepuis > query.DateJusqua)
            errors.Add(new("dateJusqua", "La date de fin doit etre posterieure ou egale a la date de debut."));
        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static TEnum? ParseEnum<TEnum>(string? value, string propertyName) where TEnum : struct, Enum
    {
        var normalized = Optional(value);
        if (normalized is null) return null;
        if (Enum.TryParse<TEnum>(normalized, true, out var result) && Enum.IsDefined(result)) return result;
        throw new ValidationException([new ValidationFailure(propertyName, $"La valeur '{normalized}' est invalide.")]);
    }

    private static string? Optional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
