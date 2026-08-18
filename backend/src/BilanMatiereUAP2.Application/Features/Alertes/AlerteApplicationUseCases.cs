using System.Text.Json;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.Alertes;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.Alertes;

public sealed record GetAlertesQuery(
    string? Periode,
    string? Statut,
    string? Severite,
    string? Source,
    string? NumeroOF,
    string? CodeArticle,
    string? Recherche,
    int Page = 1,
    int PageSize = 50);

public interface IAlerteApplicationService
{
    Task<PagedAlertesResponse> SearchAsync(GetAlertesQuery query, CancellationToken cancellationToken = default);
    Task<AlerteDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AlerteDetailResponse> AcquitterAsync(Guid id, string? commentaire, CancellationToken cancellationToken = default);
    Task<AlerteDetailResponse> ResoudreAsync(Guid id, string? commentaire, CancellationToken cancellationToken = default);
    Task<AlerteDetailResponse> IgnorerAsync(Guid id, string commentaire, CancellationToken cancellationToken = default);
    Task<AlerteRecalculResponse> RecalculerAsync(string periode, CancellationToken cancellationToken = default);
}

public sealed class AlerteApplicationService(
    IAlerteRepository repository,
    IAlerteGenerationService generationService,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider) : IAlerteApplicationService
{
    private static readonly IReadOnlyCollection<string> SourcesFonderie = [SourcesAlertes.BilanFonderie];
    private static readonly IReadOnlyCollection<string> SourcesUsinage2 =
        [SourcesAlertes.BilanUsinage2, SourcesAlertes.BilanUap2];

    public async Task<PagedAlertesResponse> SearchAsync(
        GetAlertesQuery query,
        CancellationToken cancellationToken = default)
    {
        ValiderPagination(query.Page, query.PageSize);
        var periode = NormaliserOptionnel(query.Periode);
        if (periode is not null) AlerteValidation.ParsePeriode(periode);

        var filter = new AlerteSearchFilter(
            periode,
            ParseEnum<StatutAlerte>(query.Statut, "statut"),
            ParseEnum<SeveriteAlerte>(query.Severite, "severite"),
            NormaliserOptionnel(query.Source),
            NormaliserOptionnel(query.NumeroOF),
            NormaliserOptionnel(query.CodeArticle),
            NormaliserOptionnel(query.Recherche),
            GetSourcesAutorisees(),
            query.Page,
            query.PageSize);
        var result = await repository.SearchAsync(filter, cancellationToken);

        return new PagedAlertesResponse(
            result.Items.Select(ToListItem).ToArray(),
            result.Total,
            query.Page,
            query.PageSize,
            new AlerteSummaryResponse(
                result.Total,
                result.Ouvertes,
                result.Acquittees,
                result.Resolues,
                result.Ignorees),
            result.DerniereDetectionLeUtc);
    }

    public async Task<AlerteDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var alerte = await repository.GetByIdReadOnlyAsync(id, cancellationToken)
            ?? throw new NotFoundException("Alerte introuvable.");
        GarantirSourceAutorisee(alerte.Source);
        return ToDetail(alerte);
    }

    public Task<AlerteDetailResponse> AcquitterAsync(
        Guid id,
        string? commentaire,
        CancellationToken cancellationToken = default)
        => TraiterAsync(id, alerte =>
        {
            if (alerte.Statut is StatutAlerte.Resolue or StatutAlerte.Ignoree)
            {
                throw new ConflictException("Une alerte terminée ne peut pas être acquittée.");
            }

            alerte.Acquitter(UtilisateurCourant(), dateTimeProvider.UtcNow, commentaire);
        }, cancellationToken);

    public Task<AlerteDetailResponse> ResoudreAsync(
        Guid id,
        string? commentaire,
        CancellationToken cancellationToken = default)
        => TraiterAsync(id, alerte =>
        {
            if (alerte.Statut == StatutAlerte.Ignoree)
            {
                throw new ConflictException("Une alerte ignorée ne peut pas être résolue.");
            }

            alerte.Resoudre(UtilisateurCourant(), dateTimeProvider.UtcNow, commentaire);
        }, cancellationToken);

    public Task<AlerteDetailResponse> IgnorerAsync(
        Guid id,
        string commentaire,
        CancellationToken cancellationToken = default)
        => TraiterAsync(id, alerte =>
        {
            if (alerte.Statut == StatutAlerte.Resolue)
            {
                throw new ConflictException("Une alerte résolue ne peut pas être ignorée.");
            }

            alerte.Ignorer(UtilisateurCourant(), dateTimeProvider.UtcNow, commentaire);
        }, cancellationToken);

    public async Task<AlerteRecalculResponse> RecalculerAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        _ = GetSourcesAutorisees();
        return await generationService.RecalculerAsync(periode, cancellationToken);
    }

    private async Task<AlerteDetailResponse> TraiterAsync(
        Guid id,
        Action<AlerteMetier> action,
        CancellationToken cancellationToken)
    {
        var alerte = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Alerte introuvable.");
        GarantirSourceAutorisee(alerte.Source);
        action(alerte);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDetail(alerte);
    }

    private IReadOnlyCollection<string>? GetSourcesAutorisees()
    {
        var role = currentUser.Role;
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) return null;
        if (string.Equals(role, "ResponsableFonderie", StringComparison.OrdinalIgnoreCase)) return SourcesFonderie;
        if (string.Equals(role, "ResponsableUsinage2", StringComparison.OrdinalIgnoreCase)) return SourcesUsinage2;
        throw new ForbiddenException("Le rôle courant n'est pas autorisé à consulter les alertes.");
    }

    private void GarantirSourceAutorisee(string source)
    {
        var autorisees = GetSourcesAutorisees();
        if (autorisees is not null && !autorisees.Contains(source, StringComparer.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Cette alerte est hors du périmètre autorisé pour le rôle courant.");
        }
    }

    private string UtilisateurCourant()
        => currentUser.UserName ?? currentUser.UserId?.ToString() ?? "UTILISATEUR_AUTHENTIFIE";

    private static void ValiderPagination(int page, int pageSize)
    {
        var erreurs = new List<ValidationFailure>();
        if (page < 1) erreurs.Add(new ValidationFailure("page", "La page doit être supérieure ou égale à 1."));
        if (pageSize is < 1 or > 200)
        {
            erreurs.Add(new ValidationFailure("pageSize", "La taille de page doit être comprise entre 1 et 200."));
        }

        if (erreurs.Count > 0) throw new ValidationException(erreurs);
    }

    private static TEnum? ParseEnum<TEnum>(string? value, string propertyName)
        where TEnum : struct, Enum
    {
        var normalise = NormaliserOptionnel(value);
        if (normalise is null) return null;
        if (Enum.TryParse<TEnum>(normalise, ignoreCase: true, out var resultat)) return resultat;
        throw new ValidationException([
            new ValidationFailure(propertyName, $"La valeur '{normalise}' n'est pas valide.")
        ]);
    }

    private static string? NormaliserOptionnel(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AlerteListItemResponse ToListItem(AlerteMetier alerte)
        => new(
            alerte.Id,
            alerte.Code,
            alerte.Type.ToString(),
            alerte.Severite.ToString(),
            alerte.Statut.ToString(),
            alerte.Titre,
            alerte.Source,
            alerte.Periode,
            alerte.NumeroOF,
            alerte.CodeArticle,
            alerte.TypeMatiere,
            alerte.DetecteeLeUtc,
            alerte.DerniereDetectionLeUtc,
            alerte.Description,
            alerte.AcquitteeLeUtc,
            alerte.AcquitteePar,
            alerte.ResolueLeUtc,
            alerte.ResoluePar,
            alerte.IgnoreeLeUtc,
            alerte.IgnoreePar,
            alerte.CommentaireTraitement);

    private static AlerteDetailResponse ToDetail(AlerteMetier alerte)
        => new(
            alerte.Id,
            alerte.Code,
            alerte.Type.ToString(),
            alerte.Severite.ToString(),
            alerte.Statut.ToString(),
            alerte.Titre,
            alerte.Description,
            alerte.Source,
            alerte.Periode,
            alerte.NumeroOF,
            alerte.CodeArticle,
            alerte.TypeMatiere,
            alerte.DetecteeLeUtc,
            alerte.DerniereDetectionLeUtc,
            alerte.AcquitteeLeUtc,
            alerte.AcquitteePar,
            alerte.ResolueLeUtc,
            alerte.ResoluePar,
            alerte.IgnoreeLeUtc,
            alerte.IgnoreePar,
            alerte.CommentaireTraitement,
            alerte.ResolutionAutomatique,
            DeserialiserDonnees(alerte.DonneesAssocieesJson));

    private static IReadOnlyDictionary<string, string?> DeserialiserDonnees(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string?>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json)
                ?? new Dictionary<string, string?>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string?>();
        }
    }
}
