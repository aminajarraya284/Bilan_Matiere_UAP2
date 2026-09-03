using System.Security.Cryptography;
using System.Text;

namespace BilanMatiereUAP2.Api.Authentication;

/// <summary>
/// Compte de demonstration utilisable UNIQUEMENT en environnement Development.
///
/// Ce type ne porte AUCUN mot de passe : l'identite d'un compte de developpement n'est pas
/// un secret, son mot de passe si. Le mot de passe est lu a la demande depuis la
/// configuration (.NET User Secrets), via <see cref="PasswordConfigurationKey"/>.
///
/// <paramref name="Id"/> alimente le contrat AuthUser du frontend (entier),
/// <paramref name="UserId"/> alimente ICurrentUserService.UserId (Guid, colonnes d'audit).
/// </summary>
public sealed record DevelopmentAccount(
    int Id,
    Guid UserId,
    string Username,
    string FullName,
    string Email,
    string Role)
{
    /// <summary>
    /// Cle de configuration du mot de passe, par exemple
    /// <c>DevelopmentAccounts:admin:Password</c>. La valeur n'est jamais versionnee :
    /// elle est fournie par <c>dotnet user-secrets</c> sur le poste du developpeur.
    /// </summary>
    public string PasswordConfigurationKey => $"{DevelopmentAccounts.ConfigurationSection}:{Username}:Password";
}

/// <summary>
/// Catalogue des identites de developpement. Les mots de passe ne figurent pas ici et ne
/// figurent nulle part dans le depot : ils sont resolus par
/// <see cref="IDevelopmentAccountAuthenticator"/> depuis la configuration.
///
/// Ces comptes ne sont de toute facon jamais joignables hors Development :
///   - AuthController refuse toute requete si l'environnement n'est pas Development
///     ou si Authentication:DevelopmentMode n'est pas a true ;
///   - Program.cs echoue au demarrage hors Development tant que l'authentification
///     Entra ID n'est pas configuree (RM-SEC-003).
/// </summary>
public static class DevelopmentAccounts
{
    public const string ConfigurationSection = "DevelopmentAccounts";
    public const string ActiveStatus = "Actif";

    public static readonly IReadOnlyList<DevelopmentAccount> All =
    [
        new(
            Id: 1,
            UserId: Guid.Parse("de000000-0000-0000-0000-000000000001"),
            Username: "admin",
            FullName: "Administrateur SOPAL",
            Email: "admin@sopal.com",
            Role: "Admin"),
        new(
            Id: 2,
            UserId: Guid.Parse("de000000-0000-0000-0000-000000000002"),
            Username: "fonderie",
            FullName: "Responsable Fonderie",
            Email: "fonderie@sopal.com",
            Role: "ResponsableFonderie"),
        new(
            Id: 3,
            UserId: Guid.Parse("de000000-0000-0000-0000-000000000003"),
            Username: "usinage2",
            FullName: "Responsable Usinage 2",
            Email: "usinage2@sopal.com",
            Role: "ResponsableUsinage2")
    ];

    public static DevelopmentAccount? FindByUsername(string? username) =>
        string.IsNullOrWhiteSpace(username)
            ? null
            : All.FirstOrDefault(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));
}

public interface IDevelopmentAccountAuthenticator
{
    /// <summary>Identites de developpement, sans aucun secret.</summary>
    IReadOnlyList<DevelopmentAccount> Accounts { get; }

    /// <summary>
    /// Retourne le compte si l'identifiant existe, que son mot de passe est configure
    /// et qu'il correspond. Retourne <c>null</c> dans tous les autres cas.
    /// </summary>
    DevelopmentAccount? Authenticate(string? username, string? password);

    /// <summary>
    /// Indique si le mot de passe du compte est renseigne en configuration. Sert au
    /// diagnostic d'ecran d'administration : un compte non configure est inutilisable.
    /// </summary>
    bool EstConfigure(DevelopmentAccount account);
}

/// <summary>
/// Resout les mots de passe de developpement depuis la configuration (User Secrets).
///
/// Aucun repli : si la cle n'est pas configuree, le compte est simplement indisponible.
/// Un mot de passe par defaut, meme reserve a Development, finit toujours par etre le
/// mot de passe reel de quelqu'un.
/// </summary>
public sealed class ConfigurationDevelopmentAccountAuthenticator(IConfiguration configuration)
    : IDevelopmentAccountAuthenticator
{
    public IReadOnlyList<DevelopmentAccount> Accounts => DevelopmentAccounts.All;

    public bool EstConfigure(DevelopmentAccount account)
        => !string.IsNullOrEmpty(configuration[account.PasswordConfigurationKey]);

    public DevelopmentAccount? Authenticate(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var account = DevelopmentAccounts.FindByUsername(username);
        if (account is null)
        {
            return null;
        }

        var expected = configuration[account.PasswordConfigurationKey];
        if (string.IsNullOrEmpty(expected))
        {
            // Compte connu mais non configure : indisponible, sans repli sur une valeur faible.
            return null;
        }

        return FixedTimeEquals(expected, password) ? account : null;
    }

    /// <summary>
    /// Comparaison a duree constante : une comparaison ordinaire s'arrete au premier
    /// caractere different et laisse mesurer la longueur du prefixe correct.
    /// </summary>
    private static bool FixedTimeEquals(string expected, string provided)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
}
