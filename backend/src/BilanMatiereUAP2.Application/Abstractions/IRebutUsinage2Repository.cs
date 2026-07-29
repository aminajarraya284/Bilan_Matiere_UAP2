using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public interface IRebutUsinage2Repository
{
    Task AddAsync(RebutUsinage2 rebut, CancellationToken cancellationToken = default);
    Task<RebutUsinage2?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<RebutUsinage2> Items, int Total)> QueryAsync(
        int? annee,
        int? mois,
        string? numeroOF,
        string? codeArticle,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuts VALIDES de la periode, seuls retenus par le Bilan Usinage 2.
    /// Les brouillons et les rebuts annules sont exclus.
    /// </summary>
    Task<IReadOnlyList<RebutUsinage2>> GetValidesByPeriodeAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
