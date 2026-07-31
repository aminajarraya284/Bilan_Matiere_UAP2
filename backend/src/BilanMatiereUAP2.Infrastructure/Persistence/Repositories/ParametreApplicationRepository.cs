using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class ParametreApplicationRepository(
    BilanMatiereUAP2DbContext context) : IParametreApplicationRepository
{
    public async Task<IReadOnlyList<ParametreApplication>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.ParametresApplication
            .AsNoTracking()
            .OrderBy(parameter => parameter.Cle)
            .ToListAsync(cancellationToken);

    public Task<ParametreApplication?> GetByCleAsync(string cle, CancellationToken cancellationToken = default)
    {
        var normalized = BilanMetierRules.NormaliserCle(cle);
        return context.ParametresApplication
            .AsNoTracking()
            .FirstOrDefaultAsync(parameter => parameter.Cle == normalized, cancellationToken);
    }

    public Task<ParametreApplication?> GetByCleForUpdateAsync(
        string cle,
        CancellationToken cancellationToken = default)
    {
        var normalized = BilanMetierRules.NormaliserCle(cle);
        return context.ParametresApplication
            .FirstOrDefaultAsync(parameter => parameter.Cle == normalized, cancellationToken);
    }
}
