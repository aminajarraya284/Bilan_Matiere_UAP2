using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

/// <summary>
/// Lectures strictement necessaires au Bilan UAP2, sur une periode.
///
/// Volontairement etroite : les repositories d'import exposent des dizaines de membres
/// (ecritures, transactions, suivi d'import) dont ce bilan n'a aucun usage. Dependre de
/// l'ensemble rendrait le service opaque sur ce qu'il lit reellement.
/// </summary>
public interface IBilanUap2Repository
{
    Task<IReadOnlyList<string>> GetPeriodesDisponiblesAsync(CancellationToken cancellationToken = default);

    /// <summary>Lignes YRJMCH actives : colonne H (quantite) et colonne I (rebut).</summary>
    Task<IReadOnlyList<X3RapportMachine>> GetRapportsMachineAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default);

    /// <summary>Entrees OF de la periode, en pieces.</summary>
    Task<IReadOnlyList<EntreeOfComposant>> GetEntreesOfAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default);

    /// <summary>Rebuts Usinage 2 VALIDES uniquement (brouillons et annules exclus).</summary>
    Task<IReadOnlyList<RebutUsinage2>> GetRebutsValidesAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default);

    /// <summary>Receptions VALIDES : seule source du poids physique unitaire resolu.</summary>
    Task<IReadOnlyList<ReceptionUsinage2>> GetReceptionsValidesAsync(
        int annee,
        int mois,
        CancellationToken cancellationToken = default);
}
