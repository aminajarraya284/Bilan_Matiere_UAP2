using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Contracts.Administration;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Administration;

public sealed record GetUtilisateursQuery(
    string? Recherche,
    bool? EstActif,
    int Page = 1,
    int PageSize = 25);

public interface IAdministrationService
{
    Task<PagedUtilisateursResponse> SearchAsync(
        GetUtilisateursQuery query,
        CancellationToken cancellationToken = default);

    Task<UtilisateurApplicationResponse> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<UtilisateurApplicationResponse> ChangerActivationAsync(
        int id,
        bool estActif,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Administration des profils utilisateurs.
///
/// PERIMETRE VOLONTAIREMENT REDUIT. Il n'existe ni creation manuelle (le profil naît au
/// premier login Entra, voir <see cref="IEntraUserProvisioningService"/>), ni changement
/// de role (Entra en est la source), ni gestion de mot de passe (Entra egalement).
/// L'unique mutation exposee est l'activation locale.
/// </summary>
public sealed class AdministrationService(
    IUtilisateurApplicationRepository repository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService) : IAdministrationService
{
    public const int PageSizeMax = 100;

    public async Task<PagedUtilisateursResponse> SearchAsync(
        GetUtilisateursQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
        {
            throw new ValidationException([new ValidationFailure("page", "La page doit être supérieure ou égale à 1.")]);
        }

        if (query.PageSize is < 1 or > PageSizeMax)
        {
            throw new ValidationException([
                new ValidationFailure("pageSize", $"La taille de page doit être comprise entre 1 et {PageSizeMax}.")
            ]);
        }

        var (items, total) = await repository.SearchAsync(
            new UtilisateurApplicationFiltre(query.Recherche, query.EstActif, query.Page, query.PageSize),
            cancellationToken);

        return new PagedUtilisateursResponse(
            items.Select(Map).ToArray(),
            total,
            query.Page,
            query.PageSize,
            Math.Max(1, (int)Math.Ceiling(total / (double)query.PageSize)));
    }

    public async Task<UtilisateurApplicationResponse> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var utilisateur = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Profil utilisateur introuvable.");
        return Map(utilisateur);
    }

    public async Task<UtilisateurApplicationResponse> ChangerActivationAsync(
        int id,
        bool estActif,
        CancellationToken cancellationToken = default)
    {
        var utilisateur = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Profil utilisateur introuvable.");

        var avant = Snapshot(utilisateur);
        var maintenant = dateTimeProvider.UtcNow;
        var modifie = estActif ? utilisateur.Activer(maintenant) : utilisateur.Desactiver(maintenant);

        // Rejouer la meme activation ne doit ni ecrire ni polluer le journal d'audit.
        if (!modifie) return Map(utilisateur);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.EnregistrerAsync(
            new AuditEntry(
                ActionAudit.Modification,
                DomaineAudit.Administration,
                nameof(UtilisateurApplication),
                utilisateur.Id.ToString(),
                utilisateur.EntraObjectId,
                nameof(AdministrationService),
                estActif ? "Activation d'un profil utilisateur." : "Désactivation d'un profil utilisateur.",
                avant,
                Snapshot(utilisateur)),
            cancellationToken);

        return Map(utilisateur);
    }

    /// <summary>
    /// Instantane d'audit. Ne contient que des attributs de profil : aucun secret n'existe
    /// sur cette entite, donc aucun ne peut fuir ici.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> Snapshot(UtilisateurApplication utilisateur)
        => new Dictionary<string, object?>
        {
            ["entraObjectId"] = utilisateur.EntraObjectId,
            ["email"] = utilisateur.Email,
            ["nomAffichage"] = utilisateur.NomAffichage,
            ["roleObserve"] = utilisateur.RoleObserve,
            ["estActif"] = utilisateur.EstActif
        };

    internal static UtilisateurApplicationResponse Map(UtilisateurApplication utilisateur)
        => new(
            utilisateur.Id,
            utilisateur.EntraObjectId,
            utilisateur.Email,
            utilisateur.NomAffichage,
            utilisateur.RoleObserve,
            utilisateur.EstActif,
            utilisateur.DateCreationUtc,
            utilisateur.DateModificationUtc,
            utilisateur.DerniereConnexionUtc);
}
