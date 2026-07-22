using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class TypeMatiere
{
    private TypeMatiere()
    {
    }

    private TypeMatiere(Guid id, string code, string libelle, DateTime dateCreationUtc)
    {
        Id = id;
        Code = NormaliserCode(code);
        Libelle = NormaliserLibelle(libelle);
        EstActif = true;
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Libelle { get; private set; } = string.Empty;
    public bool EstActif { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateModificationUtc { get; private set; }

    public static TypeMatiere Creer(string code, string libelle, DateTime dateCreationUtc, Guid? id = null)
    {
        return new TypeMatiere(id ?? Guid.NewGuid(), code, libelle, dateCreationUtc);
    }

    public void Modifier(string libelle, DateTime dateModificationUtc)
    {
        Libelle = NormaliserLibelle(libelle);
        DateModificationUtc = dateModificationUtc;
    }

    public void Activer(DateTime dateModificationUtc)
    {
        if (EstActif)
        {
            return;
        }

        EstActif = true;
        DateModificationUtc = dateModificationUtc;
    }

    public void Desactiver(DateTime dateModificationUtc)
    {
        if (!EstActif)
        {
            return;
        }

        EstActif = false;
        DateModificationUtc = dateModificationUtc;
    }

    public static string NormaliserCode(string code)
    {
        var normalized = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainValidationException("Le code du type matière est obligatoire.");
        }

        if (normalized.Length > 30)
        {
            throw new DomainValidationException("Le code du type matière ne peut pas dépasser 30 caractères.");
        }

        return normalized;
    }

    private static string NormaliserLibelle(string libelle)
    {
        var normalized = libelle?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainValidationException("Le libellé du type matière est obligatoire.");
        }

        if (normalized.Length > 100)
        {
            throw new DomainValidationException("Le libellé du type matière ne peut pas dépasser 100 caractères.");
        }

        return normalized;
    }
}
