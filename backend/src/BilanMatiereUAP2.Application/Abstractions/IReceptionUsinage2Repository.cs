using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record ReceptionsUsinage2Filter(
    DateOnly? PeriodeDebut,
    DateOnly? PeriodeFin,
    string? NumeroOF,
    string? CodeArticle,
    string? CodeTypeMatiere,
    string? Atelier,
    string? Statut,
    int Page,
    int PageSize);

public interface IReceptionUsinage2Repository
{
    Task<ReceptionUsinage2?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ReceptionUsinage2>> GetPagedAsync(ReceptionsUsinage2Filter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByOfAndArticleAsync(string numeroOF, string codeArticle, int annee, int mois, CancellationToken cancellationToken = default);

    // Receptions Valides du mois, utilisees pour le calcul du Bilan Usinage 2 (INPUT en pieces).
    Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceptionUsinage2>> GetToutesValidesByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default);

    // Receptions Valides d'une DATE precise, utilisees par l'alerte journaliere de non-reception.
    Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReceptionUsinage2>> GetConcernedByControlePoidsAsync(
        string codeArticle,
        string codeTypeMatiere,
        string? numeroOF,
        DateOnly dateControle,
        CancellationToken cancellationToken = default);

    Task AddAsync(ReceptionUsinage2 reception, CancellationToken cancellationToken = default);
}
