namespace BilanMatiereUAP2.Api.Authentication;

/// <summary>
/// Configuration de l'authentification Microsoft Entra ID.
///
/// Rien n'est devine : tant que <see cref="Authority"/> ET <see cref="Audience"/> ne sont
/// pas renseignes, l'application refuse de demarrer hors Development plutot que de servir
/// des endpoints sans authentification reelle (RM-SEC-003).
/// </summary>
public sealed class EntraIdAuthenticationOptions
{
    public const string SectionName = "Authentication:Jwt";

    /// <summary>
    /// Autorite OIDC du tenant, par exemple
    /// <c>https://login.microsoftonline.com/{tenantId}/v2.0</c>.
    /// </summary>
    public string? Authority { get; init; }

    /// <summary>Identifiant d'application (client ID) attendu dans la revendication <c>aud</c>.</summary>
    public string? Audience { get; init; }

    /// <summary>
    /// Emetteurs acceptes. Utile lorsque le tenant emet a la fois des jetons v1 et v2.
    /// Vide : l'emetteur est deduit de l'autorite.
    /// </summary>
    public string[] ValidIssuers { get; init; } = [];

    /// <summary>Revendication portant les app roles. Entra ID utilise <c>roles</c>.</summary>
    public string RoleClaimType { get; init; } = "roles";

    /// <summary>Revendication portant le nom affichable de l'utilisateur.</summary>
    public string NameClaimType { get; init; } = "name";

    /// <summary>
    /// Correspondance « app role Entra ID » vers role applicatif
    /// (<c>Admin</c>, <c>ResponsableFonderie</c>, <c>ResponsableUsinage2</c>).
    /// Vide : les libelles Entra ID sont utilises tels quels.
    /// </summary>
    public Dictionary<string, string> CorrespondanceRoles { get; init; } = [];

    public bool EstConfigure =>
        !string.IsNullOrWhiteSpace(Authority) && !string.IsNullOrWhiteSpace(Audience);
}
