namespace BilanMatiereUAP2.Contracts.InventairesFonderie;

// Les contrats de comptage sont distincts des contrats historiques *Of/*Categorie/*Four.
// Cette séparation rend explicite que les nouveaux endpoints écrivent dans les tables
// InventairesConteneursComptage, InventairesCategoriesComptage et InventairesFoursComptage.
public sealed record CreateInventaireComptageConteneurRequest(
    DateOnly DateInventaire,
    string NumeroConteneur,
    string? NumeroOF,
    string? CodeArticle,
    string? DesignationArticle,
    Guid TypeMatiereId,
    decimal QuantiteKg,
    string? Observation);

public sealed record CreateInventaireComptageCategorieRequest(
    DateOnly Periode,
    string Categorie,
    Guid TypeMatiereId,
    decimal QuantiteKg,
    string? Observation);

public sealed record CreateInventaireComptageFourRequest(
    DateOnly DateInventaire,
    string CodeFour,
    Guid TypeMatiereId,
    decimal QuantiteKg,
    string? Observation);

public sealed record InventaireComptageConteneurResponse(
    Guid Id,
    DateOnly Periode,
    DateOnly DateInventaire,
    string NumeroConteneur,
    string? NumeroOF,
    string? CodeArticle,
    string? DesignationArticle,
    Guid TypeMatiereId,
    string CodeTypeMatiere,
    decimal QuantiteKg,
    string? Observation,
    string Statut,
    DateTime CreeLeUtc,
    DateTime? ModifieLeUtc,
    DateTime? ValideLeUtc,
    string? ValidePar);

public sealed record InventaireComptageCategorieResponse(
    Guid Id,
    DateOnly Periode,
    string Categorie,
    Guid TypeMatiereId,
    string CodeTypeMatiere,
    decimal QuantiteKg,
    string? Observation,
    string Statut,
    bool InclusBilanFonderie,
    bool VisibleRapportInventaire,
    DateTime CreeLeUtc,
    DateTime? ModifieLeUtc,
    DateTime? ValideLeUtc,
    string? ValidePar);

public sealed record InventaireComptageFourResponse(
    Guid Id,
    DateOnly Periode,
    DateOnly DateInventaire,
    string CodeFour,
    Guid TypeMatiereId,
    string CodeTypeMatiere,
    decimal QuantiteKg,
    string? Observation,
    string Statut,
    bool InclusBilanFonderie,
    bool VisibleRapportInventaire,
    DateTime CreeLeUtc,
    DateTime? ModifieLeUtc,
    DateTime? ValideLeUtc,
    string? ValidePar);

public sealed record InventaireComptageTotalResponse(
    Guid TypeMatiereId,
    string CodeTypeMatiere,
    decimal? StockDebutKg,
    decimal StockFinKg,
    decimal TotalFoursKg,
    decimal TotalUtiliseBilanKg,
    decimal TotalInventaireRapportKg,
    IReadOnlyList<string> Anomalies);

// Forme historique conservée pour les consommateurs qui lisaient déjà Totaux.
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
    IReadOnlyList<InventaireComptageConteneurResponse> Conteneurs,
    IReadOnlyList<InventaireComptageCategorieResponse> Categories,
    IReadOnlyList<InventaireComptageFourResponse> Fours,
    IReadOnlyList<InventaireTotalParMatiereResponse> Totaux,
    IReadOnlyList<InventaireComptageTotalResponse>? TotauxParTypeMatiere = null);
