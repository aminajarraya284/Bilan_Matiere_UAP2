using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

/// <summary>
/// Profil applicatif local d'un utilisateur authentifie par Microsoft Entra ID.
///
/// PERIMETRE VOLONTAIREMENT ETROIT. L'identite, le mot de passe et le role sont geres
/// par Entra ID ; l'application ne stocke ni secret ni jeton. Elle ne detient qu'une
/// seule decision propre : <see cref="EstActif"/>, qui autorise ou coupe l'acces local.
///
/// <see cref="RoleObserve"/> est une PHOTOGRAPHIE du role porte par le jeton a la
/// derniere connexion. Il sert a l'affichage et au diagnostic, JAMAIS a l'autorisation :
/// les policies lisent la revendication du jeton, pas cette colonne. Une modification en
/// base ne peut donc pas elever les privileges de qui que ce soit.
/// </summary>
public sealed class UtilisateurApplication
{
    public const int EntraObjectIdMaxLength = 150;
    public const int EmailMaxLength = 320;
    public const int NomAffichageMaxLength = 200;

    private UtilisateurApplication() { }

    private UtilisateurApplication(
        string entraObjectId,
        string? email,
        string? nomAffichage,
        string? roleObserve,
        DateTime dateUtc)
    {
        EntraObjectId = Obligatoire(entraObjectId, EntraObjectIdMaxLength, "L'identifiant Entra (oid) est obligatoire.");
        Email = Optionnel(email, EmailMaxLength);
        NomAffichage = Optionnel(nomAffichage, NomAffichageMaxLength);
        RoleObserve = NormaliserRoleObserve(roleObserve);
        EstActif = true;
        DateCreationUtc = GarantirUtc(dateUtc);
        DateModificationUtc = DateCreationUtc;
        DerniereConnexionUtc = DateCreationUtc;
    }

    public int Id { get; private set; }
    public string EntraObjectId { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? NomAffichage { get; private set; }
    public string? RoleObserve { get; private set; }
    public bool EstActif { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime DateModificationUtc { get; private set; }
    public DateTime? DerniereConnexionUtc { get; private set; }

    /// <summary>
    /// Provisionnement automatique a la premiere connexion Entra. Un profil naît
    /// toujours actif : refuser d'emblee un utilisateur que l'IT vient d'habiliter
    /// dans Entra n'aurait aucun sens.
    /// </summary>
    public static UtilisateurApplication ProvisionnerDepuisEntra(
        string entraObjectId,
        string? email,
        string? nomAffichage,
        string? roleObserve,
        DateTime dateUtc)
        => new(entraObjectId, email, nomAffichage, roleObserve, dateUtc);

    /// <summary>
    /// Connexions suivantes : rafraichit les attributs venus du jeton et l'horodatage.
    /// Idempotent, et <see cref="EstActif"/> reste inchange — c'est la seule decision
    /// que l'application detient, Entra ne doit pas la reecrire.
    /// Retourne <c>true</c> si quelque chose a change, pour eviter une ecriture inutile.
    /// </summary>
    public bool SynchroniserDepuisEntra(
        string? email,
        string? nomAffichage,
        string? roleObserve,
        DateTime dateUtc)
    {
        var nouvelEmail = Optionnel(email, EmailMaxLength) ?? Email;
        var nouveauNom = Optionnel(nomAffichage, NomAffichageMaxLength) ?? NomAffichage;
        var nouveauRole = NormaliserRoleObserve(roleObserve) ?? RoleObserve;

        var modifie = !string.Equals(nouvelEmail, Email, StringComparison.Ordinal)
            || !string.Equals(nouveauNom, NomAffichage, StringComparison.Ordinal)
            || !string.Equals(nouveauRole, RoleObserve, StringComparison.Ordinal);

        Email = nouvelEmail;
        NomAffichage = nouveauNom;
        RoleObserve = nouveauRole;
        DerniereConnexionUtc = GarantirUtc(dateUtc);
        if (modifie) DateModificationUtc = GarantirUtc(dateUtc);
        return modifie;
    }

    /// <summary>Retourne <c>true</c> si le statut a reellement change.</summary>
    public bool Activer(DateTime dateUtc)
    {
        if (EstActif) return false;
        EstActif = true;
        DateModificationUtc = GarantirUtc(dateUtc);
        return true;
    }

    /// <summary>Retourne <c>true</c> si le statut a reellement change.</summary>
    public bool Desactiver(DateTime dateUtc)
    {
        if (!EstActif) return false;
        EstActif = false;
        DateModificationUtc = GarantirUtc(dateUtc);
        return true;
    }

    /// <summary>
    /// Roles applicatifs reconnus. Un role Entra inconnu n'est pas une erreur ici : il
    /// est simplement ignore, et l'utilisateur n'obtiendra aucun privilege.
    /// </summary>
    public static bool EstRoleReconnu(string? role)
        => role is "Admin" or "ResponsableFonderie" or "ResponsableUsinage2";

    private static string? NormaliserRoleObserve(string? role)
    {
        var value = role?.Trim();
        return EstRoleReconnu(value) ? value : null;
    }

    private static string Obligatoire(string? value, int maxLength, string message)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maxLength)
        {
            throw new DomainValidationException(message);
        }

        return normalized;
    }

    private static string? Optionnel(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }

    private static DateTime GarantirUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
