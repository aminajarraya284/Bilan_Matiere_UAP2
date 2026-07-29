namespace BilanMatiereUAP2.Api.Authentication;

/// <summary>
/// Compte de demonstration utilisable UNIQUEMENT en environnement Development.
/// <paramref name="Id"/> alimente le contrat AuthUser du frontend (entier),
/// <paramref name="UserId"/> alimente ICurrentUserService.UserId (Guid, colonnes d'audit).
/// </summary>
public sealed record DevelopmentAccount(
    int Id,
    Guid UserId,
    string Username,
    string Password,
    string FullName,
    string Email,
    string Role);

/// <summary>
/// Referentiel des comptes de demonstration Development.
///
/// ATTENTION : ces comptes et leurs mots de passe sont volontairement en clair car ils
/// ne sont JAMAIS accessibles hors Development :
///   - AuthController refuse toute requete si l'environnement n'est pas Development
///     ou si Authentication:DevelopmentMode n'est pas a true ;
///   - Program.cs echoue au demarrage hors Development tant que l'authentification
///     Entra ID n'est pas branchee (voir TODO_METIER Entra ID).
///
/// Ils repliquent a l'identique les profils deja documentes sur l'ecran de connexion
/// du frontend (admin / fonderie / usinage2, mot de passe identique a l'identifiant).
/// Aucun secret de production ne doit etre ajoute ici.
/// </summary>
public static class DevelopmentAccounts
{
    public const string ActiveStatus = "Actif";

    private static readonly DevelopmentAccount[] Accounts =
    [
        new(
            Id: 1,
            UserId: Guid.Parse("de000000-0000-0000-0000-000000000001"),
            Username: "admin",
            Password: "admin",
            FullName: "Administrateur SOPAL",
            Email: "admin@sopal.com",
            Role: "Admin"),
        new(
            Id: 2,
            UserId: Guid.Parse("de000000-0000-0000-0000-000000000002"),
            Username: "fonderie",
            Password: "fonderie",
            FullName: "Responsable Fonderie",
            Email: "fonderie@sopal.com",
            Role: "ResponsableFonderie"),
        new(
            Id: 3,
            UserId: Guid.Parse("de000000-0000-0000-0000-000000000003"),
            Username: "usinage2",
            Password: "usinage2",
            FullName: "Responsable Usinage 2",
            Email: "usinage2@sopal.com",
            Role: "ResponsableUsinage2")
    ];

    /// <summary>
    /// Recherche un compte par identifiant et mot de passe.
    /// La comparaison de l'identifiant est insensible a la casse, celle du mot de passe
    /// ne l'est pas. Retourne null si l'utilisateur est inconnu OU le mot de passe faux,
    /// afin de ne pas divulguer l'existence d'un compte.
    /// </summary>
    public static DevelopmentAccount? Authenticate(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var account = Accounts.FirstOrDefault(a =>
            string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));

        return account is not null && string.Equals(account.Password, password, StringComparison.Ordinal)
            ? account
            : null;
    }

    public static DevelopmentAccount? FindByUsername(string? username) =>
        string.IsNullOrWhiteSpace(username)
            ? null
            : Accounts.FirstOrDefault(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));
}
