using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

/// <summary>
/// Acces au comptage physique d'inventaire, en trois parties.
/// Toutes les lectures sont filtrees sur une PERIODE (premier jour du mois).
/// </summary>
public interface IInventaireComptageRepository
{
    Task<IReadOnlyList<InventaireConteneur>> GetConteneursAsync(DateOnly periode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventaireCategorieComptage>> GetCategoriesAsync(DateOnly periode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventaireFourComptage>> GetFoursAsync(DateOnly periode, CancellationToken cancellationToken = default);

    Task<InventaireConteneur?> GetConteneurAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventaireCategorieComptage?> GetCategorieAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventaireFourComptage?> GetFourAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(InventaireConteneur ligne, CancellationToken cancellationToken = default);
    Task AddAsync(InventaireCategorieComptage ligne, CancellationToken cancellationToken = default);
    Task AddAsync(InventaireFourComptage ligne, CancellationToken cancellationToken = default);

    Task<InventaireFonderie?> GetProjectionAsync(
        DateOnly periode,
        Guid typeMatiereId,
        CancellationToken cancellationToken = default);

    Task AddProjectionAsync(
        InventaireFonderie projection,
        CancellationToken cancellationToken = default);

    Task<IInventaireComptageTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Transaction applicative couvrant la ligne détaillée et sa projection mensuelle.
/// </summary>
public interface IInventaireComptageTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
