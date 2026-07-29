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
    IConfiguration configuration,
    IDevelopmentTokenService tokenService) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";
    private const string BearerPrefix = "Bearer ";
    private static readonly string[] AllowedRoles = ["Admin", "ResponsableFonderie", "ResponsableUsinage2"];
    private static readonly Guid DefaultUserId = Guid.Parse("de000000-0000-0000-0000-000000000001");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("Authentication:DevelopmentMode"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // 1. Token emis par POST /api/auth/login : source d'identite prioritaire.
        //    C'est le seul mode qui reflete reellement le compte choisi a la connexion,
        //    et donc le seul qui permet a une policy de refuser un role insuffisant (403).
        var authorization = Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            if (!authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Schema d'autorisation non supporte."));
            }

            var payload = tokenService.Validate(authorization[BearerPrefix.Length..].Trim());
            if (payload is null)
            {
                // Token absent de la signature attendue ou expire : 401, afin que le
                // frontend purge la session et redirige vers /login.
                return Task.FromResult(AuthenticateResult.Fail("Token de developpement invalide ou expire."));
            }

            return Task.FromResult(Success(payload.UserId, payload.FullName, payload.Role));
        }

        // 2. Repli historique par en-tetes, conserve pour Swagger, curl et les tests
        //    d'integration existants qui n'ont pas besoin de passer par la connexion.
        var requestedRole = Request.Headers["X-Development-Role"].FirstOrDefault();
        var role = AllowedRoles.Contains(requestedRole, StringComparer.Ordinal)
            ? requestedRole!
            : "Admin";
        var userName = Request.Headers["X-Development-User"].FirstOrDefault() ?? "Utilisateur développement";
        var userId = Request.Headers["X-Development-UserId"].FirstOrDefault();
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            parsedUserId = DefaultUserId;
        }

        return Task.FromResult(Success(parsedUserId, userName, role));
    }

    private AuthenticateResult Success(Guid userId, string userName, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
