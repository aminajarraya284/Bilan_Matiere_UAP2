using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BilanMatiereUAP2.Api.Authentication;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IWebHostEnvironment environment,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";
    private static readonly string[] AllowedRoles = ["Admin", "ResponsableFonderie", "ResponsableUsinage2"];

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("Authentication:DevelopmentMode"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var requestedRole = Request.Headers["X-Development-Role"].FirstOrDefault();
        var role = AllowedRoles.Contains(requestedRole, StringComparer.Ordinal)
            ? requestedRole!
            : "Admin";
        var userName = Request.Headers["X-Development-User"].FirstOrDefault() ?? "Utilisateur développement";
        var userId = Request.Headers["X-Development-UserId"].FirstOrDefault();
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            parsedUserId = Guid.Parse("de000000-0000-0000-0000-000000000001");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, parsedUserId.ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
