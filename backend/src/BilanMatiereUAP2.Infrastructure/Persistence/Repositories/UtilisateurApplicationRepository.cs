using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class UtilisateurApplicationRepository(BilanMatiereUAP2DbContext context)
    : IUtilisateurApplicationRepository
{
    public async Task<(IReadOnlyList<UtilisateurApplication> Items, int Total)> SearchAsync(
        UtilisateurApplicationFiltre filtre,
        CancellationToken cancellationToken = default)
    {
        // Filtres appliques AVANT materialisation : la table n'est jamais chargee en memoire.
        var query = context.UtilisateursApplication.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtre.Recherche))
        {
            var terme = filtre.Recherche.Trim();
            query = query.Where(u =>
                (u.NomAffichage != null && EF.Functions.Like(u.NomAffichage, $"%{terme}%")) ||
                (u.Email != null && EF.Functions.Like(u.Email, $"%{terme}%")));
        }

        if (filtre.EstActif is not null)
        {
            query = query.Where(u => u.EstActif == filtre.EstActif.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.NomAffichage ?? u.Email ?? u.EntraObjectId)
            .Skip((filtre.Page - 1) * filtre.PageSize)
            .Take(filtre.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<UtilisateurApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => context.UtilisateursApplication.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<UtilisateurApplication?> GetByEntraObjectIdAsync(
        string entraObjectId,
        CancellationToken cancellationToken = default)
        => context.UtilisateursApplication
            .FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, cancellationToken);

    public Task AddAsync(UtilisateurApplication utilisateur, CancellationToken cancellationToken = default)
        => context.UtilisateursApplication.AddAsync(utilisateur, cancellationToken).AsTask();
}
