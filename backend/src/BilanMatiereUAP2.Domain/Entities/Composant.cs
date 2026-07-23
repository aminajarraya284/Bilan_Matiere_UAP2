using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class Composant
{
    private Composant()
    {
    }

    private Composant(Guid id, string code, string? designation, DateTime dateCreationUtc)
    {
        Id = id;
        Code = ImportX3Batch.NormaliserTexte(code, 80)
            ?? throw new DomainValidationException("Le code composant est obligatoire.");
        Designation = ImportX3Batch.NormaliserTexte(designation, 250);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string? Designation { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateModificationUtc { get; private set; }
    public ICollection<X3MouvementStock> Mouvements { get; private set; } = new List<X3MouvementStock>();
    public ICollection<ConsommationComposant> Consommations { get; private set; } = new List<ConsommationComposant>();

    public static Composant Creer(string code, string? designation, DateTime dateCreationUtc, Guid? id = null)
        => new(id ?? Guid.NewGuid(), code, designation, dateCreationUtc);

    public void MettreAJourDesignation(string? designation, DateTime dateModificationUtc)
    {
        var normalized = ImportX3Batch.NormaliserTexte(designation, 250);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            Designation = normalized;
        }

        DateModificationUtc = dateModificationUtc;
    }
}
