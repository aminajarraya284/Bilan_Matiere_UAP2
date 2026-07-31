namespace BilanMatiereUAP2.Contracts.InventairesFonderie;

/// <summary>
/// Contrats du comptage physique d'inventaire, en KILOGRAMMES.
/// Les noms de champs reprennent ceux deja affiches par l'ecran Inventaire Fonderie
/// afin que sa disposition reste strictement inchangee.
/// </summary>
public sealed record InventaireConteneurResponse(
    Guid Id,
    DateOnly DateInventaire,
    int Mois,
    int Annee,
    string NumeroConteneur,
    string? OfDestination,
    string? CodeArticle,
    string? DesignationArticle,
    Guid TypeMatiereId,
    string CodeTypeMatiere,
    decimal QuantiteReelle,
    string Unite,
    string? Observation,
    string Statut);

public sealed record InventaireCategorieResponse(
    Guid Id,
    int Mois,
    int Annee,
    string Categorie,
    Guid TypeMatiereId,
    string CodeTypeMatiere,
    decimal QuantiteReelle,
    string Unite,
    string? Commentaire,
    string Statut,
    /// <summary>Une categorie comptee entre dans le Stock fin.</summary>
    bool InclusDansBilanFonderie,
    bool VisibleDansRapportInventaire);

public sealed record InventaireFourResponse(
    Guid Id,
    DateOnly DateInventaire,
    int Mois,
    int Annee,
    string Four,
    Guid TypeMatiereId,
    string CodeTypeMatiere,
    decimal Quantite,
    string Unite,
    string? Observation,
    string Statut,
    /// <summary>Toujours false : l'inventaire du four est exclu du bilan.</summary>
    bool InclusDansBilanFonderie,
    bool VisibleDansRapportInventaire);

// ----------------------------------------------------------------- requetes

public sealed record CreateInventaireConteneurRequest(
    DateOnly DateInventaire,
    string NumeroConteneur,
    string? OfDestination,
    string? CodeArticle,
    string? DesignationArticle,
    Guid TypeMatiereId,
    decimal QuantiteReelle,
    string? Observation);

public sealed record CreateInventaireCategorieRequest(
    int Mois,
    int Annee,
    string Categorie,
    Guid TypeMatiereId,
    decimal QuantiteReelle,
    string? Commentaire);

public sealed record CreateInventaireFourRequest(
    DateOnly DateInventaire,
    string Four,
    Guid TypeMatiereId,
    decimal Quantite,
    string? Observation);

// ----------------------------------------------------------------- totaux

/// <summary>
/// Totaux d'inventaire par type matiere, en kg.
/// <paramref name="TotalUtiliseBilan"/> est le STOCK FIN de la periode : parties 1 + 2.
/// <paramref name="TotalInventaireRapport"/> ajoute les fours, pour le rapport seulement.
/// </summary>
public sealed record InventaireTotalParMatiereResponse(
    Guid TypeMatiereId,
    string CodeTypeMatiere,
    decimal TotalPartie1,
    decimal TotalPartie2,
    decimal TotalPartie3,
    decimal TotalInventaireRapport,
    decimal TotalUtiliseBilan);

public sealed record InventaireComptagePeriodeResponse(
    string Periode,
    IReadOnlyList<InventaireConteneurResponse> Conteneurs,
    IReadOnlyList<InventaireCategorieResponse> Categories,
    IReadOnlyList<InventaireFourResponse> Fours,
    IReadOnlyList<InventaireTotalParMatiereResponse> Totaux);
