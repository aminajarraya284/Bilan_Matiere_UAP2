using BilanMatiereUAP2.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace BilanMatiereUAP2.Infrastructure.Services;

public sealed class LocalImportFileStorage : IImportFileStorage
{
    private readonly string _rootPath;

    public LocalImportFileStorage(IConfiguration configuration)
    {
        _rootPath = configuration["ImportFileStorage:RootPath"]?.Trim() is { Length: > 0 } configured
            ? configured
            : Path.Combine(AppContext.BaseDirectory, "Storage", "Imports");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Guid batchId,
        Guid fileId,
        string nomFichierOriginal,
        byte[] content,
        DateTime dateUtc,
        CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"import-staging-{fileId:N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(tempPath, content, cancellationToken);

            var relativeDirectory = Path.Combine(
                dateUtc.Year.ToString("D4"),
                dateUtc.Month.ToString("D2"),
                batchId.ToString("D"));
            var targetDirectory = Path.Combine(_rootPath, relativeDirectory);
            Directory.CreateDirectory(targetDirectory);

            var safeName = SanitizeFileName(nomFichierOriginal);
            var targetFileName = $"{fileId:D}-{safeName}";
            var targetPath = Path.Combine(targetDirectory, targetFileName);

            if (File.Exists(targetPath))
            {
                throw new InvalidOperationException($"Un fichier archive existe deja a cet emplacement : {targetFileName}.");
            }

            File.Move(tempPath, targetPath);
            return Path.Combine(relativeDirectory, targetFileName);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public Task<Stream?> OpenReadAsync(string cheminStockage, CancellationToken cancellationToken = default)
    {
        // Le chemin stocke est toujours relatif et genere par SaveAsync (jamais construit a partir
        // d'une entree utilisateur) : Path.GetFullPath + verification de prefixe empeche neanmoins
        // toute traversee de repertoire si la valeur venait a etre alteree.
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, cheminStockage));
        var rootFullPath = Path.GetFullPath(_rootPath);
        if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    private static string SanitizeFileName(string original)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(original.Select(c => invalidChars.Contains(c) || c is '/' or '\\' ? '_' : c).ToArray());
        sanitized = sanitized.Trim().Trim('.');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "fichier";
        }

        return sanitized.Length > 200 ? sanitized[..200] : sanitized;
    }
}
