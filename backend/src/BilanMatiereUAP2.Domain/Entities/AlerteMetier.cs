using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class AlerteMetier
{
    public const int CommentaireMaxLength = 1000;
    public const int DonneesAssocieesMaxLength = 4000;

    private AlerteMetier()
    {
    }

    private AlerteMetier(
        Guid id,
        string code,
        TypeAlerte type,
        SeveriteAlerte severite,
        string titre,
        string description,
        string source,
        string? periode,
        string? numeroOf,
        string? codeArticle,
        string? typeMatiere,
        string cleDeduplication,
        bool resolutionAutomatique,
        string? donneesAssocieesJson,
        DateTime detecteeLeUtc)
    {
        Id = id;
        Code = NormaliserObligatoire(code, 100, "Le code d'alerte est obligatoire.", majuscules: true);
        Type = type;
        Severite = severite;
        Statut = StatutAlerte.Ouverte;
        Titre = NormaliserObligatoire(titre, 200, "Le titre de l'alerte est obligatoire.");
        Description = NormaliserObligatoire(description, 1000, "La description de l'alerte est obligatoire.");
        Source = NormaliserObligatoire(source, 100, "La source de l'alerte est obligatoire.");
        Periode = NormaliserOptionnel(periode, 7, majuscules: true);
        NumeroOF = NormaliserOptionnel(numeroOf, 50, majuscules: true);
        CodeArticle = NormaliserOptionnel(codeArticle, 100, majuscules: true);
        TypeMatiere = NormaliserOptionnel(typeMatiere, 50, majuscules: true);
        CleDeduplication = NormaliserObligatoire(
            cleDeduplication,
            600,
            "La clé de déduplication est obligatoire.",
            majuscules: true);
        ResolutionAutomatique = resolutionAutomatique;
        DonneesAssocieesJson = NormaliserOptionnel(donneesAssocieesJson, DonneesAssocieesMaxLength);
        DetecteeLeUtc = GarantirUtc(detecteeLeUtc);
        DerniereDetectionLeUtc = DetecteeLeUtc;
        EstActive = true;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public TypeAlerte Type { get; private set; }
    public SeveriteAlerte Severite { get; private set; }
    public StatutAlerte Statut { get; private set; }
    public string Titre { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public string? Periode { get; private set; }
    public string? NumeroOF { get; private set; }
    public string? CodeArticle { get; private set; }
    public string? TypeMatiere { get; private set; }
    public string CleDeduplication { get; private set; } = string.Empty;
    public DateTime DetecteeLeUtc { get; private set; }
    public DateTime DerniereDetectionLeUtc { get; private set; }
    public DateTime? AcquitteeLeUtc { get; private set; }
    public string? AcquitteePar { get; private set; }
    public DateTime? ResolueLeUtc { get; private set; }
    public string? ResoluePar { get; private set; }
    public DateTime? IgnoreeLeUtc { get; private set; }
    public string? IgnoreePar { get; private set; }
    public string? CommentaireTraitement { get; private set; }
    public string? DonneesAssocieesJson { get; private set; }
    public bool ResolutionAutomatique { get; private set; }
    public bool EstActive { get; private set; }

    public static AlerteMetier Creer(
        string code,
        TypeAlerte type,
        SeveriteAlerte severite,
        string titre,
        string description,
        string source,
        string? periode,
        string? numeroOf,
        string? codeArticle,
        string? typeMatiere,
        string cleDeduplication,
        bool resolutionAutomatique,
        string? donneesAssocieesJson,
        DateTime detecteeLeUtc,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            code,
            type,
            severite,
            titre,
            description,
            source,
            periode,
            numeroOf,
            codeArticle,
            typeMatiere,
            cleDeduplication,
            resolutionAutomatique,
            donneesAssocieesJson,
            detecteeLeUtc);

    public void MettreAJourDetection(
        SeveriteAlerte severite,
        string titre,
        string description,
        bool resolutionAutomatique,
        string? donneesAssocieesJson,
        DateTime detectionUtc)
    {
        if (!EstActive)
        {
            throw new DomainValidationException("Une alerte terminée ne peut pas être mise à jour comme active.");
        }

        var date = GarantirUtc(detectionUtc);
        if (date < DerniereDetectionLeUtc)
        {
            throw new DomainValidationException("La dernière détection ne peut pas précéder la détection précédente.");
        }

        Severite = severite;
        Titre = NormaliserObligatoire(titre, 200, "Le titre de l'alerte est obligatoire.");
        Description = NormaliserObligatoire(description, 1000, "La description de l'alerte est obligatoire.");
        ResolutionAutomatique = resolutionAutomatique;
        DonneesAssocieesJson = NormaliserOptionnel(donneesAssocieesJson, DonneesAssocieesMaxLength);
        DerniereDetectionLeUtc = date;
    }

    public void Acquitter(string utilisateur, DateTime dateUtc, string? commentaire = null)
    {
        if (Statut == StatutAlerte.Acquittee)
        {
            return;
        }

        if (Statut != StatutAlerte.Ouverte || !EstActive)
        {
            throw new DomainValidationException("Seule une alerte ouverte peut être acquittée.");
        }

        AcquitteePar = NormaliserObligatoire(utilisateur, 150, "L'utilisateur d'acquittement est obligatoire.");
        AcquitteeLeUtc = GarantirDateAction(dateUtc);
        MettreAJourCommentaireOptionnel(commentaire);
        Statut = StatutAlerte.Acquittee;
    }

    public void Resoudre(string utilisateur, DateTime dateUtc, string? commentaire = null)
    {
        if (Statut == StatutAlerte.Resolue)
        {
            return;
        }

        if (Statut == StatutAlerte.Ignoree || !EstActive)
        {
            throw new DomainValidationException("Une alerte ignorée ne peut pas être résolue.");
        }

        ResoluePar = NormaliserObligatoire(utilisateur, 150, "L'utilisateur de résolution est obligatoire.");
        ResolueLeUtc = GarantirDateAction(dateUtc);
        MettreAJourCommentaireOptionnel(commentaire);
        Statut = StatutAlerte.Resolue;
        EstActive = false;
    }

    public void Ignorer(string utilisateur, DateTime dateUtc, string commentaire)
    {
        if (string.IsNullOrWhiteSpace(commentaire))
        {
            throw new DomainValidationException("Une justification est obligatoire pour ignorer une alerte.");
        }

        if (Statut == StatutAlerte.Ignoree)
        {
            return;
        }

        if (Statut == StatutAlerte.Resolue || !EstActive)
        {
            throw new DomainValidationException("Une alerte résolue ne peut pas être ignorée.");
        }

        IgnoreePar = NormaliserObligatoire(utilisateur, 150, "L'utilisateur d'ignorance est obligatoire.");
        IgnoreeLeUtc = GarantirDateAction(dateUtc);
        CommentaireTraitement = NormaliserObligatoire(
            commentaire,
            CommentaireMaxLength,
            "Une justification est obligatoire pour ignorer une alerte.");
        Statut = StatutAlerte.Ignoree;
        EstActive = false;
    }

    public void ResoudreAutomatiquement(DateTime dateUtc)
    {
        if (!EstActive || !ResolutionAutomatique)
        {
            return;
        }

        ResoluePar = "SYSTEME";
        ResolueLeUtc = GarantirDateAction(dateUtc);
        CommentaireTraitement = "Condition non détectée lors du recalcul de la même période.";
        Statut = StatutAlerte.Resolue;
        EstActive = false;
    }

    private DateTime GarantirDateAction(DateTime value)
    {
        var date = GarantirUtc(value);
        if (date < DetecteeLeUtc)
        {
            throw new DomainValidationException("La date de traitement ne peut pas précéder la détection.");
        }

        return date;
    }

    private void MettreAJourCommentaireOptionnel(string? commentaire)
    {
        var normalise = NormaliserOptionnel(commentaire, CommentaireMaxLength);
        if (normalise is not null)
        {
            CommentaireTraitement = normalise;
        }
    }

    private static DateTime GarantirUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainValidationException("Les dates d'alerte doivent être exprimées en UTC.");
        }

        return value;
    }

    private static string NormaliserObligatoire(
        string? value,
        int maxLength,
        string message,
        bool majuscules = false)
    {
        var normalise = value?.Trim();
        if (string.IsNullOrEmpty(normalise))
        {
            throw new DomainValidationException(message);
        }

        if (normalise.Length > maxLength)
        {
            throw new DomainValidationException($"La valeur ne peut pas dépasser {maxLength} caractères.");
        }

        return majuscules ? normalise.ToUpperInvariant() : normalise;
    }

    private static string? NormaliserOptionnel(string? value, int maxLength, bool majuscules = false)
    {
        var normalise = value?.Trim();
        if (string.IsNullOrEmpty(normalise))
        {
            return null;
        }

        if (normalise.Length > maxLength)
        {
            throw new DomainValidationException($"La valeur ne peut pas dépasser {maxLength} caractères.");
        }

        return majuscules ? normalise.ToUpperInvariant() : normalise;
    }
}
