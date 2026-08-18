using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record AlerteSearchFilter(
    string? Periode,
    StatutAlerte? Statut,
    SeveriteAlerte? Severite,
    string? Source,
    string? NumeroOF,
    string? CodeArticle,
    string? Recherche,
    IReadOnlyCollection<string>? SourcesAutorisees,
    int Page,
    int PageSize);

public sealed record AlerteSearchResult(
    IReadOnlyList<AlerteMetier> Items,
    int Total,
    int Ouvertes,
    int Acquittees,
    int Resolues,
    int Ignorees,
    DateTime? DerniereDetectionLeUtc);

public interface IAlerteRepository
{
    Task<AlerteMetier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AlerteMetier?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlerteMetier>> GetActiveByPeriodAsync(
        string periode,
        CancellationToken cancellationToken = default);

    Task<AlerteSearchResult> SearchAsync(
        AlerteSearchFilter filter,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<AlerteMetier> alertes,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
