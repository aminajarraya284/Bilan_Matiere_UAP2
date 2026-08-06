using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class DashboardReadRepository(BilanMatiereUAP2DbContext context) : IDashboardReadRepository
{
    public async Task<DashboardOperationsData> GetOperationsAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default)
    {
        var periodeNormalisee = new DateOnly(periode.Year, periode.Month, 1);
        var periodeSuivante = periodeNormalisee.AddMonths(1);

        var conteneurs = await context.InventairesConteneursComptage
            .AsNoTracking()
            .Where(item => item.Periode == periodeNormalisee)
            .GroupBy(item => item.Statut)
            .Select(group => new StatutCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
        var categories = await context.InventairesCategoriesComptage
            .AsNoTracking()
            .Where(item => item.Periode == periodeNormalisee)
            .GroupBy(item => item.Statut)
            .Select(group => new StatutCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
        var fours = await context.InventairesFoursComptage
            .AsNoTracking()
            .Where(item => item.Periode == periodeNormalisee)
            .GroupBy(item => item.Statut)
            .Select(group => new StatutCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var compteurs = conteneurs.Concat(categories).Concat(fours).ToArray();
        var receptionsValidees = await context.ReceptionsUsinage2
            .AsNoTracking()
            .CountAsync(
                item => item.DateReception >= periodeNormalisee &&
                        item.DateReception < periodeSuivante &&
                        item.StatutReception == StatutReceptionUsinage2.Valide,
                cancellationToken);

        return new DashboardOperationsData(
            compteurs.Where(item => item.Statut == StatutInventaireFonderie.Brouillon).Sum(item => item.Count),
            compteurs.Where(item => item.Statut == StatutInventaireFonderie.Valide).Sum(item => item.Count),
            receptionsValidees);
    }

    public async Task<DashboardImportsData> GetImportsAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default)
    {
        var debutUtc = DateTime.SpecifyKind(
            new DateTime(periode.Year, periode.Month, 1),
            DateTimeKind.Utc);
        var finUtc = debutUtc.AddMonths(1);

        var imports = await context.ImportBatches
            .AsNoTracking()
            .Where(item => item.DateCreationUtc >= debutUtc && item.DateCreationUtc < finUtc)
            .GroupBy(_ => 1)
            .Select(group => new ImportAggregate(
                group.Count(item => item.Statut == StatutImport.Termine ||
                                    item.Statut == StatutImport.TermineAvecAvertissementArchive),
                group.Count(item => item.Statut == StatutImport.Partiel),
                group.Count(item => item.Statut == StatutImport.Echec),
                group.Count(item => item.Statut == StatutImport.Duplicate),
                group.Count(item => item.Statut == StatutImport.EnCours),
                group.Sum(item => item.NombreAnomalies),
                group.Max(item => (DateTime?)(item.DateFinUtc ?? item.DateCreationUtc))))
            .SingleOrDefaultAsync(cancellationToken);

        var importsX3 = await context.ImportX3Batches
            .AsNoTracking()
            .Where(item => item.DateCreationUtc >= debutUtc && item.DateCreationUtc < finUtc)
            .GroupBy(_ => 1)
            .Select(group => new ImportAggregate(
                group.Count(item => item.Statut == StatutRapprochementX3.Rapproche),
                group.Count(item => item.Statut == StatutRapprochementX3.ImportPartiel),
                0,
                0,
                group.Count(item => item.DateFinUtc == null),
                group.Sum(item => item.NombreAnomalies),
                group.Max(item => (DateTime?)(item.DateFinUtc ?? item.DateCreationUtc))))
            .SingleOrDefaultAsync(cancellationToken);

        imports ??= ImportAggregate.Empty;
        importsX3 ??= ImportAggregate.Empty;

        return new DashboardImportsData(
            imports.Reussis + importsX3.Reussis,
            imports.Partiels + importsX3.Partiels,
            imports.Echoues + importsX3.Echoues,
            imports.FichiersIdentiques + importsX3.FichiersIdentiques,
            imports.EnCours + importsX3.EnCours,
            imports.NombreAnomalies + importsX3.NombreAnomalies,
            PlusRecente(imports.DernierImportUtc, importsX3.DernierImportUtc));
    }

    private static DateTime? PlusRecente(DateTime? gauche, DateTime? droite)
        => gauche is null ? droite : droite is null ? gauche : gauche > droite ? gauche : droite;

    private sealed record StatutCount(StatutInventaireFonderie Statut, int Count);

    private sealed record ImportAggregate(
        int Reussis,
        int Partiels,
        int Echoues,
        int FichiersIdentiques,
        int EnCours,
        int NombreAnomalies,
        DateTime? DernierImportUtc)
    {
        public static readonly ImportAggregate Empty = new(0, 0, 0, 0, 0, 0, null);
    }
}
