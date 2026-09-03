using System.Security.Claims;
using BilanMatiereUAP2.Application.Features.Administration;

namespace BilanMatiereUAP2.Api.Middleware;

/// <summary>
/// Rapproche chaque requete authentifiee par Entra ID avec son profil applicatif local :
/// creation au premier passage, rafraichissement ensuite, et refus si le profil a ete
/// desactive ou si le jeton ne porte aucun role SOPAL reconnu.
///
/// Place APRES UseAuthentication et AVANT UseAuthorization : le principal doit exister,
/// et le refus doit intervenir avant que la moindre policy metier ne s'applique.
///
/// N'agit jamais sur le schema de developpement : les comptes admin/fonderie/usinage2
/// n'ont pas de profil SQL et ne doivent pas en avoir (voir la mission, PARTIE F).
/// </summary>
public sealed class EntraUserProvisioningMiddleware(RequestDelegate next, ILogger<EntraUserProvisioningMiddleware> logger)
{
    /// <summary>Revendications portant l'identifiant d'objet Entra, par ordre de preference.</summary>
    private static readonly string[] ObjectIdClaims =
    [
        "oid",
        "http://schemas.microsoft.com/identity/claims/objectidentifier"
    ];

    private static readonly string[] EmailClaims =
    [
        "email",
        "preferred_username",
        ClaimTypes.Email,
        ClaimTypes.Upn
    ];

    private static readonly string[] NomClaims =
    [
        "name",
        ClaimTypes.Name
    ];

    public async Task InvokeAsync(HttpContext context, IEntraUserProvisioningService provisioning)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var objectId = PremiereValeur(user, ObjectIdClaims);
        if (string.IsNullOrWhiteSpace(objectId))
        {
            // Pas d'oid : ce n'est pas un jeton Entra (schema Development, tests).
            // Le provisionnement ne s'applique pas.
            await next(context);
            return;
        }

        var identite = new EntraIdentite(
            objectId,
            PremiereValeur(user, EmailClaims),
            PremiereValeur(user, NomClaims),
            user.FindAll(ClaimTypes.Role).Concat(user.FindAll("roles"))
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray());

        var resultat = await provisioning.ProvisionnerAsync(identite, context.RequestAborted);

        switch (resultat)
        {
            case ResultatProvisionnement.Desactive:
                // L'identifiant Entra n'est pas un secret, mais on ne journalise ni le
                // jeton ni les revendications.
                logger.LogWarning(
                    "Acces refuse : le profil applicatif de l'utilisateur Entra {ObjectId} est desactive.",
                    objectId);
                await EcrireRefusAsync(
                    context,
                    "Accès désactivé",
                    "Votre accès à l'application a été désactivé. Contactez un administrateur SOPAL.");
                return;

            case ResultatProvisionnement.AucunRoleReconnu:
                logger.LogWarning(
                    "Acces refuse : aucun role SOPAL reconnu pour l'utilisateur Entra {ObjectId}.",
                    objectId);
                await EcrireRefusAsync(
                    context,
                    "Aucun rôle applicatif",
                    "Aucun rôle SOPAL n'est associé à votre compte. Contactez l'équipe informatique.");
                return;

            case ResultatProvisionnement.Cree:
                logger.LogInformation(
                    "Profil applicatif cree automatiquement pour l'utilisateur Entra {ObjectId}.",
                    objectId);
                break;
        }

        await next(context);
    }

    private static string? PremiereValeur(ClaimsPrincipal user, IEnumerable<string> types)
        => types
            .Select(user.FindFirstValue)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static Task EcrireRefusAsync(HttpContext context, string titre, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new
        {
            title = titre,
            status = StatusCodes.Status403Forbidden,
            detail,
            instance = context.Request.Path.Value
        });
    }
}
