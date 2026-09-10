using System.Security.Claims;
using BilanMatiereUAP2.Application.Abstractions;

namespace BilanMatiereUAP2.Api.Services;

public sealed class HttpCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(
        Principal?.FindFirstValue("oid")
        ?? Principal?.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub"), out var id) ? id : null;
    public string? UserName => Principal?.Identity?.Name;
    // Les roles personnalises peuvent preceder leur correspondance applicative.
    public string? Role => new[] { "Admin", "ResponsableFonderie", "ResponsableUsinage2" }
        .FirstOrDefault(role => Principal?.IsInRole(role) == true)
        ?? Principal?.Identities.SelectMany(identity => identity.FindAll(identity.RoleClaimType))
            .Select(claim => claim.Value).FirstOrDefault();
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
