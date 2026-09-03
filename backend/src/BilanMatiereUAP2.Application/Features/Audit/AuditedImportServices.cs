using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.EntreeOf;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Application.Services;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Contracts.Imports;
using BilanMatiereUAP2.Contracts.ImportsX3;
using BilanMatiereUAP2.Contracts.MouvementsStock;
using BilanMatiereUAP2.Contracts.RapportsMachine;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Features.Audit;

public sealed class AuditedX3ImportService(
    X3ImportService inner,
    IAuditService auditService) : IX3ImportService
{
    public async Task<ImportX3Response> ImportAsync(
        ImportSortiesMatiereX3Command command,
        CancellationToken cancellationToken)
    {
        var nomsFichiers = new[]
        {
            command.EncoursProductFile?.FileName,
            command.MouvementsStockFile?.FileName
        }.Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToArray();

        ImportX3Response result;
        try
        {
            result = await inner.ImportAsync(command, cancellationToken);
        }
        catch (Exception exception)
        {
            await TryAuditFailureAsync(
                auditService,
                TypeImport.SortiesMatiereX3.ToString(),
                nomsFichiers,
                nameof(AuditedX3ImportService),
                exception,
                cancellationToken);
            throw;
        }

        var action = ImportAuditMapper.Action(result.EstDoublon, result.Statut);
        await auditService.EnregistrerAsync(
            ImportAuditMapper.Entry(
                action,
                result.ImportBatchId,
                TypeImport.SortiesMatiereX3.ToString(),
                nomsFichiers,
                result.Statut,
                result.EncoursProduct.LignesLues + result.MouvementsStock.LignesLues,
                null,
                null,
                result.EncoursProduct.LignesIgnorees + result.MouvementsStock.LignesIgnorees,
                result.DateFinUtc ?? result.DateCreationUtc,
                nameof(AuditedX3ImportService),
                new Dictionary<string, object?>
                {
                    ["sortiesGenerees"] = result.SortiesGenerees,
                    ["nombreAnomalies"] = result.NombreAnomalies
                }),
            cancellationToken);
        return result;
    }

    public Task<ImportX3Response> GetByIdAsync(GetImportX3ByIdQuery query, CancellationToken cancellationToken)
        => inner.GetByIdAsync(query, cancellationToken);

    public Task<IReadOnlyList<ImportX3AnomalyResponse>> GetAnomaliesAsync(
        GetImportX3AnomaliesQuery query,
        CancellationToken cancellationToken)
        => inner.GetAnomaliesAsync(query, cancellationToken);

    public Task<PagedConsommationsComposantsResponse> GetSortiesAsync(
        GetSortiesMatiereQuery query,
        CancellationToken cancellationToken)
        => inner.GetSortiesAsync(query, cancellationToken);

    public Task<OrdreFabricationContexteResponse> GetOrdreContexteAsync(
        GetOrdreFabricationContexteQuery query,
        CancellationToken cancellationToken)
        => inner.GetOrdreContexteAsync(query, cancellationToken);

    private static async Task TryAuditFailureAsync(
        IAuditService auditService,
        string type,
        IReadOnlyCollection<string> nomsFichiers,
        string source,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditService.EnregistrerAsync(
                ImportAuditMapper.Failure(type, nomsFichiers, source, exception),
                cancellationToken);
        }
        catch
        {
            // L'exception d'import initiale doit rester celle exposee a l'appelant.
        }
    }
}

public sealed class AuditedReportMachineService(
    ReportMachineService inner,
    IAuditService auditService) : IReportMachineService
{
    public async Task<ImportRapportMachineResponse> ImportAsync(
        Stream fileStream,
        string profilImport,
        string sourceSystem,
        string nomFichierOriginal,
        CancellationToken cancellationToken = default)
    {
        ImportRapportMachineResponse result;
        try
        {
            result = await inner.ImportAsync(fileStream, profilImport, sourceSystem, nomFichierOriginal, cancellationToken);
        }
        catch (Exception exception)
        {
            await AuditedX3ImportServiceFailure.TryAsync(
                auditService,
                TypeImport.RapportMachine.ToString(),
                [nomFichierOriginal],
                nameof(AuditedReportMachineService),
                exception,
                cancellationToken);
            throw;
        }

        await auditService.EnregistrerAsync(
            ImportAuditMapper.Entry(
                ImportAuditMapper.Action(result.Duplicate, result.Statut),
                result.ImportBatchId,
                TypeImport.RapportMachine.ToString(),
                [nomFichierOriginal],
                result.Statut,
                result.RowsRead,
                result.Inserted,
                result.Updated,
                result.Rejected,
                DateTime.UtcNow,
                nameof(AuditedReportMachineService),
                new Dictionary<string, object?>
                {
                    ["lignesInchangees"] = result.Unchanged,
                    ["lignesValides"] = result.RowsValid,
                    ["nombreAnomalies"] = result.Anomalies,
                    ["profilImport"] = profilImport,
                    ["sourceSystem"] = sourceSystem
                }),
            cancellationToken);
        return result;
    }

    public Task<PagedResult<RapportMachineResponse>> QueryAsync(
        string? numeroOF,
        string? codeArticle,
        string? codePoste,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => inner.QueryAsync(numeroOF, codeArticle, codePoste, page, pageSize, cancellationToken);

    public Task<RapportMachineConsultationResponse> QueryConsultationAsync(
        RapportMachineConsultationQuery query,
        CancellationToken cancellationToken = default)
        => inner.QueryConsultationAsync(query, cancellationToken);

    public Task<ImportTrackingResponse> GetImportTrackingAsync(CancellationToken cancellationToken = default)
        => inner.GetImportTrackingAsync(cancellationToken);

    public Task<RapportMachineAggregateResponse> AggregateAsync(
        string numeroOF,
        string codeArticle,
        int? annee = null,
        int? mois = null,
        CancellationToken cancellationToken = default)
        => inner.AggregateAsync(numeroOF, codeArticle, annee, mois, cancellationToken);

    public Task<RecalculerOrigineProcessusResult> RecalculerOrigineProcessusAsync(
        CancellationToken cancellationToken = default)
        => inner.RecalculerOrigineProcessusAsync(cancellationToken);
}

public sealed class AuditedEntreeOfImportService(
    EntreeOfImportService inner,
    IAuditService auditService) : IEntreeOfImportService
{
    public async Task<ImportEntreeOfResponse> ImportAsync(
        Stream fileStream,
        string sourceSystem,
        string nomFichierOriginal,
        CancellationToken cancellationToken = default)
    {
        ImportEntreeOfResponse result;
        try
        {
            result = await inner.ImportAsync(fileStream, sourceSystem, nomFichierOriginal, cancellationToken);
        }
        catch (Exception exception)
        {
            await AuditedX3ImportServiceFailure.TryAsync(
                auditService,
                TypeImport.MvtEntreeOF.ToString(),
                [nomFichierOriginal],
                nameof(AuditedEntreeOfImportService),
                exception,
                cancellationToken);
            throw;
        }

        await auditService.EnregistrerAsync(
            ImportAuditMapper.Entry(
                ImportAuditMapper.Action(result.Duplicate, result.Statut),
                result.ImportBatchId,
                TypeImport.MvtEntreeOF.ToString(),
                [nomFichierOriginal],
                result.Statut,
                result.RowsRead,
                result.Inserted,
                result.Updated,
                result.Rejected,
                DateTime.UtcNow,
                nameof(AuditedEntreeOfImportService),
                new Dictionary<string, object?>
                {
                    ["lignesInchangees"] = result.Unchanged,
                    ["lignesValides"] = result.RowsValid,
                    ["sourceSystem"] = sourceSystem
                }),
            cancellationToken);
        return result;
    }

    public Task<EntreeOfResolutionResult> ResolveAsync(
        string numeroOF,
        int annee,
        int mois,
        string? codeArticleAttendu,
        CancellationToken cancellationToken = default)
        => inner.ResolveAsync(numeroOF, annee, mois, codeArticleAttendu, cancellationToken);

    public Task<MouvementsStockConsultationResponse> QueryMouvementsAsync(
        MouvementStockConsultationQuery query,
        CancellationToken cancellationToken = default)
        => inner.QueryMouvementsAsync(query, cancellationToken);

    public Task<ImportTrackingResponse> GetImportTrackingAsync(CancellationToken cancellationToken = default)
        => inner.GetImportTrackingAsync(cancellationToken);
}

public sealed class AuditedManufacturingReferencesService(
    ManufacturingReferencesService inner,
    IManufacturingReferencesRepository repository,
    IImportHashCalculator hashCalculator,
    IAuditService auditService) : IManufacturingReferencesService
{
    public Task<ImportReferenceResponse> ImportGammesAsync(
        Stream fileStream,
        string profilImport,
        string sourceSystem,
        string nomFichierOriginal,
        bool isCompleteExport = false,
        CancellationToken cancellationToken = default)
        => ImportAsync(
            fileStream,
            profilImport,
            sourceSystem,
            nomFichierOriginal,
            isCompleteExport,
            TypeImport.GammeFabrication,
            inner.ImportGammesAsync,
            cancellationToken);

    public Task<ImportReferenceResponse> ImportNomenclaturesAsync(
        Stream fileStream,
        string profilImport,
        string sourceSystem,
        string nomFichierOriginal,
        bool isCompleteExport = false,
        CancellationToken cancellationToken = default)
        => ImportAsync(
            fileStream,
            profilImport,
            sourceSystem,
            nomFichierOriginal,
            isCompleteExport,
            TypeImport.Nomenclature,
            inner.ImportNomenclaturesAsync,
            cancellationToken);

    private async Task<ImportReferenceResponse> ImportAsync(
        Stream fileStream,
        string profilImport,
        string sourceSystem,
        string nomFichierOriginal,
        bool isCompleteExport,
        TypeImport typeImport,
        Func<Stream, string, string, string, bool, CancellationToken, Task<ImportReferenceResponse>> import,
        CancellationToken cancellationToken)
    {
        var bytes = await ReadAllBytesAsync(fileStream, cancellationToken);
        var hash = hashCalculator.CalculateSha256(bytes);
        ImportReferenceResponse result;
        try
        {
            result = await import(
                new MemoryStream(bytes, writable: false),
                profilImport,
                sourceSystem,
                nomFichierOriginal,
                isCompleteExport,
                cancellationToken);
        }
        catch (Exception exception)
        {
            var failedBatch = await FindBatchSafelyAsync(hash, typeImport, cancellationToken);
            try
            {
                await auditService.EnregistrerAsync(
                    ImportAuditMapper.Failure(
                        typeImport.ToString(),
                        [nomFichierOriginal],
                        nameof(AuditedManufacturingReferencesService),
                        exception,
                        failedBatch),
                    cancellationToken);
            }
            catch
            {
                // L'exception d'import initiale doit rester celle exposee a l'appelant.
            }
            throw;
        }

        var batch = await FindBatchAsync(hash, typeImport, cancellationToken);
        await auditService.EnregistrerAsync(
            ImportAuditMapper.Entry(
                ImportAuditMapper.Action(result.Duplicate, batch?.Statut.ToString()),
                batch?.Id,
                typeImport.ToString(),
                [nomFichierOriginal],
                batch?.Statut.ToString() ?? (result.Duplicate ? "Duplicate" : "Termine"),
                batch?.ReadCount,
                result.Inserted,
                result.Updated,
                batch?.RejectedCount,
                batch?.DateFinUtc ?? DateTime.UtcNow,
                nameof(AuditedManufacturingReferencesService),
                new Dictionary<string, object?>
                {
                    ["lignesInchangees"] = batch?.UnchangedCount,
                    ["lignesValides"] = batch?.ValidCount,
                    ["profilImport"] = profilImport,
                    ["sourceSystem"] = sourceSystem,
                    ["exportComplet"] = isCompleteExport
                }),
            cancellationToken);
        return result;
    }

    private async Task<ImportBatch?> FindBatchAsync(
        string hash,
        TypeImport typeImport,
        CancellationToken cancellationToken)
    {
        var batch = await repository.GetBatchByHashAsync(hash, cancellationToken);
        return batch?.TypeImport == typeImport ? batch : null;
    }

    private async Task<ImportBatch?> FindBatchSafelyAsync(
        string hash,
        TypeImport typeImport,
        CancellationToken cancellationToken)
    {
        try
        {
            return await FindBatchAsync(hash, typeImport, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }
}

internal static class AuditedX3ImportServiceFailure
{
    public static async Task TryAsync(
        IAuditService auditService,
        string type,
        IReadOnlyCollection<string> nomsFichiers,
        string source,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditService.EnregistrerAsync(
                ImportAuditMapper.Failure(type, nomsFichiers, source, exception),
                cancellationToken);
        }
        catch
        {
            // L'exception d'import initiale doit rester celle exposee a l'appelant.
        }
    }
}

internal static class ImportAuditMapper
{
    public static ActionAudit Action(bool duplicate, string? statut)
    {
        if (duplicate || string.Equals(statut, StatutImport.Duplicate.ToString(), StringComparison.OrdinalIgnoreCase))
            return ActionAudit.ImportDuplicate;
        if (string.Equals(statut, StatutImport.Partiel.ToString(), StringComparison.OrdinalIgnoreCase))
            return ActionAudit.ImportPartiel;
        if (string.Equals(statut, StatutImport.Echec.ToString(), StringComparison.OrdinalIgnoreCase))
            return ActionAudit.ImportEchoue;
        return ActionAudit.ImportReussi;
    }

    public static AuditEntry Entry(
        ActionAudit action,
        Guid? importBatchId,
        string type,
        IReadOnlyCollection<string> nomsFichiers,
        string statut,
        int? lignesLues,
        int? lignesInserees,
        int? lignesMisesAJour,
        int? lignesRejetees,
        DateTime dateUtc,
        string source,
        IReadOnlyDictionary<string, object?>? extra = null)
    {
        var informations = new Dictionary<string, object?>
        {
            ["importBatchId"] = importBatchId,
            ["type"] = type,
            ["nomsFichiers"] = nomsFichiers,
            ["statut"] = statut,
            ["lignesLues"] = lignesLues,
            ["lignesInserees"] = lignesInserees,
            ["lignesMisesAJour"] = lignesMisesAJour,
            ["lignesRejetees"] = lignesRejetees,
            ["dateUtc"] = dateUtc
        };
        if (extra is not null)
        {
            foreach (var pair in extra) informations[pair.Key] = pair.Value;
        }

        return new AuditEntry(
            action,
            DomaineAudit.ImportX3,
            nameof(ImportBatch),
            importBatchId?.ToString() ?? "ECHEC_SANS_LOT",
            importBatchId is null ? type : $"{type}|{importBatchId}",
            source,
            $"{action} : import {type}.",
            Informations: informations);
    }

    public static AuditEntry Failure(
        string type,
        IReadOnlyCollection<string> nomsFichiers,
        string source,
        Exception exception,
        ImportBatch? batch = null)
        => Entry(
            ActionAudit.ImportEchoue,
            batch?.Id,
            type,
            nomsFichiers,
            batch?.Statut.ToString() ?? StatutImport.Echec.ToString(),
            batch?.ReadCount,
            batch?.InsertedCount,
            batch?.UpdatedCount,
            batch?.RejectedCount,
            batch?.DateFinUtc ?? DateTime.UtcNow,
            source,
            new Dictionary<string, object?>
            {
                ["typeErreur"] = exception.GetType().Name
            });
}
