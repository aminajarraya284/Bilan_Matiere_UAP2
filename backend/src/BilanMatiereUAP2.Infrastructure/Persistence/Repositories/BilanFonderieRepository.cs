using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

/// <summary>
/// Les agregations sont faites EN MEMOIRE, apres filtrage sur la periode.
///
/// Raison : EF Core 8 ne sait pas agreger un <c>decimal</c> sous SQLite (provider des
/// tests d'integration). Le contournement habituel — caster en <c>double</c> — introduit
/// une perte de precision inacceptable sur des poids qui alimentent un bilan matiere.
/// Le volume est borne par le mois demande, ce qui rend le chargement acceptable.
/// </summary>
public sealed class BilanFonderieRepository(BilanMatiereUAP2DbContext context) : IBilanFonderieRepository
{
    public async Task<IReadOnlyList<BilanFonderieTypeMatiere>> GetTypesMatiereActifsAsync(
        CancellationToken cancellationToken = default)
        => await context.TypesMatiere
            .AsNoTracking()
            .Where(type => type.EstActif)
            .OrderBy(type => type.Code)
            .Select(type => new BilanFonderieTypeMatiere(type.Id, type.Code))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BilanFonderieStock>> GetStocksAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default)
        => await context.InventairesFonderie
            .AsNoTracking()
            .Where(inventaire =>
                inventaire.Periode == periode &&
                inventaire.Statut != StatutInventaireFonderie.Annule)
            .Select(inventaire => new BilanFonderieStock(
                inventaire.TypeMatiereId,
                inventaire.StockDebutKg,
                inventaire.StockFinKg))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BilanFonderieSortieX3>> GetSortiesMagasinX3Async(
        DateOnly periode,
        CancellationToken cancellationToken = default)
    {
        var fin = periode.AddMonths(1);
        var lignes = await context.ConsommationsComposants
            .AsNoTracking()
            .Where(consommation =>
                consommation.Statut == StatutRapprochementX3.Rapproche &&
                consommation.DateImputation >= periode &&
                consommation.DateImputation < fin)
            .Select(consommation => new
            {
                consommation.CodeTypeMatiere,
                consommation.Unite,
                consommation.Consommation
            })
            .ToListAsync(cancellationToken);

        return lignes
            .GroupBy(ligne => new { ligne.CodeTypeMatiere, ligne.Unite })
            .Select(groupe => new BilanFonderieSortieX3(
                groupe.Key.CodeTypeMatiere,
                groupe.Key.Unite,
                groupe.Sum(ligne => ligne.Consommation),
                groupe.Count()))
            .ToArray();
    }

    public async Task<IReadOnlyList<BilanFonderieDechet>> GetDechetsAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default)
    {
        var fin = periode.AddMonths(1);
        var lignes = await context.DechetsFonderie
            .AsNoTracking()
            .Where(dechet =>
                !dechet.EstSupprime &&
                dechet.Statut == StatutDechet.Valide &&
                dechet.DateDeclaration >= periode &&
                dechet.DateDeclaration < fin)
            .Select(dechet => new { dechet.TypeMatiereId, dechet.PoidsKg })
            .ToListAsync(cancellationToken);

        return lignes
            .GroupBy(ligne => ligne.TypeMatiereId)
            .Select(groupe => new BilanFonderieDechet(groupe.Key, groupe.Sum(ligne => ligne.PoidsKg)))
            .ToArray();
    }

    public async Task<IReadOnlyList<BilanFonderieReception>> GetReceptionsUsinage2Async(
        DateOnly periode,
        CancellationToken cancellationToken = default)
    {
        var fin = periode.AddMonths(1);
        var lignes = await context.ReceptionsUsinage2
            .AsNoTracking()
            .Where(reception =>
                reception.StatutReception == StatutReceptionUsinage2.Valide &&
                reception.DateReception >= periode &&
                reception.DateReception < fin &&
                reception.CodeTypeMatiere != null)
            .Select(reception => new { reception.CodeTypeMatiere, reception.PoidsTotalReceptionneKg })
            .ToListAsync(cancellationToken);

        return lignes
            .GroupBy(ligne => ligne.CodeTypeMatiere!, StringComparer.OrdinalIgnoreCase)
            .Select(groupe => new BilanFonderieReception(
                groupe.Key,
                groupe.Sum(ligne => ligne.PoidsTotalReceptionneKg ?? 0m),
                groupe.Count(ligne => ligne.PoidsTotalReceptionneKg is not null),
                groupe.Count(ligne => ligne.PoidsTotalReceptionneKg is null)))
            .ToArray();
    }

    public async Task<int> CompterReceptionsSansTypeMatiereAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default)
    {
        var fin = periode.AddMonths(1);
        return await context.ReceptionsUsinage2
            .AsNoTracking()
            .CountAsync(
                reception =>
                    reception.StatutReception == StatutReceptionUsinage2.Valide &&
                    reception.DateReception >= periode &&
                    reception.DateReception < fin &&
                    reception.CodeTypeMatiere == null,
                cancellationToken);
    }
}
