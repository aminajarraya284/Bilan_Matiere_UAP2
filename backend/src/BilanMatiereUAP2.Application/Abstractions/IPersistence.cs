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

public interface IControlePoidsFonderieRepository
{
    Task<ControlePoidsFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<ControlePoidsFonderie>> GetPagedAsync(
        ControlesPoidsFonderieFilter filter,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ControlePoidsFonderie>> FindCandidatesAsync(
        string codeArticle,
        Guid typeMatiereId,
        string? numeroOF,
        DateOnly dateReception,
        CancellationToken cancellationToken);
    Task AddAsync(ControlePoidsFonderie controle, CancellationToken cancellationToken);
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

public sealed record ControlesPoidsFonderieFilter(
    DateOnly? DateControle,
    string? NumeroOF,
    string? CodeArticle,
    Guid? TypeMatiereId,
    StatutControlePoidsFonderie? Statut,
    int Page,
    int PageSize);

public sealed record ResumeDechetsFonderieData(
    Guid TypeMatiereId,
    string TypeMatiereCode,
    decimal TotalBavureKg,
    decimal TotalPoudreKg);
