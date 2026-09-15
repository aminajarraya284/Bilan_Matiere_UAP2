using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace BilanMatiereUAP2.Api.Authentication;

/// <summary>
/// Traduit les app roles Entra ID en roles applicatifs, d'apres la correspondance
/// configuree (<c>Authentication:Jwt:CorrespondanceRoles</c>).
///
/// Les roles d'origine sont CONSERVES : la transformation ajoute, elle ne remplace pas.
/// Retirer un role sur la foi d'une configuration incomplete reviendrait a refuser
/// silencieusement des acces legitimes.
/// </summary>
public sealed class EntraIdRoleMappingTransformation(
    IReadOnlyDictionary<string, string> correspondance,
    string roleClaimType) : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var rolesEntra = identity.FindAll(roleClaimType).Select(claim => claim.Value).ToArray();
        var dejaPresents = identity
            .FindAll(identity.RoleClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var roleEntra in rolesEntra)
        {
            if (!correspondance.TryGetValue(roleEntra, out var roleApplicatif)) continue;
            if (!dejaPresents.Add(roleApplicatif)) continue;
            identity.AddClaim(new Claim(identity.RoleClaimType, roleApplicatif));
        }

        return Task.FromResult(principal);
    }
}
