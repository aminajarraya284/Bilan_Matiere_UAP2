using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Contracts.Archivage;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Archivage;

public sealed record GetPeriodesBilanArchiveesQuery(string? Periode, string? Statut, int Page = 1, int PageSize = 20);
public sealed record RapportArchiveResult(byte[] Content, string ContentType, string FileName);

public interface IPeriodeBilanArchiveService
{
    Task<PeriodeBilanArchiveDetailResponse> EnregistrerAsync(string periode, CancellationToken cancellationToken = default);
    Task<PagedPeriodesBilanArchiveesResponse> SearchAsync(GetPeriodesBilanArchiveesQuery query, CancellationToken cancellationToken = default);
    Task<PeriodeBilanArchiveDetailResponse> GetAsync(string periode, CancellationToken cancellationToken = default);
    Task<RapportArchiveResult> GetRapportAsync(string periode, CancellationToken cancellationToken = default);
}

public sealed class PeriodeBilanArchiveService(
    IPeriodeBilanArchiveRepository repository,
    IUnitOfWork unitOfWork,
    IBilanFonderieService bilanFonderieService,
    IBilanUsinage2Service bilanUsinage2Service,
    IBilanUap2Service bilanUap2Service,
    IParametreApplicationRepository parametres,
    IAlerteRepository alertes,
    ICurrentUserContext currentUser,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService) : IPeriodeBilanArchiveService
{
    private const string VersionCalcul = "2026.09";
    private const string TemplateVersion = "Template_Rapport_Bilan_Matiere.md@1";
    private static readonly Regex PeriodeRegex = new("^(?:19|20)\\d{2}-(?:0[1-9]|1[0-2])$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PeriodeBilanArchiveDetailResponse> EnregistrerAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        periode = ValiderPeriode(periode);
        if (await repository.ExistsAsync(periode, cancellationToken))
            throw new ConflictException("PERIODE_DEJA_ENREGISTREE");

        // Appels séquentiels : les services partagent le même DbContext scoped.
        var fonderie = await bilanFonderieService.GetAsync(periode, cancellationToken);
        var usinage2 = await bilanUsinage2Service.GetAsync(periode, cancellationToken);
        var uap2 = await bilanUap2Service.GetAsync(periode, cancellationToken);
        var parametresSnapshot = await ChargerParametresAsync(cancellationToken);
        var alertesActives = await alertes.GetActiveByPeriodAsync(periode, cancellationToken);

        var anomalies = fonderie.Lignes.SelectMany(x => x.Anomalies)
            .Concat(usinage2.Lignes.SelectMany(x => x.Anomalies))
            .Concat(uap2.Lignes.SelectMany(x => x.Anomalies))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var nombreAnomalies = fonderie.NombreAnomalies + usinage2.NombreAnomalies + uap2.NombreAnomalies;
        var nombreManquantes = fonderie.NombreLignesDonneesManquantes
            + usinage2.NombreLignesDonneesManquantes + uap2.NombreLignesDonneesManquantes;
        var statut = DeterminerStatut(nombreAnomalies, nombreManquantes);
        var dateUtc = dateTimeProvider.UtcNow;
        var acteur = string.IsNullOrWhiteSpace(currentUser.DisplayName) ? currentUser.UserId : currentUser.DisplayName;
        var snapshot = new PeriodeBilanSnapshot(
            PeriodeBilanArchivee.SnapshotSchemaVersion, VersionCalcul, periode, dateUtc, acteur, statut,
            fonderie, usinage2, uap2, parametresSnapshot, anomalies);
        var archive = PeriodeBilanArchivee.Creer(
            periode, dateUtc, acteur, statut, VersionCalcul, alertesActives.Count,
            nombreAnomalies, nombreManquantes, JsonSerializer.Serialize(snapshot, JsonOptions), TemplateVersion);

        await repository.AddAsync(archive, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditService.EnregistrerAsync(new AuditEntry(
            ActionAudit.Cloture,
            DomaineAudit.ArchivageBilans,
            nameof(PeriodeBilanArchivee),
            archive.Id.ToString(),
            periode,
            nameof(PeriodeBilanArchiveService),
            $"Clôture de la période {periode}.",
            NouvellesValeurs: new Dictionary<string, object?>
            {
                ["periode"] = periode,
                ["statutGlobal"] = statut,
                ["nombreAlertes"] = alertesActives.Count,
                ["nombreAnomalies"] = nombreAnomalies,
                ["nombreDonneesManquantes"] = nombreManquantes,
                ["schemaVersion"] = PeriodeBilanArchivee.SnapshotSchemaVersion,
                ["versionCalcul"] = VersionCalcul
            }), cancellationToken);

        return ToDetail(archive, snapshot);
    }

    public async Task<PagedPeriodesBilanArchiveesResponse> SearchAsync(
        GetPeriodesBilanArchiveesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            throw Validation("page", "La pagination est invalide.");
        var periode = string.IsNullOrWhiteSpace(query.Periode) ? null : ValiderPeriode(query.Periode);
        var result = await repository.SearchAsync(
            new PeriodeBilanArchiveFilter(periode, Nettoyer(query.Statut), query.Page, query.PageSize), cancellationToken);
        return new PagedPeriodesBilanArchiveesResponse(
            result.Items.Select(ToListItem).ToArray(), query.Page, query.PageSize, result.Total,
            Math.Max(1, (int)Math.Ceiling(result.Total / (double)query.PageSize)));
    }

    public async Task<PeriodeBilanArchiveDetailResponse> GetAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        periode = ValiderPeriode(periode);
        var archive = await repository.GetByPeriodeAsync(periode, cancellationToken)
            ?? throw new NotFoundException("Période archivée introuvable.");
        return ToDetail(archive, Deserialiser(archive));
    }

    public async Task<RapportArchiveResult> GetRapportAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        var detail = await GetAsync(periode, cancellationToken);
        var contenu = GenererRapport(detail);
        return new RapportArchiveResult(
            new UTF8Encoding(true).GetBytes(contenu),
            "text/markdown; charset=utf-8",
            $"rapport-bilan-matiere-{detail.Metadata.Periode}.md");
    }

    private async Task<IReadOnlyDictionary<string, string?>> ChargerParametresAsync(CancellationToken cancellationToken)
    {
        var resultat = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var cle in new[] { ParametresApplicationCles.CodeAtelierUap2, ParametresApplicationCles.SeuilEcartUap2 })
            resultat[cle] = (await parametres.GetByCleAsync(cle, cancellationToken))?.Valeur;
        return resultat;
    }

    private static string DeterminerStatut(int anomalies, int manquantes)
        => manquantes > 0 ? "DonneesManquantes" : anomalies > 0 ? "Anomalie" : "OK";

    private static PeriodeBilanArchiveListItem ToListItem(PeriodeBilanArchivee x)
        => new(x.Id, x.Periode, x.DateEnregistrementUtc, x.EnregistrePar, x.StatutGlobal,
            x.NombreAlertes, x.NombreAnomalies, x.NombreDonneesManquantes, x.SchemaVersion, x.VersionCalcul);

    private static PeriodeBilanArchiveDetailResponse ToDetail(PeriodeBilanArchivee x, PeriodeBilanSnapshot snapshot)
        => new(ToListItem(x), snapshot);

    private static PeriodeBilanSnapshot Deserialiser(PeriodeBilanArchivee archive)
        => JsonSerializer.Deserialize<PeriodeBilanSnapshot>(archive.SnapshotJson, JsonOptions)
            ?? throw new InvalidOperationException("Le snapshot archivé est illisible.");

    private static string ValiderPeriode(string periode)
    {
        var valeur = periode?.Trim() ?? string.Empty;
        if (!PeriodeRegex.IsMatch(valeur)) throw Validation("periode", "La période doit respecter le format AAAA-MM.");
        return valeur;
    }

    private static string? Nettoyer(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ValidationException Validation(string property, string message)
        => new([new ValidationFailure(property, message)]);

    private static string GenererRapport(PeriodeBilanArchiveDetailResponse detail)
    {
        var s = detail.Snapshot;
        static string V(decimal? value, string unite) => value is null ? "—" : $"{value.Value.ToString("N2", CultureInfo.GetCultureInfo("fr-FR"))} {unite}";
        static string Cell(string? value) => (value ?? "—").Replace("|", "\\|");
        var b = new StringBuilder();
        b.AppendLine("# Rapport bilan matière — SOPAL").AppendLine();
        b.AppendLine($"- **Période :** {s.Periode}");
        b.AppendLine($"- **Date de clôture UTC :** {s.DateEnregistrementUtc:O}");
        b.AppendLine($"- **Enregistré par :** {Cell(s.EnregistrePar)}");
        b.AppendLine($"- **Statut global :** {s.StatutGlobal}").AppendLine();
        b.AppendLine("## Synthèse des bilans").AppendLine();
        b.AppendLine("| Bilan | Résultat | Unité | Lignes | Anomalies | Données manquantes |");
        b.AppendLine("|---|---:|---|---:|---:|---:|");
        b.AppendLine($"| Fonderie | {V(s.Fonderie.TotalEcartKg, "kg")} | kg | {s.Fonderie.NombreLignes} | {s.Fonderie.NombreAnomalies} | {s.Fonderie.NombreLignesDonneesManquantes} |");
        b.AppendLine($"| Usinage 2 | {V(s.Usinage2.TotalBilanUsinage2Pieces, "pièces")} | pièces | {s.Usinage2.NombreLignes} | {s.Usinage2.NombreAnomalies} | {s.Usinage2.NombreLignesDonneesManquantes} |");
        b.AppendLine($"| UAP2 | {V(s.Uap2.TotalEcartUap2Pieces, "pièces")} | pièces | {s.Uap2.NombreLignes} | {s.Uap2.NombreAnomalies} | {s.Uap2.NombreLignesDonneesManquantes} |").AppendLine();
        b.AppendLine("## Répartition des quantités matière — Fonderie").AppendLine();
        b.AppendLine("| Type matière | Input (kg) | Output (kg) | Écart (kg) | Taux | Statut |");
        b.AppendLine("|---|---:|---:|---:|---:|---|");
        foreach (var x in s.Fonderie.Lignes)
            b.AppendLine($"| {Cell(x.CodeTypeMatiere)} | {V(x.InputKg, "kg")} | {V(x.OutputKg, "kg")} | {V(x.EcartKg, "kg")} | {V(x.TauxEcartPourcent, "%")} | {Cell(x.Statut)} |");
        b.AppendLine().AppendLine("## Paramètres de calcul").AppendLine();
        foreach (var p in s.ParametresCalcul) b.AppendLine($"- **{Cell(p.Key)} :** {Cell(p.Value)}");
        b.AppendLine().AppendLine("## Anomalies archivées").AppendLine();
        if (s.Anomalies.Count == 0) b.AppendLine("Aucune anomalie.");
        else foreach (var anomalie in s.Anomalies) b.AppendLine($"- {Cell(anomalie)}");
        b.AppendLine().AppendLine("## Validation administrative").AppendLine();
        b.AppendLine("Archive officielle en lecture seule. Le contenu est généré exclusivement depuis le snapshot SQL.");
        return b.ToString();
    }
}

