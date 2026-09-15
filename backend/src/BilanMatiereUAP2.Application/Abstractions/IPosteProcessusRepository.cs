using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record PostesProcessusFilter(
    string? Poste,
    OrigineProcessus? Origine,
    bool? Actif,
    int Page,
    int PageSize);

public interface IPosteProcessusRepository
{
    Task<PagedResult<RefPosteProcessus>> GetPagedAsync(
        PostesProcessusFilter filter,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefPosteProcessus>> GetAllAsync(
        bool? actif,
        CancellationToken cancellationToken = default);
    Task<RefPosteProcessus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RefPosteProcessus?> GetByCodeAsync(string codePoste, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefPosteProcessusHistorique>> GetHistoryAsync(
        Guid posteProcessusId,
        CancellationToken cancellationToken = default);
    Task AddAsync(RefPosteProcessus entity, CancellationToken cancellationToken = default);
    Task AddHistoryAsync(
        RefPosteProcessusHistorique history,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
