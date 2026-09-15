using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class X3OrdreFabrication
{
    private X3OrdreFabrication()
    {
    }

    private X3OrdreFabrication(
        Guid id,
        string numeroOF,
        Guid dernierImportBatchId,
        DateTime dateCreationUtc)
    {
        Id = id;
        NumeroOF = NormaliserNumeroOF(numeroOF);
        DernierImportBatchId = dernierImportBatchId;
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public string NumeroOF { get; private set; } = string.Empty;
    public string? ArticlePrincipal { get; private set; }
    public string? DesignationPrincipale { get; private set; }
    public string? SituationOF { get; private set; }
    public string? Atelier { get; private set; }
    public DateOnly? DateObjectifOF { get; private set; }
    public string? StatutX3 { get; private set; }
    public string StatutArticle { get; private set; } = "ArticleIntrouvable";
    public Guid DernierImportBatchId { get; private set; }
    public ImportX3Batch DernierImportBatch { get; private set; } = null!;
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateModificationUtc { get; private set; }
    public ICollection<X3OperationOF> Operations { get; private set; } = new List<X3OperationOF>();
    public ICollection<X3MouvementStock> Mouvements { get; private set; } = new List<X3MouvementStock>();
    public ICollection<ConsommationComposant> Consommations { get; private set; } = new List<ConsommationComposant>();

    public static X3OrdreFabrication Creer(
        string numeroOF,
        Guid dernierImportBatchId,
        DateTime dateCreationUtc,
        Guid? id = null)
    {
        if (dernierImportBatchId == Guid.Empty)
        {
            throw new DomainValidationException("L'import de l'ordre de fabrication est obligatoire.");
        }

        return new X3OrdreFabrication(id ?? Guid.NewGuid(), numeroOF, dernierImportBatchId, dateCreationUtc);
    }

    public void MettreAJourContexte(
        string? articlePrincipal,
        string? designationPrincipale,
        string? situationOF,
        string? atelier,
        DateOnly? dateObjectifOF,
        string? statutX3,
        Guid dernierImportBatchId,
        DateTime dateModificationUtc,
        string statutArticle = "Trouve")
    {
        ArticlePrincipal = ImportX3Batch.NormaliserTexte(articlePrincipal, 80);
        DesignationPrincipale = ImportX3Batch.NormaliserTexte(designationPrincipale, 250);
        SituationOF = ImportX3Batch.NormaliserTexte(situationOF, 80);
        Atelier = ImportX3Batch.NormaliserTexte(atelier, 30);
        DateObjectifOF = dateObjectifOF;
        StatutX3 = ImportX3Batch.NormaliserTexte(statutX3, 80);
        StatutArticle = ImportX3Batch.NormaliserTexte(statutArticle, 50) ?? "ArticleIntrouvable";
        DernierImportBatchId = dernierImportBatchId;
        DateModificationUtc = dateModificationUtc;
    }

    public X3OperationOF AjouterOuMettreAJourOperation(
        int numeroLigneSource,
        string? numeroOperation,
        string? codePoste,
        string? typePoste,
        string? numeroPoste,
        string? effectifPoste,
        string? cad,
        string? effectifOperation,
        decimal? quantitePrevue,
        decimal? quantiteRestante,
        DateOnly? dateOperation,
        Guid importBatchId,
        DateTime now)
    {
        var operationNumber = NormaliserCleOperation(numeroOperation);
        var workCenter = NormaliserCleOperation(codePoste);
        var existing = Operations.SingleOrDefault(item =>
            item.NumeroOperation == operationNumber && item.CodePoste == workCenter);

        if (existing is null)
        {
            existing = X3OperationOF.Creer(
                Id,
                numeroLigneSource,
                operationNumber,
                workCenter,
                typePoste,
                numeroPoste,
                effectifPoste,
                cad,
                effectifOperation,
                quantitePrevue,
                quantiteRestante,
                dateOperation,
                importBatchId,
                now);
            Operations.Add(existing);
        }
        else
        {
            existing.MettreAJour(
                numeroLigneSource,
                typePoste,
                numeroPoste,
                effectifPoste,
                cad,
                effectifOperation,
                quantitePrevue,
                quantiteRestante,
                dateOperation,
                importBatchId,
                now);
        }

        return existing;
    }

    public static string NormaliserNumeroOF(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant().Replace(" ", string.Empty) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainValidationException("Le numero d'OF est obligatoire.");
        }

        if (normalized.Length > 50)
        {
            throw new DomainValidationException("Le numero d'OF ne peut pas depasser 50 caracteres.");
        }

        return normalized;
    }

    private static string NormaliserCleOperation(string? value)
    {
        return value?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
