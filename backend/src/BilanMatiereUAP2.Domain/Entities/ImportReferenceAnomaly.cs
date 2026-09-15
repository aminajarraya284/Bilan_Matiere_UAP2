using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class ImportReferenceAnomaly
{
    private ImportReferenceAnomaly() { }

    private ImportReferenceAnomaly(
        Guid id,
        Guid importBatchId,
        TypeImport typeImport,
        int numeroLigneSource,
        string? nomFeuilleSource,
        string message,
        DateTime dateCreationUtc)
    {
        Id = id;
        ImportBatchId = importBatchId;
        TypeImport = typeImport;
        NumeroLigneSource = numeroLigneSource;
        NomFeuilleSource = nomFeuilleSource?.Trim();
        Message = message.Trim();
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public Guid ImportBatchId { get; private set; }
    public TypeImport TypeImport { get; private set; }
    public int NumeroLigneSource { get; private set; }
    public string? NomFeuilleSource { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime DateCreationUtc { get; private set; }

    public static ImportReferenceAnomaly Creer(
        Guid importBatchId,
        TypeImport typeImport,
        int numeroLigneSource,
        string? nomFeuilleSource,
        string message,
        DateTime dateCreationUtc)
        => new(Guid.NewGuid(), importBatchId, typeImport, numeroLigneSource, nomFeuilleSource, message, dateCreationUtc);
}
