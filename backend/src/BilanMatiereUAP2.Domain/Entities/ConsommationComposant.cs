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
        string codeTypeMatiere,
        string? designationComposant,
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
        CodeTypeMatiere = ImportX3Batch.NormaliserTexte(codeTypeMatiere, 80)
            ?? throw new DomainValidationException("Le code type matiere est obligatoire.");
        DesignationComposant = ImportX3Batch.NormaliserTexte(designationComposant, 250);
        Unite = ImportX3Batch.NormaliserTexte(unite, 20)
            ?? throw new DomainValidationException("L'unite du mouvement est obligatoire.");
        CleMetier = GenererCleMetier(NumeroOF, CodeTypeMatiere, Unite, DateImputation);
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
    public string CodeTypeMatiere { get; private set; } = string.Empty;
    public string? DesignationComposant { get; private set; }
    public string Unite { get; private set; } = string.Empty;
    public string CleMetier { get; private set; } = string.Empty;
    public decimal SommeQuantiteX3Signee { get; private set; }
    public decimal Consommation { get; private set; }
    public StatutRapprochementX3 Statut { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateMiseAJourUtc { get; private set; }

    public static ConsommationComposant Creer(
        Guid importBatchId,
        DateOnly dateImputation,
        X3OrdreFabrication ordre,
        string codeTypeMatiere,
        string? designationComposant,
        string unite,
        decimal sommeQuantiteX3Signee,
        DateTime dateCreationUtc,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), importBatchId, dateImputation, ordre, codeTypeMatiere, designationComposant, unite, sommeQuantiteX3Signee, dateCreationUtc);

    public void MettreAJour(
        X3OrdreFabrication ordre,
        string? designationComposant,
        decimal sommeQuantiteX3Signee,
        Guid importBatchId,
        DateTime dateMiseAJourUtc)
    {
        OrdreFabricationId = ordre.Id;
        ArticlePrincipal = ordre.ArticlePrincipal ?? ArticlePrincipal;
        DesignationPrincipale = ordre.DesignationPrincipale;
        DesignationComposant = ImportX3Batch.NormaliserTexte(designationComposant, 250);
        SommeQuantiteX3Signee = sommeQuantiteX3Signee;
        Consommation = -sommeQuantiteX3Signee;
        ImportBatchId = importBatchId;
        DateMiseAJourUtc = dateMiseAJourUtc;
    }

    public static string GenererCleMetier(
        string numeroOF, string codeTypeMatiere, string unite, DateOnly dateImputation)
    {
        return $"{numeroOF.ToUpperInvariant()}|{codeTypeMatiere.ToUpperInvariant()}|{unite.ToUpperInvariant()}|{dateImputation:yyyy-MM-dd}";
    }
}
