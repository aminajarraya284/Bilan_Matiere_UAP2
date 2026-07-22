using System.Security.Claims;
using BilanMatiereUAP2.Application.Abstractions;

namespace BilanMatiereUAP2.Api.Services;

public sealed class HttpCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string? UserName => Principal?.Identity?.Name;
    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
