using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class TypeMatiereRepository(BilanMatiereUAP2DbContext dbContext) : ITypeMatiereRepository
{
    public async Task<IReadOnlyList<TypeMatiere>> GetAllAsync(bool? actif, CancellationToken cancellationToken)
    {
        var query = dbContext.TypesMatiere.AsNoTracking();
        if (actif is not null)
        {
            query = query.Where(entity => entity.EstActif == actif);
        }

        return await query.OrderBy(entity => entity.Code).ToListAsync(cancellationToken);
    }

    public Task<TypeMatiere?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.TypesMatiere.SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public Task<TypeMatiere?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken)
    {
        return dbContext.TypesMatiere
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Code == normalizedCode, cancellationToken);
    }

    public async Task AddAsync(TypeMatiere typeMatiere, CancellationToken cancellationToken)
    {
        await dbContext.TypesMatiere.AddAsync(typeMatiere, cancellationToken);
    }
}
