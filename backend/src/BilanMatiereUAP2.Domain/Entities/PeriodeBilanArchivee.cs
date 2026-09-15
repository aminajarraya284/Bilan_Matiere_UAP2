using BilanMatiereUAP2.Domain.Common;

namespace BilanMatiereUAP2.Domain.Entities;

/// <summary>Snapshot immuable des bilans d'une période clôturée par un administrateur.</summary>
public sealed class PeriodeBilanArchivee
{
    public const int SnapshotSchemaVersion = 1;

    private PeriodeBilanArchivee() { }

    private PeriodeBilanArchivee(
        string periode,
        DateTime dateEnregistrementUtc,
        string enregistrePar,
        string statutGlobal,
        string versionCalcul,
        int nombreAlertes,
        int nombreAnomalies,
        int nombreDonneesManquantes,
        string snapshotJson,
        string templateVersion)
    {
        if (dateEnregistrementUtc.Kind != DateTimeKind.Utc)
            throw new DomainValidationException("La date d'enregistrement doit être en UTC.");
        if (string.IsNullOrWhiteSpace(periode) || periode.Length != 7 || periode[4] != '-')
            throw new DomainValidationException("La période doit respecter le format AAAA-MM.");
        if (string.IsNullOrWhiteSpace(snapshotJson))
            throw new DomainValidationException("Le snapshot est obligatoire.");

        Id = Guid.NewGuid();
        Periode = periode;
        DateEnregistrementUtc = dateEnregistrementUtc;
        EnregistrePar = Obligatoire(enregistrePar, "L'utilisateur d'enregistrement", 200);
        StatutGlobal = Obligatoire(statutGlobal, "Le statut global", 50);
        VersionCalcul = Obligatoire(versionCalcul, "La version de calcul", 50);
        NombreAlertes = Math.Max(0, nombreAlertes);
        NombreAnomalies = Math.Max(0, nombreAnomalies);
        NombreDonneesManquantes = Math.Max(0, nombreDonneesManquantes);
        SnapshotJson = snapshotJson;
        RapportTemplateVersion = Obligatoire(templateVersion, "La version du modèle de rapport", 50);
        SchemaVersion = SnapshotSchemaVersion;
    }

    public Guid Id { get; private set; }
    public string Periode { get; private set; } = string.Empty;
    public DateTime DateEnregistrementUtc { get; private set; }
    public string EnregistrePar { get; private set; } = string.Empty;
    public string StatutGlobal { get; private set; } = string.Empty;
    public string VersionCalcul { get; private set; } = string.Empty;
    public int SchemaVersion { get; private set; }
    public int NombreAlertes { get; private set; }
    public int NombreAnomalies { get; private set; }
    public int NombreDonneesManquantes { get; private set; }
    public string SnapshotJson { get; private set; } = string.Empty;
    public string RapportTemplateVersion { get; private set; } = string.Empty;

    public static PeriodeBilanArchivee Creer(
        string periode,
        DateTime dateEnregistrementUtc,
        string enregistrePar,
        string statutGlobal,
        string versionCalcul,
        int nombreAlertes,
        int nombreAnomalies,
        int nombreDonneesManquantes,
        string snapshotJson,
        string templateVersion)
        => new(periode, dateEnregistrementUtc, enregistrePar, statutGlobal, versionCalcul,
            nombreAlertes, nombreAnomalies, nombreDonneesManquantes, snapshotJson, templateVersion);

    private static string Obligatoire(string value, string label, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            throw new DomainValidationException($"{label} est obligatoire et limité à {maxLength} caractères.");
        return value.Trim();
    }
}
