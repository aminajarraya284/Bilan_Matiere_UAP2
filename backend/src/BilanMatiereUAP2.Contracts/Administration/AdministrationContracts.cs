namespace BilanMatiereUAP2.Contracts.Administration;

/// <summary>
/// Profil utilisateur local. Ne porte AUCUN secret : ni mot de passe, ni empreinte,
/// ni jeton — l'application n'en stocke pas.
/// </summary>
public sealed record UtilisateurApplicationResponse(
    int Id,
    string EntraObjectId,
    string? Email,
    string? NomAffichage,
    // Rôle porté par le jeton à la dernière connexion. Lecture seule : Entra en est la source.
    string? RoleObserve,
    bool EstActif,
    DateTime DateCreationUtc,
    DateTime DateModificationUtc,
    DateTime? DerniereConnexionUtc);

public sealed record PagedUtilisateursResponse(
    IReadOnlyList<UtilisateurApplicationResponse> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);

/// <summary>Seule mutation exposée : activer ou couper l'accès local.</summary>
public sealed record ToggleUtilisateurApplicationRequest(bool EstActif);

/// <summary>
/// Compte de développement, exposé en lecture seule et jamais hors Development.
/// Ne contient volontairement aucun champ de mot de passe.
/// </summary>
public sealed record CompteDeveloppementResponse(
    string Username,
    string NomAffichage,
    string Email,
    string Role,
    // Faux si le secret n'est pas configuré sur ce poste : le compte est alors inutilisable.
    bool EstConfigure);
