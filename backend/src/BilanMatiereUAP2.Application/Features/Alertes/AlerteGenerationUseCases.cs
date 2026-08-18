using System.Globalization;
using System.Text.Json;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts.Alertes;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Alertes;

internal static class AlerteValidation
{
    public static DateOnly ParsePeriode(string periode)
    {
        if (!DateTime.TryParseExact(
                periode,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            throw new ValidationException([
                new ValidationFailure("periode", "La période doit respecter le format YYYY-MM.")
            ]);
        }

        return new DateOnly(parsed.Year, parsed.Month, 1);
    }
}

public interface IAlerteGenerationService
{
    Task<AlerteRecalculResponse> RecalculerAsync(
        string periode,
        CancellationToken cancellationToken = default);
}

public sealed class AlerteGenerationService(
    IAlerteDetectionService detectionService,
    IAlerteRepository repository,
    IDateTimeProvider dateTimeProvider) : IAlerteGenerationService
{
    public async Task<AlerteRecalculResponse> RecalculerAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        AlerteValidation.ParsePeriode(periode);
        var maintenant = dateTimeProvider.UtcNow;
        var detections = await detectionService.DetecterAsync(periode, cancellationToken);

        var detectionsParCle = detections
            .GroupBy(ConstruireCle, StringComparer.Ordinal)
            .ToDictionary(groupe => groupe.Key, groupe => groupe.First(), StringComparer.Ordinal);
        var actives = await repository.GetActiveByPeriodAsync(periode, cancellationToken);
        var activesParCle = actives.ToDictionary(alerte => alerte.CleDeduplication, StringComparer.Ordinal);

        var nouvelles = new List<AlerteMetier>();
        var misesAJour = 0;
        foreach (var (cle, detection) in detectionsParCle)
        {
            var json = SerialiserDonnees(detection.DonneesAssociees);
            if (activesParCle.TryGetValue(cle, out var existante))
            {
                existante.MettreAJourDetection(
                    detection.Severite,
                    detection.Titre,
                    detection.Description,
                    detection.ResolutionAutomatique,
                    json,
                    maintenant);
                misesAJour++;
                continue;
            }

            nouvelles.Add(AlerteMetier.Creer(
                detection.Code,
                detection.Type,
                detection.Severite,
                detection.Titre,
                detection.Description,
                detection.Source,
                detection.Periode,
                detection.NumeroOF,
                detection.CodeArticle,
                detection.TypeMatiere,
                cle,
                detection.ResolutionAutomatique,
                json,
                maintenant));
        }

        var resoluesAutomatiquement = 0;
        foreach (var existante in actives.Where(alerte => !detectionsParCle.ContainsKey(alerte.CleDeduplication)))
        {
            if (!existante.ResolutionAutomatique) continue;
            existante.ResoudreAutomatiquement(maintenant);
            resoluesAutomatiquement++;
        }

        if (nouvelles.Count > 0)
        {
            await repository.AddRangeAsync(nouvelles, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return new AlerteRecalculResponse(
            periode,
            maintenant,
            detectionsParCle.Count,
            nouvelles.Count,
            misesAJour,
            resoluesAutomatiquement);
    }

    private static string ConstruireCle(AlerteDetection detection)
        => AlerteDeduplication.Construire(
            detection.Code,
            detection.Periode,
            detection.NumeroOF,
            detection.CodeArticle,
            detection.TypeMatiere,
            detection.Source);

    private static string? SerialiserDonnees(IReadOnlyDictionary<string, string?> donnees)
        => donnees.Count == 0 ? null : JsonSerializer.Serialize(donnees);
}
