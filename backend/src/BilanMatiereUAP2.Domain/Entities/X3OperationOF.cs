using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class X3OperationOF
{
    private X3OperationOF()
    {
    }

    private X3OperationOF(
        Guid id,
        Guid ordreFabricationId,
        int numeroLigneSource,
        string numeroOperation,
        string codePoste,
        string? typePoste,
        string? numeroPoste,
        string? effectifPoste,
        string? cad,
        string? effectifOperation,
        decimal? quantitePrevue,
        decimal? quantiteRestante,
        DateOnly? dateOperation,
        Guid importBatchId,
        DateTime dateCreationUtc)
    {
        if (ordreFabricationId == Guid.Empty || importBatchId == Guid.Empty || numeroLigneSource <= 0)
        {
            throw new DomainValidationException("Les references de l'operation X3 sont invalides.");
        }

        Id = id;
        OrdreFabricationId = ordreFabricationId;
        NumeroOperation = NormaliserCodeOperation(numeroOperation, 30);
        CodePoste = NormaliserCodeOperation(codePoste, 50);
        MettreAJourValeurs(
            numeroLigneSource,
            typePoste,
            numeroPoste,
            effectifPoste,
            cad,
            effectifOperation,
            quantitePrevue,
            quantiteRestante,
            dateOperation,
            importBatchId);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrdreFabricationId { get; private set; }
    public X3OrdreFabrication OrdreFabrication { get; private set; } = null!;
    public int NumeroLigneSource { get; private set; }
    public string NumeroOperation { get; private set; } = string.Empty;
    public string CodePoste { get; private set; } = string.Empty;
    public string? TypePoste { get; private set; }
    public string? NumeroPoste { get; private set; }
    public string? EffectifPoste { get; private set; }
    public string? Cad { get; private set; }
    public string? EffectifOperation { get; private set; }
    public decimal? QuantitePrevue { get; private set; }
    public decimal? QuantiteRestante { get; private set; }
    public DateOnly? DateOperation { get; private set; }
    public Guid ImportBatchId { get; private set; }
    public ImportX3Batch ImportBatch { get; private set; } = null!;
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateModificationUtc { get; private set; }

    internal static X3OperationOF Creer(
        Guid ordreFabricationId,
        int numeroLigneSource,
        string numeroOperation,
        string codePoste,
        string? typePoste,
        string? numeroPoste,
        string? effectifPoste,
        string? cad,
        string? effectifOperation,
        decimal? quantitePrevue,
        decimal? quantiteRestante,
        DateOnly? dateOperation,
        Guid importBatchId,
        DateTime dateCreationUtc,
        Guid? id = null)
    {
        return new X3OperationOF(
            id ?? Guid.NewGuid(),
            ordreFabricationId,
            numeroLigneSource,
            numeroOperation,
            codePoste,
            typePoste,
            numeroPoste,
            effectifPoste,
            cad,
            effectifOperation,
            quantitePrevue,
            quantiteRestante,
            dateOperation,
            importBatchId,
            dateCreationUtc);
    }

    internal void MettreAJour(
        int numeroLigneSource,
        string? typePoste,
        string? numeroPoste,
        string? effectifPoste,
        string? cad,
        string? effectifOperation,
        decimal? quantitePrevue,
        decimal? quantiteRestante,
        DateOnly? dateOperation,
        Guid importBatchId,
        DateTime dateModificationUtc)
    {
        MettreAJourValeurs(
            numeroLigneSource,
            typePoste,
            numeroPoste,
            effectifPoste,
            cad,
            effectifOperation,
            quantitePrevue,
            quantiteRestante,
            dateOperation,
            importBatchId);
        DateModificationUtc = dateModificationUtc;
    }

    private void MettreAJourValeurs(
        int numeroLigneSource,
        string? typePoste,
        string? numeroPoste,
        string? effectifPoste,
        string? cad,
        string? effectifOperation,
        decimal? quantitePrevue,
        decimal? quantiteRestante,
        DateOnly? dateOperation,
        Guid importBatchId)
    {
        NumeroLigneSource = numeroLigneSource;
        TypePoste = ImportX3Batch.NormaliserTexte(typePoste, 30);
        NumeroPoste = ImportX3Batch.NormaliserTexte(numeroPoste, 30);
        EffectifPoste = ImportX3Batch.NormaliserTexte(effectifPoste, 30);
        Cad = ImportX3Batch.NormaliserTexte(cad, 30);
        EffectifOperation = ImportX3Batch.NormaliserTexte(effectifOperation, 30);
        QuantitePrevue = quantitePrevue;
        QuantiteRestante = quantiteRestante;
        DateOperation = dateOperation;
        ImportBatchId = importBatchId;
    }

    private static string NormaliserCodeOperation(string value, int maxLength)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length > maxLength)
        {
            throw new DomainValidationException($"La cle d'operation ne peut pas depasser {maxLength} caracteres.");
        }

        return normalized;
    }
}
