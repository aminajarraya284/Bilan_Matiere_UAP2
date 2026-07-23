using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class X3MouvementStock
{
    private X3MouvementStock()
    {
    }

    private X3MouvementStock(
        Guid id,
        Guid importBatchId,
        int numeroLigneSource,
        string? nomFeuilleSource,
        DateOnly dateImputation,
        string? code,
        string? designation,
        DateOnly? dateCreationX3,
        string? heureCreationX3,
        string? utilisateur,
        decimal quantiteX3Signee,
        string? unite,
        string? transaction,
        string? typePiece,
        string? lot,
        string? pieceOrigine,
        string numeroOFNormalise,
        string? designationMouvement,
        string? emplacement,
        string? statutX3,
        DateTime dateCreationUtc)
    {
        if (importBatchId == Guid.Empty || numeroLigneSource <= 0)
        {
            throw new DomainValidationException("Les references du mouvement X3 sont invalides.");
        }

        Id = id;
        ImportBatchId = importBatchId;
        NumeroLigneSource = numeroLigneSource;
        NomFeuilleSource = ImportX3Batch.NormaliserTexte(nomFeuilleSource, 128);
        DateImputation = dateImputation;
        Code = ImportX3Batch.NormaliserTexte(code, 80);
        Designation = ImportX3Batch.NormaliserTexte(designation, 250);
        DateCreationX3 = dateCreationX3;
        HeureCreationX3 = ImportX3Batch.NormaliserTexte(heureCreationX3, 30);
        Utilisateur = ImportX3Batch.NormaliserTexte(utilisateur, 100);
        QuantiteX3Signee = quantiteX3Signee;
        Unite = ImportX3Batch.NormaliserTexte(unite, 20);
        Transaction = ImportX3Batch.NormaliserTexte(transaction, 80);
        TypePiece = ImportX3Batch.NormaliserTexte(typePiece, 80);
        Lot = ImportX3Batch.NormaliserTexte(lot, 80);
        PieceOrigine = ImportX3Batch.NormaliserTexte(pieceOrigine, 100);
        NumeroOFNormalise = numeroOFNormalise;
        DesignationMouvement = ImportX3Batch.NormaliserTexte(designationMouvement, 250);
        Emplacement = ImportX3Batch.NormaliserTexte(emplacement, 80);
        StatutX3 = ImportX3Batch.NormaliserTexte(statutX3, 80);
        StatutRapprochement = StatutRapprochementX3.MouvementInvalide;
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public Guid ImportBatchId { get; private set; }
    public ImportX3Batch ImportBatch { get; private set; } = null!;
    public int NumeroLigneSource { get; private set; }
    public string? NomFeuilleSource { get; private set; }
    public DateOnly DateImputation { get; private set; }
    public string? Code { get; private set; }
    public string? Designation { get; private set; }
    public DateOnly? DateCreationX3 { get; private set; }
    public string? HeureCreationX3 { get; private set; }
    public string? Utilisateur { get; private set; }
    public decimal QuantiteX3Signee { get; private set; }
    public string? Unite { get; private set; }
    public string? Transaction { get; private set; }
    public string? TypePiece { get; private set; }
    public string? Lot { get; private set; }
    public string? PieceOrigine { get; private set; }
    public string NumeroOFNormalise { get; private set; } = string.Empty;
    public string? DesignationMouvement { get; private set; }
    public string? Emplacement { get; private set; }
    public string? StatutX3 { get; private set; }
    public Guid? OrdreFabricationId { get; private set; }
    public X3OrdreFabrication? OrdreFabrication { get; private set; }
    public Guid? ComposantId { get; private set; }
    public Composant? Composant { get; private set; }
    public StatutRapprochementX3 StatutRapprochement { get; private set; }
    public DateTime DateCreationUtc { get; private set; }

    public static X3MouvementStock Creer(
        Guid importBatchId,
        int numeroLigneSource,
        string? nomFeuilleSource,
        DateOnly dateImputation,
        string? code,
        string? designation,
        DateOnly? dateCreationX3,
        string? heureCreationX3,
        string? utilisateur,
        decimal quantiteX3Signee,
        string? unite,
        string? transaction,
        string? typePiece,
        string? lot,
        string? pieceOrigine,
        string numeroOFNormalise,
        string? designationMouvement,
        string? emplacement,
        string? statutX3,
        DateTime dateCreationUtc,
        Guid? id = null)
    {
        return new X3MouvementStock(
            id ?? Guid.NewGuid(),
            importBatchId,
            numeroLigneSource,
            nomFeuilleSource,
            dateImputation,
            code,
            designation,
            dateCreationX3,
            heureCreationX3,
            utilisateur,
            quantiteX3Signee,
            unite,
            transaction,
            typePiece,
            lot,
            pieceOrigine,
            numeroOFNormalise,
            designationMouvement,
            emplacement,
            statutX3,
            dateCreationUtc);
    }

    public void DefinirRapprochement(
        Guid? ordreFabricationId,
        Guid? composantId,
        StatutRapprochementX3 statut)
    {
        OrdreFabricationId = ordreFabricationId;
        ComposantId = composantId;
        StatutRapprochement = statut;
    }
}
