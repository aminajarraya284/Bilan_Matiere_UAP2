using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class SortieMatiereOF
{
    private SortieMatiereOF()
    {
    }

    private SortieMatiereOF(
        Guid id,
        Guid importBatchId,
        DateOnly dateImputation,
        Guid ordreFabricationId,
        string numeroOF,
        string articlePrincipal,
        string? designationPrincipale,
        Guid typeMatiereId,
        string codeTypeMatiere,
        decimal sommeQuantiteX3Signee,
        StatutRapprochementX3 statut,
        DateTime dateCreationUtc)
    {
        if (importBatchId == Guid.Empty || ordreFabricationId == Guid.Empty || typeMatiereId == Guid.Empty)
        {
            throw new DomainValidationException("Les references de la sortie matiere sont invalides.");
        }

        Id = id;
        ImportBatchId = importBatchId;
        DateImputation = dateImputation;
        OrdreFabricationId = ordreFabricationId;
        NumeroOF = X3OrdreFabrication.NormaliserNumeroOF(numeroOF);
        ArticlePrincipal = ImportX3Batch.NormaliserTexte(articlePrincipal, 80)
            ?? throw new DomainValidationException("L'article principal de l'OF est obligatoire.");
        DesignationPrincipale = ImportX3Batch.NormaliserTexte(designationPrincipale, 250);
        TypeMatiereId = typeMatiereId;
        CodeTypeMatiere = ImportX3Batch.NormaliserTexte(codeTypeMatiere, 30)
            ?? throw new DomainValidationException("Le code du type matiere est obligatoire.");
        SommeQuantiteX3Signee = sommeQuantiteX3Signee;
        SortieMpKg = -sommeQuantiteX3Signee;
        Statut = statut;
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
    public Guid TypeMatiereId { get; private set; }
    public TypeMatiere TypeMatiere { get; private set; } = null!;
    public string CodeTypeMatiere { get; private set; } = string.Empty;
    public decimal SommeQuantiteX3Signee { get; private set; }
    public decimal SortieMpKg { get; private set; }
    public StatutRapprochementX3 Statut { get; private set; }
    public DateTime DateCreationUtc { get; private set; }

    public static SortieMatiereOF Creer(
        Guid importBatchId,
        DateOnly dateImputation,
        Guid ordreFabricationId,
        string numeroOF,
        string articlePrincipal,
        string? designationPrincipale,
        Guid typeMatiereId,
        string codeTypeMatiere,
        decimal sommeQuantiteX3Signee,
        DateTime dateCreationUtc,
        Guid? id = null)
    {
        return new SortieMatiereOF(
            id ?? Guid.NewGuid(),
            importBatchId,
            dateImputation,
            ordreFabricationId,
            numeroOF,
            articlePrincipal,
            designationPrincipale,
            typeMatiereId,
            codeTypeMatiere,
            sommeQuantiteX3Signee,
            StatutRapprochementX3.Rapproche,
            dateCreationUtc);
    }
}
