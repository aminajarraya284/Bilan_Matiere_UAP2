namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record StoredFileContent(Stream Content, string MimeType, string NomFichierOriginal);

public interface IImportFileStorage
{
    // Ecrit le contenu dans un fichier temporaire, verifie l'absence d'ecrasement, puis deplace vers
    // le stockage definitif Storage/Imports/{annee}/{mois}/{batchId}/{fileId}-{nomSanitize}.
    // A appeler uniquement apres succes complet de l'import (batch cree et transaction validee) :
    // en cas d'echec avant cet appel, aucun fichier n'est laisse sur le disque definitif.
    Task<string> SaveAsync(
        Guid batchId,
        Guid fileId,
        string nomFichierOriginal,
        byte[] content,
        DateTime dateUtc,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string cheminStockage, CancellationToken cancellationToken = default);
}
