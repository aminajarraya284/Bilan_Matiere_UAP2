using System.ComponentModel.DataAnnotations;

namespace BilanMatiereUAP2.Contracts.Auth;

/// <summary>
/// Requete de connexion locale, reservee a l'environnement Development.
/// L'authentification de production (Entra ID) fera l'objet d'un lot separe.
/// </summary>
public sealed record DevelopmentLoginRequest
{
    [Required(ErrorMessage = "L'identifiant est obligatoire.")]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Utilisateur authentifie. La forme reprend exactement le contrat AuthUser deja
/// consomme par le frontend (auth.store.ts::isAuthUser) : identifiant numerique,
/// role unique et statut. Le mot de passe n'est jamais expose.
/// </summary>
public sealed record AuthenticatedUserResponse(
    int Id,
    string FullName,
    string Username,
    string Email,
    string Role,
    string Status);

/// <summary>
/// Reponse de connexion attendue par le frontend (LoginResponse).
/// <paramref name="ExpiresAtUtc"/> est un ajout informatif : le frontend actuel
/// ne le lit pas encore, l'expiration reelle etant portee par le token lui-meme.
/// </summary>
public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    AuthenticatedUserResponse User);
