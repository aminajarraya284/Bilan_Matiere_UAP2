using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Contracts.InventairesFonderie;

// --- Partie 1 : Conteneurs / OF ---
public sealed record CreateInventaireConteneurRequest(
    DateOnly DateInventaire,
    string NumeroConteneur,
    string NumeroOF,
    string CodeTypeMatiere,
    decimal PoidsReelKg,
    string? Observation);

public sealed record UpdateInventaireConteneurRequest(
    DateOnly DateInventaire,
    string NumeroConteneur,
    string NumeroOF,
    string CodeTypeMatiere,
    decimal PoidsReelKg,
    string? Observation);

public sealed record InventaireConteneurResponse(
    Guid Id,
    DateOnly DateInventaire,
    string NumeroConteneur,
    string NumeroOF,
    string CodeTypeMatiere,
    decimal PoidsReelKg,
    string? Observation,
    string Statut,
    DateTime CreeLeUtc,
    DateTime? ModifieLeUtc,
    DateTime? ValideLeUtc,
    string? ValidePar,
    DateTime? AnnuleLeUtc,
    string? AnnulePar);

public sealed record PagedInventairesConteneursResponse(
    IReadOnlyList<InventaireConteneurResponse> Items,
    int Total,
    int Page,
    int PageSize);

// --- Partie 2 : Catégories ---
public sealed record CreateInventaireCategorieRequest(
    DateOnly DateInventaire,
    CategorieInventaireFonderie Categorie,
    string CodeTypeMatiere,
    decimal PoidsReelKg,
    string? Commentaire);

public sealed record UpdateInventaireCategorieRequest(
    DateOnly DateInventaire,
    CategorieInventaireFonderie Categorie,
    string CodeTypeMatiere,
    decimal PoidsReelKg,
    string? Commentaire);

public sealed record InventaireCategorieResponse(
    Guid Id,
    DateOnly DateInventaire,
    string Categorie,
    string CodeTypeMatiere,
    decimal PoidsReelKg,
    string? Commentaire,
    string Statut,
    bool InclusBilanFonderie,
    bool VisibleRapportInventaire,
    DateTime CreeLeUtc,
    DateTime? ModifieLeUtc,
    DateTime? ValideLeUtc,
    string? ValidePar,
    DateTime? AnnuleLeUtc,
    string? AnnulePar);

public sealed record PagedInventairesCategoriesResponse(
    IReadOnlyList<InventaireCategorieResponse> Items,
    int Total,
    int Page,
    int PageSize);

// --- Partie 3 : Fours ---
public sealed record CreateInventaireFourRequest(
    DateOnly DateInventaire,
    string CodeFour,
    string CodeTypeMatiere,
    decimal QuantiteKg,
    string? Observation);

public sealed record UpdateInventaireFourRequest(
    DateOnly DateInventaire,
    string CodeFour,
    string CodeTypeMatiere,
    decimal QuantiteKg,
    string? Observation);

public sealed record InventaireFourResponse(
    Guid Id,
    DateOnly DateInventaire,
    int Mois,
    int Annee,
    string CodeFour,
    string CodeTypeMatiere,
    decimal QuantiteKg,
    string? Observation,
    string Statut,
    bool InclusBilanFonderie,
    bool VisibleRapportInventaire,
    DateTime CreeLeUtc,
    DateTime? ModifieLeUtc,
    DateTime? ValideLeUtc,
    string? ValidePar,
    DateTime? AnnuleLeUtc,
    string? AnnulePar);

public sealed record PagedInventairesFoursResponse(
    IReadOnlyList<InventaireFourResponse> Items,
    int Total,
    int Page,
    int PageSize);

// --- Synthèse ---
public sealed record InventaireSyntheseLigneDto(
    string CodeTypeMatiere,
    decimal Partie1Kg,
    decimal Partie2Kg,
    decimal Partie3FourKg,
    decimal TotalRapportInventaireKg,
    decimal TotalBilanMatiereKg,
    string Statut);

public sealed record InventaireSyntheseTotauxDto(
    decimal Partie1Kg,
    decimal Partie2Kg,
    decimal Partie3FourKg,
    decimal TotalRapportInventaireKg,
    decimal TotalBilanMatiereKg);

public sealed record InventaireSyntheseDto(
    int Mois,
    int Annee,
    IReadOnlyCollection<InventaireSyntheseLigneDto> Lignes,
    InventaireSyntheseTotauxDto TotalGeneral);

// --- Rétrocompatibilité anciens endpoints d'inventaire fonderie ---
public sealed record CreateInventaireFonderieRequest(
    string Periode,
    Guid TypeMatiereId,
    decimal? StockDebutKg,
    decimal StockFinKg,
    string? Observation);

public sealed record UpdateInventaireFonderieRequest(
    string Periode,
    Guid TypeMatiereId,
    decimal? StockDebutKg,
    decimal StockFinKg,
    string? Observation);

public sealed record InventaireFonderieResponse(
    Guid Id,
    string Periode,
    Guid TypeMatiereId,
    string TypeMatiereCode,
    string TypeMatiereLibelle,
    decimal? StockDebutKg,
    decimal StockFinKg,
    string? Observation,
    string Statut,
    string? CreePar,
    DateTime CreeLeUtc,
    string? ModifiePar,
    DateTime? ModifieLeUtc,
    string? ValidePar,
    DateTime? ValideLeUtc,
    string? AnnulePar,
    DateTime? AnnuleLeUtc);

public sealed record PagedInventairesFonderieResponse(
    IReadOnlyList<InventaireFonderieResponse> Items,
    int Total,
    int Page,
    int PageSize);
