using BilanMatiereUAP2.Api.Authentication;
using BilanMatiereUAP2.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

/// <summary>
/// Authentification locale reservee a l'environnement Development.
///
/// Ce controleur debloque l'usage de l'application en Development (VITE_USE_REAL_API=true)
/// en fournissant au frontend un token exploitable. Il ne constitue en aucun cas
/// l'authentification de production : celle-ci passera par Entra ID
/// (voir TODO_METIER Entra ID dans Program.cs) et fera l'objet d'un lot separe.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IDevelopmentTokenService tokenService,
    IDevelopmentAccountAuthenticator accountAuthenticator,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase
{
    private bool IsDevelopmentLoginEnabled =>
        environment.IsDevelopment() && configuration.GetValue<bool>("Authentication:DevelopmentMode");

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<LoginResponse> Login(DevelopmentLoginRequest request)
    {
        // Hors Development, l'endpoint n'existe pas : on ne divulgue meme pas sa presence.
        if (!IsDevelopmentLoginEnabled)
        {
            logger.LogWarning(
                "Tentative d'appel a /api/auth/login dans l'environnement '{Environment}' ou la connexion locale est desactivee.",
                environment.EnvironmentName);
            return NotFound();
        }

        // Le mot de passe attendu vient des User Secrets : si la cle n'est pas configuree,
        // le compte est indisponible, sans repli sur une valeur faible.
        var account = accountAuthenticator.Authenticate(request.Username, request.Password);
        if (account is null)
        {
            // Message unique pour utilisateur inconnu et mot de passe invalide :
            // ne pas permettre d'enumerer les comptes existants.
            logger.LogInformation("Echec de connexion Development pour l'identifiant '{Username}'.", request.Username);
            return Problem(
                title: "Identifiants incorrects",
                detail: "Identifiant ou mot de passe incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAtUtc) = tokenService.Issue(account);

        // Le mot de passe n'est jamais recopie dans la reponse.
        return Ok(new LoginResponse(
            token,
            expiresAtUtc,
            new AuthenticatedUserResponse(
                account.Id,
                account.FullName,
                account.Username,
                account.Email,
                account.Role,
                DevelopmentAccounts.ActiveStatus)));
    }
}
