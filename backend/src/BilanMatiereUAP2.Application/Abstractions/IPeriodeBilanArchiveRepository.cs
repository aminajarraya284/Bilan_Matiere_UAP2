using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record PeriodeBilanArchiveFilter(string? Periode, string? Statut, int Page, int PageSize);

public interface IPeriodeBilanArchiveRepository
{
    Task<bool> ExistsAsync(string periode, CancellationToken cancellationToken = default);
    Task AddAsync(PeriodeBilanArchivee archive, CancellationToken cancellationToken = default);
    Task<PeriodeBilanArchivee?> GetByPeriodeAsync(string periode, CancellationToken cancellationToken = default);
    Task<PagedResult<PeriodeBilanArchivee>> SearchAsync(
        PeriodeBilanArchiveFilter filter,
        CancellationToken cancellationToken = default);
}

