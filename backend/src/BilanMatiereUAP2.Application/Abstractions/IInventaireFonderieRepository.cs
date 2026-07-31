using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record InventairesFonderieFilter(
    DateOnly? Periode,
    Guid? TypeMatiereId,
    StatutInventaireFonderie? Statut,
    int Page,
    int PageSize);

public interface IInventaireFonderieRepository
{
    Task<InventaireFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<InventaireFonderie>> GetPagedAsync(
        InventairesFonderieFilter filter,
        CancellationToken cancellationToken);
    Task<bool> ExistsActiveAsync(
        DateOnly periode,
        Guid typeMatiereId,
        Guid? excludeId,
        CancellationToken cancellationToken);
    Task AddAsync(InventaireFonderie inventaire, CancellationToken cancellationToken);
}
