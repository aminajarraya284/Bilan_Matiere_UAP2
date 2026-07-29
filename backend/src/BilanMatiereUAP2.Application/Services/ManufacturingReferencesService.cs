using System.Security.Cryptography;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Imports;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Services;

public sealed class ManufacturingReferencesService : IManufacturingReferencesService
{
    private readonly IManufacturingReferencesRepository _repository;
    private readonly IGammeFabricationReader _gammeReader;
    private readonly INomenclatureReader _nomenclatureReader;
    private readonly IImportArchiveService _archiveService;

    public ManufacturingReferencesService(
        IManufacturingReferencesRepository repository,
        IGammeFabricationReader gammeReader,
        INomenclatureReader nomenclatureReader,
        IImportArchiveService archiveService)
    {
        _repository = repository;
        _gammeReader = gammeReader;
        _nomenclatureReader = nomenclatureReader;
        _archiveService = archiveService;
    }

    public async Task<ImportReferenceResponse> ImportGammesAsync(Stream fileStream, string profilImport, string sourceSystem, string nomFichierOriginal, bool isCompleteExport = false, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAllBytesAsync(fileStream, cancellationToken);
        var hash = ComputeSha256(bytes);
        var existing = await _repository.GetBatchByHashAsync(hash, TypeImport.GammeFabrication, profilImport, cancellationToken);
        if (existing is not null)
        {
            return new ImportReferenceResponse(true, 0, 0);
        }

        var batch = ImportBatch.Demarrer(hash, TypeImport.GammeFabrication, profilImport, null, null, DateTime.UtcNow);

        // Le batch (EnCours) est valide AVANT la transaction metier : la ligne existe donc toujours
        // apres un rollback, ce qui permet de la marquer 'Echec' de facon fiable en cas d'erreur.
        await _repository.AddBatchAsync(batch, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        IDisposable? tx = null;
        try
        {
            tx = await _repository.BeginTransactionAsync(cancellationToken);

            var gammes = DedupParCleMetier(
                (await _gammeReader.ReadAsync(new MemoryStream(bytes), sourceSystem, batch.Id, cancellationToken)).ToList(),
                g => g.CleMetier);
            var keys = gammes.Select(g => g.CleMetier).ToList();

            // Un seul aller-retour (par lots) au lieu d'une requete par ligne (N+1).
            // Entites suivies : les MettreAJour sont donc bien persistees.
            var existingByKey = (await _repository.GetGammesByCleMetiersAsync(keys, cancellationToken))
                .GroupBy(g => g.CleMetier)
                .ToDictionary(group => group.Key, group => group.First());

            var toAdd = new List<RefGammeFabrication>();
            var inserted = 0;
            var updated = 0;
            foreach (var g in gammes)
            {
                if (existingByKey.TryGetValue(g.CleMetier, out var ex))
                {
                    ex.MettreAJour(
                        g.DesignationArticle,
                        g.DateFinValidite,
                        g.EtapeSuivi,
                        g.AlternativeGamme,
                        g.NumeroOperation,
                        g.CodeAtelier,
                        g.CodePoste,
                        g.NombrePostes,
                        g.GroupePosteCharge,
                        g.DesignationOperation,
                        g.UniteOperation,
                        g.CoefficientOperationStock,
                        g.TempsCycle,
                        g.Trs,
                        g.TempsAttente,
                        g.Jalon,
                        g.Quantite,
                        g.Cadence,
                        g.DateModificationX3,
                        g.TempsReglage,
                        g.CoefficientCcpt,
                        sourceSystem,
                        batch.Id,
                        DateTime.UtcNow);
                    ex.Reactiver();
                    updated++;
                }
                else
                {
                    toAdd.Add(g);
                    inserted++;
                }
            }

            if (toAdd.Count > 0) await _repository.AddRefGammesAsync(toAdd, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            // Desactivation logique des lignes absentes UNIQUEMENT sur un export complet.
            if (isCompleteExport)
            {
                await _repository.DisableMissingGammesAsync(sourceSystem, keys, cancellationToken);
            }

            batch.Terminer(StatutImport.Termine, DateTime.UtcNow, gammes.Count, gammes.Count, inserted + updated, 0);
            await _repository.UpdateBatchAsync(batch, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            await _repository.CommitTransactionAsync(tx, cancellationToken);
            tx = null;
            await ArchiverSansEchouerAsync(batch.Id, TypeImport.GammeFabrication, profilImport, nomFichierOriginal, bytes, cancellationToken);
            return new ImportReferenceResponse(false, inserted, updated);
        }
        catch
        {
            await MarquerEchecAsync(tx, batch.Id, cancellationToken);
            throw;
        }
    }

    public async Task<ImportReferenceResponse> ImportNomenclaturesAsync(Stream fileStream, string profilImport, string sourceSystem, string nomFichierOriginal, bool isCompleteExport = false, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAllBytesAsync(fileStream, cancellationToken);
        var hash = ComputeSha256(bytes);
        var existing = await _repository.GetBatchByHashAsync(hash, TypeImport.Nomenclature, profilImport, cancellationToken);
        if (existing is not null)
        {
            return new ImportReferenceResponse(true, 0, 0);
        }

        var batch = ImportBatch.Demarrer(hash, TypeImport.Nomenclature, profilImport, null, null, DateTime.UtcNow);

        await _repository.AddBatchAsync(batch, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        IDisposable? tx = null;
        try
        {
            tx = await _repository.BeginTransactionAsync(cancellationToken);

            var nomenclatures = DedupParCleMetier(
                (await _nomenclatureReader.ReadAsync(new MemoryStream(bytes), sourceSystem, batch.Id, cancellationToken)).ToList(),
                n => n.CleMetier);
            var keys = nomenclatures.Select(n => n.CleMetier).ToList();

            var existingByKey = (await _repository.GetNomenclaturesByCleMetiersAsync(keys, cancellationToken))
                .GroupBy(n => n.CleMetier)
                .ToDictionary(group => group.Key, group => group.First());

            var toAdd = new List<RefNomenclature>();
            var inserted = 0;
            var updated = 0;
            foreach (var n in nomenclatures)
            {
                if (existingByKey.TryGetValue(n.CleMetier, out var ex))
                {
                    ex.MettreAJour(
                        n.AlternativeNomenclature,
                        n.CodeParentNiveau,
                        n.CodeComposant,
                        n.Niveau,
                        n.Sequence,
                        n.DesignationComposant,
                        n.QuantiteLien,
                        n.QuantiteLienCumulee,
                        n.UniteQuantite,
                        n.CategorieComposant,
                        n.TypeLien,
                        n.PourcentageRebut,
                        sourceSystem,
                        batch.Id,
                        DateTime.UtcNow);
                    ex.Reactiver();
                    updated++;
                }
                else
                {
                    toAdd.Add(n);
                    inserted++;
                }
            }

            if (toAdd.Count > 0) await _repository.AddRefNomenclaturesAsync(toAdd, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            if (isCompleteExport)
            {
                await _repository.DisableMissingNomenclaturesAsync(sourceSystem, keys, cancellationToken);
            }

            batch.Terminer(StatutImport.Termine, DateTime.UtcNow, nomenclatures.Count, nomenclatures.Count, inserted + updated, 0);
            await _repository.UpdateBatchAsync(batch, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            await _repository.CommitTransactionAsync(tx, cancellationToken);
            tx = null;
            await ArchiverSansEchouerAsync(batch.Id, TypeImport.Nomenclature, profilImport, nomFichierOriginal, bytes, cancellationToken);
            return new ImportReferenceResponse(false, inserted, updated);
        }
        catch
        {
            await MarquerEchecAsync(tx, batch.Id, cancellationToken);
            throw;
        }
    }

    // Best-effort : l'archivage du fichier source ne doit jamais faire echouer un import metier
    // deja reussi (donnees deja persistees). Une erreur d'archivage est absorbee ici.
    private async Task ArchiverSansEchouerAsync(Guid batchId, TypeImport typeImport, string profilImport, string nomFichierOriginal, byte[] content, CancellationToken cancellationToken)
    {
        try
        {
            await _archiveService.ArchiveAsync(batchId, typeImport, profilImport, nomFichierOriginal, content, null, null, cancellationToken);
        }
        catch
        {
            // best-effort : ne pas masquer le succes de l'import metier
        }
    }

    // Annule la transaction metier puis marque le batch en echec via une mise a jour SQL directe.
    // Toute erreur lors du marquage est absorbee pour ne pas masquer l'exception d'origine (re-lancee par l'appelant).
    private async Task MarquerEchecAsync(IDisposable? tx, Guid batchId, CancellationToken cancellationToken)
    {
        if (tx is not null)
        {
            try { await _repository.RollbackTransactionAsync(tx, cancellationToken); }
            catch { /* la transaction peut deja etre invalide */ }
        }

        try { await _repository.MarkBatchAsEchecAsync(batchId, DateTime.UtcNow, cancellationToken); }
        catch { /* best-effort : ne pas masquer l'exception d'origine */ }
    }

    // Conserve la derniere occurrence de chaque CleMetier (une ligne plus recente du fichier prime),
    // garantissant des compteurs inserted/updated exacts et un upsert deterministe.
    private static List<T> DedupParCleMetier<T>(IReadOnlyList<T> items, Func<T, string> keySelector)
        => items
            .GroupBy(keySelector)
            .Select(group => group.Last())
            .ToList();

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToUpperInvariant();
    }
}
