using BilanMatiereUAP2.Api.Authentication;
using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Administration;
using BilanMatiereUAP2.Contracts.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

/// <summary>
/// Administration des acces. Deux capacites seulement : consulter les profils et
/// activer/desactiver l'acces local.
///
/// Volontairement ABSENTS : creation de profil (automatique au premier login Entra),
/// changement de role (gere dans Entra ID) et gestion de mot de passe (Entra egalement).
/// </summary>
[ApiController]
[Route("api/v1/admin/utilisateurs")]
[Authorize(Policy = AuthorizationPolicies.ManageAdministration)]
public sealed class AdministrationController(IAdministrationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedUtilisateursResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedUtilisateursResponse>> Search(
        [FromQuery] string? recherche,
        [FromQuery] bool? estActif,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => Ok(await service.SearchAsync(
            new GetUtilisateursQuery(recherche, estActif, page, pageSize),
            cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<UtilisateurApplicationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UtilisateurApplicationResponse>> Get(
        int id,
        CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));

    [HttpPut("{id:int}/activation")]
    [ProducesResponseType<UtilisateurApplicationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UtilisateurApplicationResponse>> ChangerActivation(
        int id,
        ToggleUtilisateurApplicationRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.ChangerActivationAsync(id, request.EstActif, cancellationToken));

    /// <summary>
    /// Comptes de connexion locale, en LECTURE SEULE et uniquement en Development.
    /// Aucun mot de passe n'est retourne : le contrat n'a meme pas de champ pour en porter un.
    /// Hors Development, la route repond 404 — elle n'existe pas dans cet environnement.
    /// </summary>
    [HttpGet("developpement")]
    [ProducesResponseType<IReadOnlyList<CompteDeveloppementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<CompteDeveloppementResponse>> GetComptesDeveloppement(
        [FromServices] IWebHostEnvironment environment,
        [FromServices] IConfiguration configuration,
        [FromServices] IDevelopmentAccountAuthenticator? authenticator = null)
    {
        var developmentLoginActif = environment.IsDevelopment()
            && configuration.GetValue<bool>("Authentication:DevelopmentMode");

        if (!developmentLoginActif || authenticator is null)
        {
            return NotFound();
        }

        return Ok(authenticator.Accounts
            .Select(account => new CompteDeveloppementResponse(
                account.Username,
                account.FullName,
                account.Email,
                account.Role,
                authenticator.EstConfigure(account)))
            .ToArray());
    }
}
