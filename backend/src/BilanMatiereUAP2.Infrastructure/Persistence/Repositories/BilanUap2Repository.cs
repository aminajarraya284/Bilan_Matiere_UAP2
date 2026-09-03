using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class BilanUap2Repository(BilanMatiereUAP2DbContext context) : IBilanUap2Repository
{
    public async Task<IReadOnlyList<string>> GetPeriodesDisponiblesAsync(CancellationToken cancellationToken = default)
    {
        var rapports = await context.X3RapportsMachine.AsNoTracking().Where(item => item.IsActive)
            .Select(item => new { item.DateProduction.Year, item.DateProduction.Month }).ToListAsync(cancellationToken);
        var entrees = await context.EntreesOfComposants.AsNoTracking()
            .Select(item => new { item.DateImputation.Year, item.DateImputation.Month }).ToListAsync(cancellationToken);
        var rebuts = await context.RebutsUsinage2.AsNoTracking().Where(item => item.Statut == StatutRebutUsinage2.Valide)
            .Select(item => new { item.Annee, item.Mois }).ToListAsync(cancellationToken);
        var receptions = await context.ReceptionsUsinage2.AsNoTracking().Where(item => item.StatutReception == StatutReceptionUsinage2.Valide)
            .Select(item => new { Year = item.DateReception.Year, Month = item.DateReception.Month }).ToListAsync(cancellationToken);

        return rapports.Select(item => $"{item.Year:0000}-{item.Month:00}")
            .Concat(entrees.Select(item => $"{item.Year:0000}-{item.Month:00}"))
            .Concat(rebuts.Select(item => $"{item.Annee:0000}-{item.Mois:00}"))
            .Concat(receptions.Select(item => $"{item.Year:0000}-{item.Month:00}"))
            .Distinct(StringComparer.Ordinal).OrderByDescending(item => item, StringComparer.Ordinal).ToArray();
    }

    public async Task<IReadOnlyList<X3RapportMachine>> GetRapportsMachineAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default)
    {
        var (debut, fin) = Bornes(annee, mois);
        return await context.X3RapportsMachine
            .AsNoTracking()
            .Where(rapport =>
                rapport.IsActive &&
                rapport.DateProduction >= debut &&
                rapport.DateProduction < fin)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EntreeOfComposant>> GetEntreesOfAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default)
    {
        var (debut, fin) = Bornes(annee, mois);
        return await context.EntreesOfComposants
            .AsNoTracking()
            .Where(entree => entree.DateImputation >= debut && entree.DateImputation < fin)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RebutUsinage2>> GetRebutsValidesAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default)
        => await context.RebutsUsinage2
            .AsNoTracking()
            .Where(rebut =>
                rebut.Statut == StatutRebutUsinage2.Valide &&
                rebut.Annee == annee &&
                rebut.Mois == mois)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReceptionUsinage2>> GetReceptionsValidesAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default)
    {
        var (debut, fin) = Bornes(annee, mois);
        return await context.ReceptionsUsinage2
            .AsNoTracking()
            .Where(reception =>
                reception.StatutReception == StatutReceptionUsinage2.Valide &&
                reception.DateReception >= debut &&
                reception.DateReception < fin)
            .ToListAsync(cancellationToken);
    }

    private static (DateOnly Debut, DateOnly Fin) Bornes(int annee, int mois)
    {
        var debut = new DateOnly(annee, mois, 1);
        return (debut, debut.AddMonths(1));
    }
}
