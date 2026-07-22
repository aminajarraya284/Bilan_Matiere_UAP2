using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public interface ITypeMatiereRepository
{
    Task<IReadOnlyList<TypeMatiere>> GetAllAsync(bool? actif, CancellationToken cancellationToken);
    Task<TypeMatiere?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TypeMatiere?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken);
    Task AddAsync(TypeMatiere typeMatiere, CancellationToken cancellationToken);
}

public interface IDechetFonderieRepository
{
    Task<DechetFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<DechetFonderie>> GetPagedAsync(DechetsFonderieFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<ResumeDechetsFonderieData>> GetResumeAsync(
        DateOnly dateDebutIncluse,
        DateOnly dateFinExclusive,
        CancellationToken cancellationToken);
    Task AddAsync(DechetFonderie dechet, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);

public sealed record DechetsFonderieFilter(
    DateOnly DateDebutIncluse,
    DateOnly DateFinExclusive,
    Guid? TypeMatiereId,
    TypeDechet? TypeDechet,
    int Page,
    int PageSize);

public sealed record ResumeDechetsFonderieData(
    Guid TypeMatiereId,
    string TypeMatiereCode,
    decimal TotalBavureKg,
    decimal TotalPoudreKg);
