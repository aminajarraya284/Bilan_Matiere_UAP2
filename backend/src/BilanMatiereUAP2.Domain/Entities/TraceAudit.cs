using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

public sealed class TraceAudit
{
    public const int JsonMaxLength = 16000;

    private TraceAudit() { }

    private TraceAudit(
        Guid id,
        DateTime dateUtc,
        ActionAudit action,
        DomaineAudit domaine,
        string entite,
        string entiteId,
        string? referenceMetier,
        string utilisateurId,
        string utilisateurNom,
        string? utilisateurEmail,
        string role,
        string source,
        string correlationId,
        string? resume,
        string? anciennesValeursJson,
        string? nouvellesValeursJson,
        string? informationsJson)
    {
        if (dateUtc.Kind != DateTimeKind.Utc)
            throw new DomainValidationException("La date d'audit doit etre en UTC.");
        if (!Enum.IsDefined(action))
            throw new DomainValidationException("L'action d'audit est invalide.");
        if (!Enum.IsDefined(domaine))
            throw new DomainValidationException("Le domaine d'audit est invalide.");

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        DateUtc = dateUtc;
        Action = action;
        Domaine = domaine;
        Entite = Obligatoire(entite, "L'entite d'audit", 100);
        EntiteId = Obligatoire(entiteId, "L'identifiant de l'entite", 150);
        ReferenceMetier = Optionnel(referenceMetier, 300);
        UtilisateurId = Obligatoire(utilisateurId, "L'identifiant utilisateur", 150);
        UtilisateurNom = Obligatoire(utilisateurNom, "Le nom utilisateur", 200);
        UtilisateurEmail = Optionnel(utilisateurEmail, 320);
        Role = Obligatoire(role, "Le role utilisateur", 200);
        Source = Obligatoire(source, "La source d'audit", 150);
        CorrelationId = Obligatoire(correlationId, "Le Correlation ID", 100);
        Resume = Optionnel(resume, 1000);
        AnciennesValeursJson = Json(anciennesValeursJson);
        NouvellesValeursJson = Json(nouvellesValeursJson);
        InformationsJson = Json(informationsJson);
    }

    public Guid Id { get; private set; }
    public DateTime DateUtc { get; private set; }
    public ActionAudit Action { get; private set; }
    public DomaineAudit Domaine { get; private set; }
    public string Entite { get; private set; } = string.Empty;
    public string EntiteId { get; private set; } = string.Empty;
    public string? ReferenceMetier { get; private set; }
    public string UtilisateurId { get; private set; } = string.Empty;
    public string UtilisateurNom { get; private set; } = string.Empty;
    public string? UtilisateurEmail { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string? Resume { get; private set; }
    public string? AnciennesValeursJson { get; private set; }
    public string? NouvellesValeursJson { get; private set; }
    public string? InformationsJson { get; private set; }

    public static TraceAudit Creer(
        DateTime dateUtc,
        ActionAudit action,
        DomaineAudit domaine,
        string entite,
        string entiteId,
        string? referenceMetier,
        string utilisateurId,
        string utilisateurNom,
        string? utilisateurEmail,
        string role,
        string source,
        string correlationId,
        string? resume,
        string? anciennesValeursJson,
        string? nouvellesValeursJson,
        string? informationsJson,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(), dateUtc, action, domaine, entite, entiteId,
            referenceMetier, utilisateurId, utilisateurNom, utilisateurEmail, role,
            source, correlationId, resume, anciennesValeursJson, nouvellesValeursJson,
            informationsJson);

    private static string Obligatoire(string? value, string label, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new DomainValidationException($"{label} est obligatoire.");
        if (normalized.Length > maxLength)
            throw new DomainValidationException($"{label} ne peut pas depasser {maxLength} caracteres.");
        return normalized;
    }

    private static string? Optionnel(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength)
            throw new DomainValidationException($"La valeur ne peut pas depasser {maxLength} caracteres.");
        return normalized;
    }

    private static string? Json(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Length > JsonMaxLength)
            throw new DomainValidationException($"Les donnees d'audit ne peuvent pas depasser {JsonMaxLength} caracteres.");
        return value;
    }
}
