using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Features.Administration;

/// <summary>Attributs extraits du jeton Entra. Ne contient jamais le jeton lui-même.</summary>
public sealed record EntraIdentite(
    string ObjectId,
    string? Email,
    string? NomAffichage,
    IReadOnlyList<string> Roles);

/// <summary>Issue du rapprochement entre une identité Entra et le profil local.</summary>
public enum ResultatProvisionnement
{
    /// <summary>Profil créé à la première connexion.</summary>
    Cree,

    /// <summary>Profil déjà connu, horodatage et attributs rafraîchis.</summary>
    Synchronise,

    /// <summary>Profil existant mais désactivé localement : l'accès doit être refusé (403).</summary>
    Desactive,

    /// <summary>Aucun rôle applicatif reconnu dans le jeton : aucun privilège, accès refusé.</summary>
    AucunRoleReconnu
}

public interface IEntraUserProvisioningService
{
    Task<ResultatProvisionnement> ProvisionnerAsync(
        EntraIdentite identite,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provisionne le profil applicatif local d'un utilisateur authentifie par Entra ID.
///
/// C'EST LA SIMPLIFICATION CENTRALE : l'equipe IT habilite l'utilisateur dans Entra, et
/// son profil SOPAL naît tout seul a la premiere connexion. Aucun formulaire
/// d'administration, aucune saisie manuelle d'identifiant Entra, aucun mot de passe local.
///
/// L'operation est idempotente : les connexions suivantes rafraichissent les attributs et
/// l'horodatage sans jamais recreer le profil ni toucher a <c>EstActif</c>.
/// </summary>
public sealed class EntraUserProvisioningService(
    IUtilisateurApplicationRepository repository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService) : IEntraUserProvisioningService
{
    /// <summary>
    /// Un jeton porteur est presente a CHAQUE requete : ecrire la date de derniere
    /// connexion a chaque fois transformerait une lecture en ecriture. On ne rafraichit
    /// donc que si l'horodatage a suffisamment vieilli — la donnee reste juste a la
    /// minute pres, ce qui est la precision utile pour un ecran d'administration.
    /// </summary>
    public static readonly TimeSpan FenetreRafraichissement = TimeSpan.FromMinutes(5);

    public async Task<ResultatProvisionnement> ProvisionnerAsync(
        EntraIdentite identite,
        CancellationToken cancellationToken = default)
    {
        // Aucun role reconnu : on n'attribue JAMAIS de role par defaut. Un utilisateur
        // authentifie sans habilitation SOPAL n'a acces a rien.
        var role = identite.Roles.FirstOrDefault(UtilisateurApplication.EstRoleReconnu);
        if (role is null)
        {
            return ResultatProvisionnement.AucunRoleReconnu;
        }

        var maintenant = dateTimeProvider.UtcNow;
        var existant = await repository.GetByEntraObjectIdAsync(identite.ObjectId, cancellationToken);

        if (existant is null)
        {
            var utilisateur = UtilisateurApplication.ProvisionnerDepuisEntra(
                identite.ObjectId,
                identite.Email,
                identite.NomAffichage,
                role,
                maintenant);

            await repository.AddAsync(utilisateur, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditService.EnregistrerAsync(
                new AuditEntry(
                    ActionAudit.Creation,
                    DomaineAudit.Administration,
                    nameof(UtilisateurApplication),
                    utilisateur.Id.ToString(),
                    utilisateur.EntraObjectId,
                    "Provisionnement automatique Entra",
                    "Création du profil utilisateur à la première connexion.",
                    new Dictionary<string, object?>(),
                    // Attributs de profil seulement : les revendications brutes du jeton
                    // ne sont jamais recopiees dans le journal.
                    new Dictionary<string, object?>
                    {
                        ["entraObjectId"] = utilisateur.EntraObjectId,
                        ["email"] = utilisateur.Email,
                        ["nomAffichage"] = utilisateur.NomAffichage,
                        ["roleObserve"] = utilisateur.RoleObserve,
                        ["estActif"] = utilisateur.EstActif
                    }),
                cancellationToken);

            return ResultatProvisionnement.Cree;
        }

        // Un profil desactive localement coupe l'acces, meme si Entra a authentifie
        // l'utilisateur : c'est la seule decision que l'application detient en propre.
        if (!existant.EstActif)
        {
            return ResultatProvisionnement.Desactive;
        }

        var horodatagePerime = existant.DerniereConnexionUtc is null
            || maintenant - existant.DerniereConnexionUtc.Value >= FenetreRafraichissement;

        var attributsModifies = existant.SynchroniserDepuisEntra(
            identite.Email,
            identite.NomAffichage,
            role,
            maintenant);

        if (attributsModifies || horodatagePerime)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ResultatProvisionnement.Synchronise;
    }
}
