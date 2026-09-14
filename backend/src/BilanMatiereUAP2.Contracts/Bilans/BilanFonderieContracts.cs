namespace BilanMatiereUAP2.Contracts.Bilans;

/// <summary>Consultation live par intervalle inclusif ; le contrat mensuel archivé reste inchangé.</summary>
public sealed record BilanFonderieIntervalleResponse(
    DateOnly DateDebut,
    DateOnly DateFin,
    BilanFonderieResponse Bilan);

/// <summary>
/// Une ligne de Bilan Fonderie, pour un type matiere et une periode (RM-FON-003).
///
/// CONVENTION D'UNITES, stricte : tout est en KILOGRAMMES, et chaque champ le porte
/// dans son nom. Aucune piece n'entre dans ce bilan : une source exprimee en pieces
/// n'est retenue que si son poids en kilogrammes est deja resolu.
///
/// DONNEE MANQUANTE (RM-COM-003) : une composante absente reste <c>null</c> et n'est
/// JAMAIS ramenee a zero. Un <c>null</c> dans une composante propage <c>null</c> au
/// total qui en depend, et l'anomalie correspondante est listee.
/// </summary>
public sealed record BilanFonderieLigneResponse(
    Guid TypeMatiereId,
    string CodeTypeMatiere,
    string Periode,

    // --- INPUT = StockDebut + SortiesMagasinX3 ------------------------------
    /// <summary>Stock de debut, repris du stock de fin de la periode precedente (RM-INV-005).</summary>
    decimal? StockDebutKg,
    /// <summary>Consommations X3 rapprochees de la periode, part exprimee en kg uniquement.</summary>
    decimal? SortiesMagasinX3Kg,
    decimal? InputKg,

    // --- OUTPUT = StockFin + DechetsFonderie + ReceptionsUsinage2 -----------
    /// <summary>Stock de fin issu de la projection d'inventaire de la periode.</summary>
    decimal? StockFinKg,
    /// <summary>Dechets fonderie valides de la periode. Toujours en kg, jamais null.</summary>
    decimal DechetsFonderieKg,
    /// <summary>Receptions Usinage 2 validees, part dont le poids en kg est renseigne.</summary>
    decimal? ReceptionsUsinage2Kg,
    decimal? OutputKg,

    // --- ECART = INPUT - OUTPUT --------------------------------------------
    /// <summary>Jamais de Math.Abs : le signe distingue un manquant d'un excedent.</summary>
    decimal? EcartKg,
    decimal? TauxEcartPourcent,

    IReadOnlyList<string> Anomalies,
    /// <summary><c>Complet</c> si aucune composante ne manque, <c>Incomplet</c> sinon.</summary>
    string Statut);

public sealed record BilanFonderieResponse(
    string Periode,
    IReadOnlyList<BilanFonderieLigneResponse> Lignes,
    /// <summary>Null des qu'une ligne a un INPUT inconnu : un total partiel serait trompeur.</summary>
    decimal? TotalInputKg,
    decimal? TotalOutputKg,
    decimal? TotalEcartKg,
    int NombreLignes,
    int NombreLignesDonneesManquantes,
    int NombreAnomalies);
