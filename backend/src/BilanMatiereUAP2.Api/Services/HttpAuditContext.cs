using System.Security.Claims;
using BilanMatiereUAP2.Api.Middleware;
using BilanMatiereUAP2.Application.Abstractions;

namespace BilanMatiereUAP2.Api.Services;

public sealed class HttpAuditContext(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext, ICorrelationIdAccessor
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string UserId => FirstClaim(
        ClaimTypes.NameIdentifier,
        "oid",
        "http://schemas.microsoft.com/identity/claims/objectidentifier",
        "sub") ?? "SYSTEM";

    public string DisplayName => Principal?.Identity?.Name
        ?? FirstClaim("name", ClaimTypes.GivenName)
        ?? (IsAuthenticated ? "Utilisateur authentifie" : "Système");

    public string? Email => FirstClaim(
        ClaimTypes.Email,
        "preferred_username",
        "upn");

    public IReadOnlyCollection<string> Roles => Principal?.Claims
        .Where(claim => claim.Type is ClaimTypes.Role or "roles")
        .Select(claim => claim.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    public string CorrelationId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context?.Items[CorrelationIdMiddleware.ItemKey] is string value && !string.IsNullOrWhiteSpace(value))
                return value;
            return context?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        }
    }

    private string? FirstClaim(params string[] types)
        => types.Select(type => Principal?.FindFirstValue(type)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
