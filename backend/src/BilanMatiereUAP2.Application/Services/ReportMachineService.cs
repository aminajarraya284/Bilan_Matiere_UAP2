using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Imports;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Contracts.Imports;
using BilanMatiereUAP2.Contracts.RapportsMachine;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Services;

public sealed class ReportMachineService(
    IReportMachineRepository repository,
    IReportMachineReader reader,
    IImportArchiveService archiveService,
    IImportDependencyRefreshService dependencyRefreshService,
    ICurrentUserService currentUser,
    IImportHashCalculator hashCalculator) : IReportMachineService
{
    // La classification vient des 3 premiers caracteres du code poste (regle SOPAL).
    // Voir OrigineProcessusRules : aucun referentiel manuel n'intervient.
    private static OrigineProcessus ResoudreOrigine(string? codePoste)
        => OrigineProcessusRules.DeterminerOrigineProcessus(codePoste);

    public async Task<ImportRapportMachineResponse> ImportAsync(Stream fileStream, string profilImport, string sourceSystem, string nomFichierOriginal, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAllBytesAsync(fileStream, cancellationToken);
        var hash = hashCalculator.CalculateSha256(bytes);
        var profile = profilImport?.Trim() ?? string.Empty;
        var duplicate = await repository.GetCompletedBatchByHashAsync(hash, profile, cancellationToken) is not null;
        var batch = ImportBatch.Demarrer(
            hash,
            TypeImport.RapportMachine,
            profile,
            currentUser.UserId,
            currentUser.UserName,
            DateTime.UtcNow);
        await repository.AddBatchAsync(batch, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        if (duplicate)
        {
            // Idempotence choisie : SHA-256 identique => nouveau lot traçable en Duplicate,
            // sans relecture ni réécriture des lignes (tous les compteurs de lignes restent à 0).
            batch.TerminerImport(StatutImport.Duplicate, DateTime.UtcNow, 0, 0, 0, 0, 0, 0, 0);
            await repository.SaveChangesAsync(cancellationToken);
            await ArchiverEtTracerErreurAsync(batch, profile, nomFichierOriginal, bytes, cancellationToken);
            return ToImportResponse(batch, true);
        }

        IDisposable? transaction = null;
        try
        {
            transaction = await repository.BeginTransactionAsync(cancellationToken);
            var readResult = await reader.ReadAsync(new X3ImportFileContent("YRJMCH.xlsx", bytes.Length, bytes), cancellationToken);
            var now = DateTime.UtcNow;
            var mappingErrors = readResult.Rows
                .Where(row => ResoudreOrigine(row.CodePoste) == OrigineProcessus.Inconnue)
                .Select(row => new X3ReadError(
                    row.NumeroLigneSource,
                    StatutRapprochementX3.PosteProcessusNonMappe,
                    $"Le poste '{row.CodePoste}' n'est pas classé CTRL, SCI, EMR ou TRN.",
                    row.NumeroOF,
                    NomFeuilleSource: row.NomFeuilleSource));
            var anomalies = readResult.Errors.Concat(mappingErrors).Select(error => ImportReferenceAnomaly.Creer(
                batch.Id,
                TypeImport.RapportMachine,
                error.NumeroLigneSource,
                error.NomFeuilleSource,
                error.Message,
                now)).ToArray();
            if (anomalies.Length > 0) await repository.AddAnomaliesAsync(anomalies, cancellationToken);

            var rows = readResult.Rows
                .Select(row => X3RapportMachine.Creer(
                    row.DateProduction,
                    row.CodePoste,
                    ResoudreOrigine(row.CodePoste),
                    row.NumeroOperation,
                    row.CodeArticle,
                    row.DesignationArticle,
                    row.NumeroOF,
                    row.Equipe,
                    row.QuantiteTotale,
                    row.QuantiteRebut,
                    row.AlternativeGamme,
                    sourceSystem,
                    batch.Id,
                    now))
                .GroupBy(row => row.CleMetier)
                .Select(group => group.Last())
                .ToArray();

            var existingByKey = (await repository.GetByCleMetiersAsync(rows.Select(row => row.CleMetier).ToArray(), cancellationToken))
                .ToDictionary(row => row.CleMetier, StringComparer.OrdinalIgnoreCase);
            var toAdd = new List<X3RapportMachine>();
            var inserted = 0;
            var updated = 0;
            var unchanged = 0;
            foreach (var row in rows)
            {
                if (existingByKey.TryGetValue(row.CleMetier, out var existing))
                {
                    if (existing.EstEquivalent(
                        row.DesignationArticle,
                        row.QuantiteTotale,
                        row.QuantiteRebut,
                        row.AlternativeGamme,
                        sourceSystem,
                        row.OrigineProcessus))
                    {
                        unchanged++;
                    }
                    else
                    {
                        existing.MettreAJour(
                            row.DesignationArticle,
                            row.QuantiteTotale,
                            row.QuantiteRebut,
                            row.AlternativeGamme,
                            sourceSystem,
                            row.OrigineProcessus,
                            batch.Id,
                            now);
                        updated++;
                    }
                }
                else
                {
                    toAdd.Add(row);
                    inserted++;
                }
            }

            if (toAdd.Count > 0) await repository.AddRowsAsync(toAdd, cancellationToken);
            var rejected = readResult.Errors.Count;
            var valid = inserted + updated + unchanged;
            batch.TerminerImport(
                StatutImport.Termine,
                now,
                valid + rejected,
                valid,
                inserted,
                updated,
                unchanged,
                rejected,
                anomalies.Length);
            await repository.SaveChangesAsync(cancellationToken);
            await repository.CommitTransactionAsync(transaction, cancellationToken);
            transaction = null;
            await RecalculerOrigineProcessusAsync(cancellationToken);
            await dependencyRefreshService.RefreshAfterRapportMachineAsync(cancellationToken);
            await ArchiverEtTracerErreurAsync(batch, profile, nomFichierOriginal, bytes, cancellationToken);
            return ToImportResponse(batch, false);
        }
        catch
        {
            if (transaction is not null)
            {
                try { await repository.RollbackTransactionAsync(transaction, cancellationToken); } catch { }
            }

            try { await repository.MarkBatchAsFailedAsync(batch.Id, DateTime.UtcNow, cancellationToken); } catch { }
            throw;
        }
    }

    private async Task ArchiverEtTracerErreurAsync(
        ImportBatch batch,
        string profile,
        string nomFichierOriginal,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        try
        {
            await archiveService.ArchiveAsync(
                batch.Id,
                TypeImport.RapportMachine,
                profile,
                nomFichierOriginal,
                bytes,
                currentUser.UserId,
                currentUser.UserName,
                cancellationToken);
        }
        catch (Exception exception)
        {
            batch.SignalerErreurArchive(exception.Message);
            await repository.SaveChangesAsync(cancellationToken);
        }
    }

    private static ImportRapportMachineResponse ToImportResponse(ImportBatch batch, bool duplicate)
        => new(
            batch.Id,
            duplicate,
            batch.InsertedCount,
            batch.UpdatedCount,
            batch.UnchangedCount,
            batch.RejectedCount,
            batch.ReadCount,
            batch.ValidCount,
            batch.NombreAnomalies,
            batch.Statut.ToString(),
            batch.ArchiveErrorMessage);

    public async Task<PagedResult<RapportMachineResponse>> QueryAsync(string? numeroOF, string? codeArticle, string? codePoste, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await repository.QueryAsync(
            NormalizeOptional(numeroOF),
            NormalizeOptional(codeArticle),
            NormalizeOptional(codePoste),
            page,
            pageSize,
            cancellationToken);
        return new PagedResult<RapportMachineResponse>(result.Items.Select(ToResponse).ToArray(), result.Total);
    }

    public async Task<RapportMachineConsultationResponse> QueryConsultationAsync(
        RapportMachineConsultationQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
            throw Validation(nameof(query.Page), "La page doit être supérieure ou égale à 1.");
        if (query.PageSize is < 1 or > 100)
            throw Validation(nameof(query.PageSize), "La taille de page doit être comprise entre 1 et 100.");
        if (query.DateDebut is not null && query.DateFin is not null && query.DateDebut > query.DateFin)
            throw Validation(nameof(query.DateFin), "La date de fin doit être postérieure ou égale à la date de début.");

        var sortBy = ParseConsultationSort(query.SortBy);
        var descending = ParseDescending(query.SortDirection);
        var result = await repository.QueryConsultationAsync(
            new RapportMachineConsultationFilter(
                query.DateDebut,
                query.DateFin,
                NormalizeOptional(query.NumeroOF),
                NormalizeOptional(query.CodeArticle),
                NormalizeOptional(query.CodePoste),
                ParseProcessus(query.Processus),
                query.ImportBatchId,
                query.Page,
                query.PageSize,
                sortBy,
                descending),
            cancellationToken);

        return new RapportMachineConsultationResponse(
            result.Items.Select(ToConsultationResponse).ToArray(),
            result.Total,
            query.Page,
            query.PageSize,
            new RapportMachineKpiResponse(
                result.Kpis.NombreLignes,
                result.Kpis.NombreOF,
                result.Kpis.Ctrl,
                result.Kpis.Sci,
                result.Kpis.Emr,
                result.Kpis.Trn,
                result.Kpis.Inconnues,
                result.Kpis.LignesRejetees),
            ToTrackingResponse(result.Imports));
    }

    public async Task<ImportTrackingResponse> GetImportTrackingAsync(CancellationToken cancellationToken = default)
        => ToTrackingResponse(await repository.GetImportTrackingAsync(TypeImport.RapportMachine, cancellationToken));

    public async Task<RapportMachineAggregateResponse> AggregateAsync(string numeroOF, string codeArticle, int? annee = null, int? mois = null, CancellationToken cancellationToken = default)
    {
        var normalizedOF = NormalizeOptional(numeroOF) ?? string.Empty;
        var normalizedArticle = NormalizeOptional(codeArticle) ?? string.Empty;
        var rows = annee is not null && mois is not null
            ? await repository.GetByOfArticleAndPeriodeAsync(normalizedOF, normalizedArticle, annee.Value, mois.Value, cancellationToken)
            : await repository.GetByOfAndArticleAsync(normalizedOF, normalizedArticle, cancellationToken);
        var rowsAvecOrigine = rows
            .Select(row => new { Row = row, Origine = ResoudreOrigine(row.CodePoste) })
            .ToArray();

        // Les totaux sont separes par origine process : ne jamais additionner SCI, EMR et TRN ensemble.
        // Les lignes non mappees (OrigineProcessus.Inconnue) sont exclues des totaux mais comptees.
        decimal TotalCtrl(OrigineProcessus origine) => rowsAvecOrigine.Where(item => item.Origine == origine).Sum(item => item.Row.QuantiteTotale);
        decimal TotalRebut(OrigineProcessus origine) => rowsAvecOrigine.Where(item => item.Origine == origine).Sum(item => item.Row.QuantiteRebut);

        return new RapportMachineAggregateResponse(
            normalizedOF,
            normalizedArticle,
            TotalCtrl(OrigineProcessus.Sci),
            TotalCtrl(OrigineProcessus.Emr),
            TotalCtrl(OrigineProcessus.Trn),
            TotalRebut(OrigineProcessus.Sci),
            TotalRebut(OrigineProcessus.Emr),
            TotalRebut(OrigineProcessus.Trn),
            rows.Count,
            rowsAvecOrigine.Count(item => item.Origine == OrigineProcessus.Emr),
            rowsAvecOrigine.Count(item => item.Origine == OrigineProcessus.Inconnue),
            rows.Count > 0 ? rows.Min(row => row.DateProduction) : null,
            rows.Count > 0 ? rows.Max(row => row.DateProduction) : null,
            rows.Count > 0);
    }

    public async Task<RecalculerOrigineProcessusResult> RecalculerOrigineProcessusAsync(CancellationToken cancellationToken = default)
    {
        var rows = await repository.GetAllActiveTrackedAsync(cancellationToken);

        IDisposable? transaction = null;
        try
        {
            transaction = await repository.BeginTransactionAsync(cancellationToken);
            var modifiees = 0;
            var parOrigine = new Dictionary<OrigineProcessus, int>
            {
                [OrigineProcessus.Sci] = 0,
                [OrigineProcessus.Emr] = 0,
                [OrigineProcessus.Trn] = 0,
                [OrigineProcessus.Inconnue] = 0,
                [OrigineProcessus.Ctrl] = 0
            };

            foreach (var row in rows)
            {
                var nouvelleOrigine = ResoudreOrigine(row.CodePoste);
                if (row.OrigineProcessus != nouvelleOrigine)
                {
                    row.RecalculerOrigineProcessus(nouvelleOrigine);
                    modifiees++;
                }

                // Compteurs sur l'etat FINAL, pas seulement sur les lignes modifiees.
                parOrigine[nouvelleOrigine] = parOrigine[nouvelleOrigine] + 1;
            }

            if (modifiees > 0) await repository.SaveChangesAsync(cancellationToken);
            await repository.CommitTransactionAsync(transaction, cancellationToken);
            transaction = null;
            return new RecalculerOrigineProcessusResult(
                rows.Count,
                modifiees,
                rows.Count - modifiees,
                parOrigine[OrigineProcessus.Sci],
                parOrigine[OrigineProcessus.Emr],
                parOrigine[OrigineProcessus.Trn],
                parOrigine[OrigineProcessus.Inconnue],
                parOrigine[OrigineProcessus.Ctrl]);
        }
        catch
        {
            if (transaction is not null)
            {
                try { await repository.RollbackTransactionAsync(transaction, cancellationToken); } catch { }
            }

            throw;
        }
    }

    private static RapportMachineResponse ToResponse(X3RapportMachine row)
        => new(row.Id, row.DateProduction, row.CodePoste, row.OrigineProcessus.ToString(), row.NumeroOperation, row.CodeArticle, row.DesignationArticle, row.NumeroOF, row.Equipe, row.QuantiteTotale, row.QuantiteRebut, row.AlternativeGamme, row.CleMetier, row.IsActive);

    private static RapportMachineConsultationItemResponse ToConsultationResponse(X3RapportMachine row)
        => new(
            row.Id,
            row.DateProduction,
            row.NumeroOF,
            row.CodeArticle,
            row.DesignationArticle,
            row.CodePoste,
            row.NumeroOperation,
            row.Equipe,
            ToProcessusLabel(row.OrigineProcessus),
            row.QuantiteTotale,
            row.QuantiteRebut,
            row.QuantiteTotale - row.QuantiteRebut,
            row.ImportBatchId);

    private static ImportTrackingResponse ToTrackingResponse(ImportTrackingSnapshot tracking)
        => new(
            ToImportExecutionResponse(tracking.DerniereTentative),
            ToImportExecutionResponse(tracking.DernierImportReussi));

    private static ImportExecutionSummaryResponse? ToImportExecutionResponse(ImportBatchSnapshot? snapshot)
    {
        if (snapshot is null) return null;
        var batch = snapshot.Batch;
        var duplicate = batch.Statut == StatutImport.Duplicate;
        return new ImportExecutionSummaryResponse(
            batch.Id,
            ToImportStatusLabel(batch.Statut),
            duplicate
                ? "Ce fichier a déjà été importé. Aucune donnée n’a été réécrite."
                : batch.ArchiveErrorMessage,
            snapshot.NomFichier,
            batch.DateCreationUtc,
            batch.DateFinUtc,
            batch.ReadCount,
            batch.ValidCount,
            batch.RejectedCount,
            batch.InsertedCount,
            batch.UpdatedCount,
            batch.UnchangedCount);
    }

    private static string ToImportStatusLabel(StatutImport statut)
        => statut switch
        {
            StatutImport.EnCours => "En cours",
            StatutImport.Termine => "Importé",
            StatutImport.Partiel => "Partiel",
            StatutImport.Echec => "Échec",
            StatutImport.Duplicate => "Fichier identique",
            StatutImport.TermineAvecAvertissementArchive => "Importé avec avertissement",
            _ => statut.ToString()
        };

    private static string ToProcessusLabel(OrigineProcessus processus)
        => processus switch
        {
            OrigineProcessus.Ctrl => "CTRL",
            OrigineProcessus.Sci => "SCI",
            OrigineProcessus.Emr => "EMR",
            OrigineProcessus.Trn => "TRN",
            _ => "Inconnue"
        };

    private static OrigineProcessus? ParseProcessus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToUpperInvariant() switch
        {
            "CTRL" => OrigineProcessus.Ctrl,
            "SCI" => OrigineProcessus.Sci,
            "EMR" => OrigineProcessus.Emr,
            "TRN" => OrigineProcessus.Trn,
            "INCONNUE" => OrigineProcessus.Inconnue,
            _ => throw Validation(nameof(RapportMachineConsultationQuery.Processus), "Le processus doit être CTRL, SCI, EMR, TRN ou Inconnue.")
        };
    }

    private static string ParseConsultationSort(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? "date";
        return normalized is "date" or "numeroof" or "article" or "poste" or "operation" or
            "processus" or "quantitetotale" or "quantiterebut" or "quantitenette" or "lotimport"
            ? normalized
            : throw Validation(nameof(RapportMachineConsultationQuery.SortBy), "Le champ de tri est invalide.");
    }

    private static bool ParseDescending(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase)) return false;
        throw Validation(nameof(RapportMachineConsultationQuery.SortDirection), "Le sens de tri doit être asc ou desc.");
    }

    private static ValidationException Validation(string propertyName, string message)
        => new([new ValidationFailure(propertyName, message)]);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }
}
