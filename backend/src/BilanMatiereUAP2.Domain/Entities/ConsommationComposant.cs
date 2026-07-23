using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class ConsommationComposant
{
    private ConsommationComposant()
    {
    }

    private ConsommationComposant(
        Guid id,
        Guid importBatchId,
        DateOnly dateImputation,
        X3OrdreFabrication ordre,
        Composant composant,
        string unite,
        decimal sommeQuantiteX3Signee,
        DateTime dateCreationUtc)
    {
        if (importBatchId == Guid.Empty)
        {
            throw new DomainValidationException("L'import associe a la consommation est obligatoire.");
        }

        Id = id;
        ImportBatchId = importBatchId;
        DateImputation = dateImputation;
        OrdreFabricationId = ordre.Id;
        NumeroOF = ordre.NumeroOF;
        ArticlePrincipal = ordre.ArticlePrincipal
            ?? throw new DomainValidationException("L'article principal de l'OF est obligatoire.");
        DesignationPrincipale = ordre.DesignationPrincipale;
        ComposantId = composant.Id;
        CodeComposant = composant.Code;
        DesignationComposant = composant.Designation;
        Unite = ImportX3Batch.NormaliserTexte(unite, 20)
            ?? throw new DomainValidationException("L'unite du mouvement est obligatoire.");
        SommeQuantiteX3Signee = sommeQuantiteX3Signee;
        Consommation = -sommeQuantiteX3Signee;
        Statut = StatutRapprochementX3.Rapproche;
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public Guid ImportBatchId { get; private set; }
    public ImportX3Batch ImportBatch { get; private set; } = null!;
    public DateOnly DateImputation { get; private set; }
    public Guid OrdreFabricationId { get; private set; }
    public X3OrdreFabrication OrdreFabrication { get; private set; } = null!;
    public string NumeroOF { get; private set; } = string.Empty;
    public string ArticlePrincipal { get; private set; } = string.Empty;
    public string? DesignationPrincipale { get; private set; }
    public Guid ComposantId { get; private set; }
    public Composant Composant { get; private set; } = null!;
    public string CodeComposant { get; private set; } = string.Empty;
    public string? DesignationComposant { get; private set; }
    public string Unite { get; private set; } = string.Empty;
    public decimal SommeQuantiteX3Signee { get; private set; }
    public decimal Consommation { get; private set; }
    public StatutRapprochementX3 Statut { get; private set; }
    public DateTime DateCreationUtc { get; private set; }

    public static ConsommationComposant Creer(
        Guid importBatchId,
        DateOnly dateImputation,
        X3OrdreFabrication ordre,
        Composant composant,
        string unite,
        decimal sommeQuantiteX3Signee,
        DateTime dateCreationUtc,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), importBatchId, dateImputation, ordre, composant, unite, sommeQuantiteX3Signee, dateCreationUtc);
}
