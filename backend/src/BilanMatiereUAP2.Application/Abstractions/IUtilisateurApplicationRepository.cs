using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

/// <summary>Critères de recherche paginée des profils utilisateurs.</summary>
public sealed record UtilisateurApplicationFiltre(
    string? Recherche,
    bool? EstActif,
    int Page,
    int PageSize);

public interface IUtilisateurApplicationRepository
{
    /// <summary>Page de profils et total correspondant au filtre. Filtrage et pagination en SQL.</summary>
    Task<(IReadOnlyList<UtilisateurApplication> Items, int Total)> SearchAsync(
        UtilisateurApplicationFiltre filtre,
        CancellationToken cancellationToken = default);

    Task<UtilisateurApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Recherche par identifiant Entra stable (oid), clé du provisionnement.</summary>
    Task<UtilisateurApplication?> GetByEntraObjectIdAsync(
        string entraObjectId,
        CancellationToken cancellationToken = default);

    Task AddAsync(UtilisateurApplication utilisateur, CancellationToken cancellationToken = default);
}
