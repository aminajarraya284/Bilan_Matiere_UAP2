using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.Imports;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Features.Imports;

public interface IImportArchiveService
{
    // Archive le fichier source. L'appelant doit persister explicitement toute erreur
    // d'archivage sur le lot sans annuler les donnees metier deja validees.
    Task ArchiveAsync(
        Guid importBatchId,
        TypeImport typeImport,
        string? profilImport,
        string nomFichierOriginal,
        byte[] content,
        Guid? uploadeParUtilisateurId,
        string? uploadeParNom,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportSourceFileResponse>> GetFilesAsync(Guid importBatchId, CancellationToken cancellationToken = default);

    Task<StoredFileContent> DownloadAsync(Guid importBatchId, Guid fileId, CancellationToken cancellationToken = default);
}

public sealed class ImportArchiveService(
    IImportSourceFileRepository repository,
    IImportFileStorage storage,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IImportHashCalculator hashCalculator) : IImportArchiveService
{
    private static readonly IReadOnlyDictionary<string, string> MimeTypesByExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    public async Task ArchiveAsync(
        Guid importBatchId,
        TypeImport typeImport,
        string? profilImport,
        string nomFichierOriginal,
        byte[] content,
        Guid? uploadeParUtilisateurId,
        string? uploadeParNom,
        CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow;
        var fileId = Guid.NewGuid();
        var extension = Path.GetExtension(nomFichierOriginal);
        var hash = hashCalculator.CalculateSha256(content);
        var cheminStockage = await storage.SaveAsync(importBatchId, fileId, nomFichierOriginal, content, now, cancellationToken);

        var entity = ImportSourceFile.Creer(
            importBatchId,
            typeImport,
            profilImport,
            nomFichierOriginal,
            extension,
            MimeTypesByExtension.GetValueOrDefault(extension, "application/octet-stream"),
            content.Length,
            hash,
            cheminStockage,
            now,
            uploadeParUtilisateurId,
            uploadeParNom,
            fileId);

        await repository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportSourceFileResponse>> GetFilesAsync(Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var files = await repository.GetByBatchIdAsync(importBatchId, cancellationToken);
        return files.Select(ToResponse).ToArray();
    }

    public async Task<StoredFileContent> DownloadAsync(Guid importBatchId, Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await repository.GetByIdAsync(fileId, cancellationToken);
        if (file is null || file.ImportBatchId != importBatchId)
        {
            throw new NotFoundException("Fichier source introuvable pour cet import.");
        }

        var stream = await storage.OpenReadAsync(file.CheminStockage, cancellationToken)
            ?? throw new NotFoundException("Le fichier archivé n'est plus disponible sur le stockage.");
        return new StoredFileContent(stream, file.MimeType, file.NomFichierOriginal);
    }

    private static ImportSourceFileResponse ToResponse(ImportSourceFile entity)
        => new(
            entity.Id,
            entity.ImportBatchId,
            entity.TypeImport.ToString(),
            entity.ProfilImport,
            entity.NomFichierOriginal,
            entity.Extension,
            entity.TailleOctets,
            entity.HashSha256,
            entity.DateUploadUtc,
            entity.UploadeParNom);
}
