using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class AlerteRepository(BilanMatiereUAP2DbContext context) : IAlerteRepository
{
    public Task<AlerteMetier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.AlertesMetier.FirstOrDefaultAsync(alerte => alerte.Id == id, cancellationToken);

    public Task<AlerteMetier?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
        => context.AlertesMetier.AsNoTracking().FirstOrDefaultAsync(alerte => alerte.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AlerteMetier>> GetActiveByPeriodAsync(
        string periode,
        CancellationToken cancellationToken = default)
        => await context.AlertesMetier
            .Where(alerte => alerte.EstActive && alerte.Periode == periode)
            .ToListAsync(cancellationToken);

    public async Task<AlerteSearchResult> SearchAsync(
        AlerteSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.AlertesMetier.AsNoTracking().AsQueryable();

        if (filter.SourcesAutorisees is not null)
        {
            var sources = filter.SourcesAutorisees.ToArray();
            query = query.Where(alerte => sources.Contains(alerte.Source));
        }

        if (!string.IsNullOrWhiteSpace(filter.Periode)) query = query.Where(alerte => alerte.Periode == filter.Periode);
        if (filter.Statut is not null) query = query.Where(alerte => alerte.Statut == filter.Statut.Value);
        if (filter.Severite is not null) query = query.Where(alerte => alerte.Severite == filter.Severite.Value);
        if (!string.IsNullOrWhiteSpace(filter.Source)) query = query.Where(alerte => alerte.Source == filter.Source);
        if (!string.IsNullOrWhiteSpace(filter.NumeroOF)) query = query.Where(alerte => alerte.NumeroOF == filter.NumeroOF);
        if (!string.IsNullOrWhiteSpace(filter.CodeArticle))
        {
            query = query.Where(alerte => alerte.CodeArticle == filter.CodeArticle);
        }

        if (!string.IsNullOrWhiteSpace(filter.Recherche))
        {
            var recherche = filter.Recherche;
            query = query.Where(alerte =>
                alerte.Code.Contains(recherche) ||
                alerte.Titre.Contains(recherche) ||
                alerte.Description.Contains(recherche) ||
                (alerte.NumeroOF != null && alerte.NumeroOF.Contains(recherche)) ||
                (alerte.CodeArticle != null && alerte.CodeArticle.Contains(recherche)));
        }

        var total = await query.CountAsync(cancellationToken);
        var compteurs = await query
            .GroupBy(alerte => alerte.Statut)
            .Select(groupe => new { Statut = groupe.Key, Total = groupe.Count() })
            .ToListAsync(cancellationToken);
        var derniereDetection = await query
            .Select(alerte => (DateTime?)alerte.DerniereDetectionLeUtc)
            .MaxAsync(cancellationToken);
        var items = await query
            .OrderByDescending(alerte => alerte.DerniereDetectionLeUtc)
            .ThenBy(alerte => alerte.Code)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        int Compter(StatutAlerte statut)
            => compteurs.FirstOrDefault(compteur => compteur.Statut == statut)?.Total ?? 0;

        return new AlerteSearchResult(
            items,
            total,
            Compter(StatutAlerte.Ouverte),
            Compter(StatutAlerte.Acquittee),
            Compter(StatutAlerte.Resolue),
            Compter(StatutAlerte.Ignoree),
            derniereDetection);
    }

    public async Task AddRangeAsync(
        IEnumerable<AlerteMetier> alertes,
        CancellationToken cancellationToken = default)
        => await context.AlertesMetier.AddRangeAsync(alertes, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
