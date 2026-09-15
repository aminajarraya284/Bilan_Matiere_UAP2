using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class RefPosteProcessus
{
    private RefPosteProcessus()
    {
    }

    private RefPosteProcessus(
        Guid id,
        string codePoste,
        string? libellePoste,
        OrigineProcessus origineProcessus,
        string? creePar,
        DateTime dateCreationUtc)
    {
        Id = id;
        CodePoste = NormaliserCodePoste(codePoste);
        LibellePoste = NormaliserLibelle(libellePoste);
        OrigineProcessus = ValiderOrigine(origineProcessus);
        IsActive = true;
        CreePar = NormaliserActeur(creePar);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public string CodePoste { get; private set; } = string.Empty;
    public string? LibellePoste { get; private set; }
    public OrigineProcessus OrigineProcessus { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public DateTime? DateModificationUtc { get; private set; }
    public string? CreePar { get; private set; }
    public string? ModifiePar { get; private set; }

    public static RefPosteProcessus Creer(
        string codePoste,
        string? libellePoste,
        OrigineProcessus origineProcessus,
        string? creePar,
        DateTime dateCreationUtc)
        => new(Guid.NewGuid(), codePoste, libellePoste, origineProcessus, creePar, dateCreationUtc);

    public void Modifier(
        string? libellePoste,
        OrigineProcessus origineProcessus,
        string? modifiePar,
        DateTime dateModificationUtc)
    {
        LibellePoste = NormaliserLibelle(libellePoste);
        OrigineProcessus = ValiderOrigine(origineProcessus);
        ModifiePar = NormaliserActeur(modifiePar);
        DateModificationUtc = dateModificationUtc;
    }

    public void ChangerActivation(bool actif, string? modifiePar, DateTime dateModificationUtc)
    {
        if (IsActive == actif)
        {
            return;
        }

        IsActive = actif;
        ModifiePar = NormaliserActeur(modifiePar);
        DateModificationUtc = dateModificationUtc;
    }

    public static string NormaliserCodePoste(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new DomainValidationException("Le poste est obligatoire.");
        }
        if (normalized.Length > 50)
        {
            throw new DomainValidationException("Le poste ne peut pas depasser 50 caracteres.");
        }
        return normalized;
    }

    private static OrigineProcessus ValiderOrigine(OrigineProcessus origine)
    {
        if (origine is OrigineProcessus.Inconnue || !Enum.IsDefined(origine))
        {
            throw new DomainValidationException("L'origine doit etre CTRL, SCI, EMR, TRN, U2 ou FON.");
        }
        return origine;
    }

    private static string? NormaliserLibelle(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }
        if (normalized.Length > 200)
        {
            throw new DomainValidationException("Le libelle du poste ne peut pas depasser 200 caracteres.");
        }
        return normalized;
    }

    private static string? NormaliserActeur(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }
        return normalized.Length <= 150 ? normalized : normalized[..150];
    }
}

public sealed class RefPosteProcessusHistorique
{
    private RefPosteProcessusHistorique()
    {
    }

    private RefPosteProcessusHistorique(
        Guid posteProcessusId,
        string codePoste,
        OrigineProcessus origineProcessus,
        bool actif,
        string action,
        string? utilisateur,
        DateTime dateUtc)
    {
        Id = Guid.NewGuid();
        PosteProcessusId = posteProcessusId;
        CodePoste = codePoste;
        OrigineProcessus = origineProcessus;
        Actif = actif;
        Action = action;
        Utilisateur = utilisateur;
        DateUtc = dateUtc;
    }

    public Guid Id { get; private set; }
    public Guid PosteProcessusId { get; private set; }
    public string CodePoste { get; private set; } = string.Empty;
    public OrigineProcessus OrigineProcessus { get; private set; }
    public bool Actif { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? Utilisateur { get; private set; }
    public DateTime DateUtc { get; private set; }

    public static RefPosteProcessusHistorique Capturer(
        RefPosteProcessus poste,
        string action,
        string? utilisateur,
        DateTime dateUtc)
        => new(
            poste.Id,
            poste.CodePoste,
            poste.OrigineProcessus,
            poste.IsActive,
            action,
            utilisateur?.Trim(),
            dateUtc);
}
