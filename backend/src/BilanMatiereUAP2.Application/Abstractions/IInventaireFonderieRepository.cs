using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record InventaireConteneurCriteria(
    int? Mois,
    int? Annee,
    string? CodeTypeMatiere,
    StatutInventaire? Statut,
    string? Search);

public sealed record InventaireCategorieCriteria(
    int? Mois,
    int? Annee,
    string? CodeTypeMatiere,
    StatutInventaire? Statut,
    CategorieInventaireFonderie? Categorie);

public sealed record InventaireFourCriteria(
    int? Mois,
    int? Annee,
    string? CodeTypeMatiere,
    StatutInventaire? Statut,
    string? CodeFour);

public sealed record InventaireSyntheseRow(
    string CodeTypeMatiere,
    decimal Partie1Kg,
    decimal Partie2Kg,
    decimal Partie3FourKg);

public sealed record InventairesFonderieFilter(
    DateOnly? Periode,
    Guid? TypeMatiereId,
    StatutInventaireFonderie? Statut,
    int Page,
    int PageSize);

public interface IInventaireFonderieRepository
{
    // --- Partie 1 : Conteneurs / OF ---
    Task<PagedResult<InventaireConteneurOf>> SearchConteneursAsync(
        InventaireConteneurCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<InventaireConteneurOf?> GetConteneurByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddConteneurAsync(
        InventaireConteneurOf entity,
        CancellationToken cancellationToken);

    Task RemoveConteneurAsync(
        InventaireConteneurOf entity,
        CancellationToken cancellationToken);

    Task<bool> ConteneurExisteAsync(
        DateOnly dateInventaire,
        string numeroConteneur,
        string numeroOF,
        string codeTypeMatiere,
        CancellationToken cancellationToken);

    // --- Partie 2 : Catégories ---
    Task<PagedResult<InventaireCategorie>> SearchCategoriesAsync(
        InventaireCategorieCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<InventaireCategorie?> GetCategorieByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddCategorieAsync(
        InventaireCategorie entity,
        CancellationToken cancellationToken);

    Task RemoveCategorieAsync(
        InventaireCategorie entity,
        CancellationToken cancellationToken);

    // --- Partie 3 : Fours ---
    Task<PagedResult<InventaireFour>> SearchFoursAsync(
        InventaireFourCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<InventaireFour?> GetFourByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddFourAsync(
        InventaireFour entity,
        CancellationToken cancellationToken);

    Task RemoveFourAsync(
        InventaireFour entity,
        CancellationToken cancellationToken);

    // --- Synthèse ---
    Task<IReadOnlyCollection<InventaireSyntheseRow>> GetSyntheseAsync(
        int mois,
        int annee,
        string? codeTypeMatiere,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    // --- Ancien modèle d'inventaire fonderie ---
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
