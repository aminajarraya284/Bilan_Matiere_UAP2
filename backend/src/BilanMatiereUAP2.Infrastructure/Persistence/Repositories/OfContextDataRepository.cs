using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class OfContextDataRepository(BilanMatiereUAP2DbContext context) : IOfContextDataRepository
{
    public async Task<OfContextData> GetAsync(string numeroOf, CancellationToken cancellationToken = default)
    {
        var normalizedOf = numeroOf.Trim().ToUpperInvariant();
        // Use normalized comparisons on the database side to tolerate variations in stored OF formatting
        var encours = await context.OrdresFabrication
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.NumeroOF != null && order.NumeroOF.Trim().ToUpper() == normalizedOf, cancellationToken);
        var rapports = await context.X3RapportsMachine
            .AsNoTracking()
            .Where(row => row.IsActive && row.NumeroOF != null && row.NumeroOF.Trim().ToUpper() == normalizedOf)
            .ToListAsync(cancellationToken);
        var entrees = await context.EntreesOfComposants
            .AsNoTracking()
            .Where(row => row.NumeroOF != null && row.NumeroOF.Trim().ToUpper() == normalizedOf)
            .ToListAsync(cancellationToken);
        var sorties = await context.ConsommationsComposants
            .AsNoTracking()
            .Where(row => row.NumeroOF != null && row.NumeroOF.Trim().ToUpper() == normalizedOf && row.Statut == StatutRapprochementX3.Rapproche)
            .Select(row => new SortieMpMaterialLink(row.NumeroOF, row.ArticlePrincipal, row.CodeTypeMatiere))
            .Distinct()
            .ToListAsync(cancellationToken);

        return new OfContextData(encours, rapports, entrees, sorties);
    }
}
