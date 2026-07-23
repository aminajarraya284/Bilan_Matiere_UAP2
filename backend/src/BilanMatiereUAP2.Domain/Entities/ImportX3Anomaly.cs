using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class ImportX3Anomaly
{
    private ImportX3Anomaly()
    {
    }

    private ImportX3Anomaly(
        Guid id,
        Guid importBatchId,
        TypeFichierImportX3 typeFichier,
        int? numeroLigneSource,
        StatutRapprochementX3 statut,
        string message,
        string? numeroOF,
        string? codeTypeMatiere,
        string? nomFeuilleSource,
        DateTime dateCreationUtc)
    {
        if (importBatchId == Guid.Empty)
        {
            throw new DomainValidationException("L'import associe a l'anomalie est obligatoire.");
        }

        if (numeroLigneSource <= 0)
        {
            throw new DomainValidationException("Le numero de ligne source est invalide.");
        }

        Id = id;
        ImportBatchId = importBatchId;
        TypeFichier = typeFichier;
        NumeroLigneSource = numeroLigneSource;
        Statut = statut;
        Message = ImportX3Batch.NormaliserTexte(message, 1000)
            ?? throw new DomainValidationException("Le message d'anomalie est obligatoire.");
        NumeroOF = ImportX3Batch.NormaliserTexte(numeroOF, 50);
        CodeTypeMatiere = ImportX3Batch.NormaliserTexte(codeTypeMatiere, 80);
        NomFeuilleSource = ImportX3Batch.NormaliserTexte(nomFeuilleSource, 128);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public Guid ImportBatchId { get; private set; }
    public ImportX3Batch ImportBatch { get; private set; } = null!;
    public TypeFichierImportX3 TypeFichier { get; private set; }
    public int? NumeroLigneSource { get; private set; }
    public StatutRapprochementX3 Statut { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? NumeroOF { get; private set; }
    public string? CodeTypeMatiere { get; private set; }
    public string? NomFeuilleSource { get; private set; }
    public DateTime DateCreationUtc { get; private set; }

    public static ImportX3Anomaly Creer(
        Guid importBatchId,
        TypeFichierImportX3 typeFichier,
        int? numeroLigneSource,
        StatutRapprochementX3 statut,
        string message,
        string? numeroOF,
        string? codeTypeMatiere,
        DateTime dateCreationUtc,
        string? nomFeuilleSource = null,
        Guid? id = null)
    {
        return new ImportX3Anomaly(
            id ?? Guid.NewGuid(),
            importBatchId,
            typeFichier,
            numeroLigneSource,
            statut,
            message,
            numeroOF,
            codeTypeMatiere,
            nomFeuilleSource,
            dateCreationUtc);
    }
}
