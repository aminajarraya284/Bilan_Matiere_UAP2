namespace BilanMatiereUAP2.Application.Abstractions;

/// <summary>Type matiere actif : porte l'axe d'agregation du Bilan Fonderie.</summary>
public sealed record BilanFonderieTypeMatiere(Guid Id, string Code);

/// <summary>Stock de debut et de fin issus de la projection d'inventaire d'une periode.</summary>
public sealed record BilanFonderieStock(Guid TypeMatiereId, decimal? StockDebutKg, decimal StockFinKg);

/// <summary>
/// Consommations X3 rapprochees, deja regroupees par type matiere ET par unite.
/// L'unite est conservee telle quelle : c'est le use case qui decide de ce qui est
/// sommable en kg, jamais le repository.
/// </summary>
public sealed record BilanFonderieSortieX3(string CodeTypeMatiere, string Unite, decimal Quantite, int NombreLignes);

public sealed record BilanFonderieDechet(Guid TypeMatiereId, decimal PoidsKg);

/// <summary>
/// Receptions Usinage 2 validees. <paramref name="PoidsTotalKg"/> ne cumule que les
/// receptions dont le poids est renseigne ; les autres sont comptees a part pour
/// pouvoir etre signalees sans etre confondues avec un zero.
/// </summary>
public sealed record BilanFonderieReception(
    string CodeTypeMatiere,
    decimal PoidsTotalKg,
    int NombreLignesAvecPoids,
    int NombreLignesSansPoids);

public interface IBilanFonderieRepository
{
    Task<IReadOnlyList<BilanFonderieTypeMatiere>> GetTypesMatiereActifsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BilanFonderieStock>> GetStocksAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BilanFonderieSortieX3>> GetSortiesMagasinX3Async(
        DateOnly periode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BilanFonderieDechet>> GetDechetsAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BilanFonderieReception>> GetReceptionsUsinage2Async(
        DateOnly periode,
        CancellationToken cancellationToken = default);

    /// <summary>Receptions validees dont le type matiere n'a pas pu etre resolu.</summary>
    Task<int> CompterReceptionsSansTypeMatiereAsync(
        DateOnly periode,
        CancellationToken cancellationToken = default);
}
