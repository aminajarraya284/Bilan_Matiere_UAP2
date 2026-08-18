using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts.Rapports;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class RapportsReadRepository(BilanMatiereUAP2DbContext context) : IRapportsReadRepository
{
    public async Task<RapportOfReadData> GetOfAsync(
        string numeroOf,
        string? codeArticle,
        CancellationToken cancellationToken = default)
    {
        var normalizedOf = numeroOf.Trim().ToUpperInvariant();

        var receptionsEntities = await context.ReceptionsUsinage2
            .AsNoTracking()
            .Where(item => item.NumeroOF == normalizedOf)
            .OrderBy(item => item.DateReception)
            .ToListAsync(cancellationToken);
        var receptions = receptionsEntities.Select(item => new RapportOfReceptionResponse(
            item.Id,
            item.DateReception,
            item.NombrePiecesReceptionnees,
            item.PoidsTotalReceptionneKg,
            item.CodeArticle,
            item.CodeTypeMatiere,
            item.AtelierOrigine,
            item.StatutReception.ToString())).ToArray();

        var processusEntities = await context.X3RapportsMachine
            .AsNoTracking()
            .Where(item => item.NumeroOF == normalizedOf && item.IsActive)
            .OrderBy(item => item.DateProduction)
            .ThenBy(item => item.CodePoste)
            .ToListAsync(cancellationToken);
        var processus = processusEntities.Select(item => new RapportOfProcessusResponse(
            item.Id,
            item.DateProduction,
            item.CodePoste,
            item.OrigineProcessus.ToString(),
            item.NumeroOperation,
            item.CodeArticle,
            item.QuantiteTotale,
            item.QuantiteRebut)).ToArray();

        var mouvementEntities = await context.X3MouvementsStock
            .AsNoTracking()
            .Where(item => item.NumeroOFNormalise == normalizedOf)
            .OrderBy(item => item.DateImputation)
            .ThenBy(item => item.NumeroLigneSource)
            .ToListAsync(cancellationToken);
        var mouvements = mouvementEntities.Select(item => new RapportOfMouvementResponse(
            item.Id,
            item.DateImputation,
            item.Code,
            item.QuantiteX3Signee,
            item.Unite,
            item.Transaction,
            item.TypePiece,
            item.PieceOrigine,
            item.Nature.ToString(),
            item.StatutRapprochement.ToString())).ToArray();

        var controlesEntities = await context.ControlesPoidsFonderie
            .AsNoTracking()
            .Include(item => item.TypeMatiere)
            .Where(item => !item.EstSupprime && item.NumeroOF == normalizedOf)
            .OrderByDescending(item => item.DateControle)
            .ToListAsync(cancellationToken);
        var controles = controlesEntities.Select(item => new RapportOfControlePoidsResponse(
            item.Id,
            item.DateControle,
            item.CodeArticle,
            item.TypeMatiere.Code,
            item.PoidsPhysiqueUnitaireKg,
            item.Statut.ToString())).ToArray();

        var normalizedArticle = codeArticle?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedArticle))
        {
            normalizedArticle = processusEntities.Select(item => item.CodeArticle).FirstOrDefault()
                ?? receptionsEntities.Select(item => item.CodeArticle).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
        }

        var references = new List<RapportOfReferenceResponse>();
        if (!string.IsNullOrWhiteSpace(normalizedArticle))
        {
            var gammes = await context.RefGammesFabrication
                .AsNoTracking()
                .Where(item => item.IsActive && item.CodeArticle == normalizedArticle)
                .OrderBy(item => item.NumeroOperation)
                .ToListAsync(cancellationToken);
            references.AddRange(gammes.Select(item => new RapportOfReferenceResponse(
                "Gamme",
                item.CodeArticle,
                null,
                item.NumeroOperation,
                item.CodeAtelier,
                item.CodePoste,
                null,
                item.UniteOperation)));

            var nomenclatures = await context.RefNomenclatures
                .AsNoTracking()
                .Where(item => item.IsActive && item.CodeArticleParent == normalizedArticle)
                .OrderBy(item => item.Niveau)
                .ThenBy(item => item.Sequence)
                .ToListAsync(cancellationToken);
            references.AddRange(nomenclatures.Select(item => new RapportOfReferenceResponse(
                "Nomenclature",
                item.CodeArticleParent,
                item.CodeComposant,
                null,
                null,
                null,
                item.QuantiteLien,
                item.UniteQuantite)));
        }

        return new RapportOfReadData(receptions, processus, mouvements, controles, references);
    }

    public async Task<PagedResult<RapportImportLigneResponse>> SearchImportsAsync(
        DateTime? dateDepuisUtc,
        DateTime? dateJusquaUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var standardQuery = context.ImportBatches.AsNoTracking().AsQueryable();
        var x3Query = context.ImportX3Batches.AsNoTracking().Include(item => item.Fichiers).AsQueryable();

        if (dateDepuisUtc is not null)
        {
            standardQuery = standardQuery.Where(item => item.DateCreationUtc >= dateDepuisUtc.Value);
            x3Query = x3Query.Where(item => item.DateCreationUtc >= dateDepuisUtc.Value);
        }

        if (dateJusquaUtc is not null)
        {
            standardQuery = standardQuery.Where(item => item.DateCreationUtc <= dateJusquaUtc.Value);
            x3Query = x3Query.Where(item => item.DateCreationUtc <= dateJusquaUtc.Value);
        }

        var totalStandard = await standardQuery.CountAsync(cancellationToken);
        var totalX3 = await x3Query.CountAsync(cancellationToken);
        var candidateCount = checked(page * pageSize);

        var standard = await standardQuery
            .OrderByDescending(item => item.DateCreationUtc)
            .Take(candidateCount)
            .ToListAsync(cancellationToken);
        var x3 = await x3Query
            .OrderByDescending(item => item.DateCreationUtc)
            .Take(candidateCount)
            .ToListAsync(cancellationToken);

        var batchIds = standard.Select(item => item.Id).ToArray();
        var sourceFiles = batchIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await context.ImportSourceFiles
                .AsNoTracking()
                .Where(item => batchIds.Contains(item.ImportBatchId))
                .GroupBy(item => item.ImportBatchId)
                .Select(group => new { Id = group.Key, Nom = group.OrderBy(item => item.DateUploadUtc).Select(item => item.NomFichierOriginal).First() })
                .ToDictionaryAsync(item => item.Id, item => item.Nom, cancellationToken);

        var rows = standard.Select(item => new RapportImportLigneResponse(
                item.Id,
                item.TypeImport.ToString(),
                item.ProfilImport,
                item.Statut.ToString(),
                sourceFiles.GetValueOrDefault(item.Id),
                item.DateCreationUtc,
                item.DateFinUtc,
                item.ReadCount,
                item.ValidCount,
                item.InsertedCount,
                item.UpdatedCount,
                item.InsertedCount + item.UpdatedCount,
                item.RejectedCount,
                item.NombreAnomalies,
                item.CreeParNom,
                !string.IsNullOrWhiteSpace(item.ArchiveErrorMessage)))
            .Concat(x3.Select(item => new RapportImportLigneResponse(
                item.Id,
                "RapprochementX3",
                null,
                item.Statut.ToString(),
                item.Fichiers.OrderBy(file => file.DateCreationUtc).Select(file => file.NomFichier).FirstOrDefault(),
                item.DateCreationUtc,
                item.DateFinUtc,
                item.LignesEncoursLues + item.LignesMouvementsLues,
                item.LignesEncoursValides + item.LignesMouvementsValides,
                item.NombreSortiesGenerees,
                0,
                item.NombreSortiesGenerees,
                (item.LignesEncoursLues - item.LignesEncoursValides) + (item.LignesMouvementsLues - item.LignesMouvementsValides),
                item.NombreAnomalies,
                item.CreeParNom,
                false)))
            .OrderByDescending(item => item.DateCreationUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PagedResult<RapportImportLigneResponse>(rows, totalStandard + totalX3);
    }
}
