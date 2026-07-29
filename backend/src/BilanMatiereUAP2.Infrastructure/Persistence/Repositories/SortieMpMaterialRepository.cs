using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class SortieMpMaterialRepository(BilanMatiereUAP2DbContext dbContext)
    : ISortieMpMaterialRepository
{
    public async Task<IReadOnlyList<SortieMpMaterialLink>> GetByPeriodeAsync(
        int annee,
        int mois,
        IReadOnlyCollection<SortieMpMaterialKey> cles,
        CancellationToken cancellationToken = default)
    {
        var normalizedKeys = cles
            .Where(cle =>
                !string.IsNullOrWhiteSpace(cle.NumeroOF) &&
                !string.IsNullOrWhiteSpace(cle.CodeArticle))
            .Select(cle => new SortieMpMaterialKey(
                cle.NumeroOF.Trim().ToUpperInvariant(),
                cle.CodeArticle.Trim().ToUpperInvariant()))
            .Distinct()
            .ToArray();
        if (normalizedKeys.Length == 0) return [];

        var debut = new DateOnly(annee, mois, 1);
        var fin = debut.AddMonths(1);
        var keySet = normalizedKeys
            .Select(cle => $"{cle.NumeroOF}|{cle.CodeArticle}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<SortieMpMaterialLink>();

        foreach (var chunk in normalizedKeys.Select(cle => cle.NumeroOF).Distinct().Chunk(1000))
        {
            var links = await dbContext.ConsommationsComposants
                .AsNoTracking()
                .Where(consommation =>
                    consommation.Statut == StatutRapprochementX3.Rapproche &&
                    consommation.DateImputation >= debut &&
                    consommation.DateImputation < fin &&
                    chunk.Contains(consommation.NumeroOF))
                .Select(consommation => new
                {
                    consommation.NumeroOF,
                    CodeArticle = consommation.ArticlePrincipal,
                    consommation.CodeTypeMatiere
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            result.AddRange(links
                .Where(lien => keySet.Contains($"{lien.NumeroOF}|{lien.CodeArticle}"))
                .Select(lien => new SortieMpMaterialLink(
                    lien.NumeroOF,
                    lien.CodeArticle,
                    lien.CodeTypeMatiere)));
        }

        return result
            .Distinct()
            .OrderBy(link => link.NumeroOF, StringComparer.Ordinal)
            .ThenBy(link => link.CodeArticle, StringComparer.Ordinal)
            .ThenBy(link => link.CodeTypeMatiere, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<IReadOnlyList<SortieMpMaterialLink>> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var links = await dbContext.ConsommationsComposants
            .AsNoTracking()
            .Where(consommation =>
                consommation.Statut == StatutRapprochementX3.Rapproche &&
                consommation.DateImputation == date)
            .Select(consommation => new SortieMpMaterialLink(
                consommation.NumeroOF,
                consommation.ArticlePrincipal,
                consommation.CodeTypeMatiere))
            .Distinct()
            .ToListAsync(cancellationToken);

        return links
            .OrderBy(link => link.NumeroOF, StringComparer.Ordinal)
            .ThenBy(link => link.CodeArticle, StringComparer.Ordinal)
            .ThenBy(link => link.CodeTypeMatiere, StringComparer.Ordinal)
            .ToArray();
    }
}
