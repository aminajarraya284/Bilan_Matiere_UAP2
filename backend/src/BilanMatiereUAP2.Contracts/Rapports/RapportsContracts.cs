using BilanMatiereUAP2.Contracts.Alertes;
using BilanMatiereUAP2.Contracts.Bilans;

namespace BilanMatiereUAP2.Contracts.Rapports;

public static class CodesRapports
{
    public const string BilanFonderie = "RPT-001";
    public const string BilanUsinage2 = "RPT-002";
    public const string BilanUap2 = "RPT-003";
    public const string DetailOf = "RPT-004";
    public const string Alertes = "RPT-005";
    public const string InventaireFonderie = "RPT-006";
    public const string HistoriqueImports = "RPT-007";
    public const string ReceptionsNonReceptionnees = "RPT-008";
}

public sealed record RapportCatalogueItemResponse(
    string Code,
    string Titre,
    string Description,
    string Module,
    string? UnitePrincipale,
    IReadOnlyList<string> Filtres,
    IReadOnlyList<string> Formats,
    bool Disponible,
    string? MotifIndisponibilite);

public sealed record RapportMetadataResponse(
    string Code,
    string Titre,
    DateTime GenereLeUtc,
    string? Periode,
    string? UnitePrincipale,
    bool EstPartiel,
    IReadOnlyList<string> Avertissements,
    string? GenerePar,
    IReadOnlyList<string> Unites,
    IReadOnlyList<string> Sources,
    IReadOnlyDictionary<string, string?> FiltresAppliques);

public sealed record RapportResponse<T>(
    RapportMetadataResponse Metadata,
    T Donnees);

public sealed record RapportImportLigneResponse(
    Guid Id,
    string TypeImport,
    string? ProfilImport,
    string Statut,
    string? NomFichier,
    DateTime DateCreationUtc,
    DateTime? DateFinUtc,
    int LignesLues,
    int LignesValides,
    int LignesInserees,
    int LignesMisesAJour,
    int LignesImportees,
    int LignesRejetees,
    int NombreAnomalies,
    string? CreePar,
    bool ArchivageEnErreur);

public sealed record PagedRapportImportsResponse(
    IReadOnlyList<RapportImportLigneResponse> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record RapportOfContexteResponse(
    string NumeroOF,
    string? CodeArticle,
    string? DesignationArticle,
    string? TypeMatiere,
    string? CodeAtelier,
    string? Groupe,
    bool EligibleCalculUap2,
    string StatutRapprochement,
    IReadOnlyList<string> Anomalies);

public sealed record RapportOfReceptionResponse(
    Guid Id,
    DateOnly DateReception,
    decimal NombrePiecesReceptionnees,
    decimal? PoidsTotalReceptionneKg,
    string? CodeArticle,
    string? TypeMatiere,
    string? AtelierOrigine,
    string Statut);

public sealed record RapportOfProcessusResponse(
    Guid Id,
    DateOnly DateProduction,
    string CodePoste,
    string Processus,
    string NumeroOperation,
    string CodeArticle,
    decimal QuantiteCtrlPieces,
    decimal QuantiteRebutPieces);

public sealed record RapportOfMouvementResponse(
    Guid Id,
    DateOnly DateImputation,
    string? CodeArticle,
    decimal QuantiteSignee,
    string? Unite,
    string? Transaction,
    string? TypePiece,
    string? PieceOrigine,
    string Nature,
    string StatutRapprochement);

public sealed record RapportOfControlePoidsResponse(
    Guid Id,
    DateOnly DateControle,
    string CodeArticle,
    string TypeMatiere,
    decimal PoidsPhysiqueUnitaireKg,
    string Statut);

public sealed record RapportOfReferenceResponse(
    string TypeReference,
    string CodeArticle,
    string? CodeComposant,
    string? NumeroOperation,
    string? CodeAtelier,
    string? CodePoste,
    decimal? QuantiteLien,
    string? UniteQuantite);

public sealed record RapportOfDetailResponse(
    RapportMetadataResponse Metadata,
    RapportOfContexteResponse Contexte,
    IReadOnlyList<RapportOfReceptionResponse> Receptions,
    IReadOnlyList<RapportOfProcessusResponse> Processus,
    IReadOnlyList<RapportOfMouvementResponse> Mouvements,
    IReadOnlyList<RapportOfControlePoidsResponse> ControlesPoids,
    IReadOnlyList<RapportOfReferenceResponse> References,
    BilanUsinage2LigneResponse? BilanUsinage2,
    BilanUap2LigneResponse? BilanUap2,
    IReadOnlyList<AlerteListItemResponse> Alertes,
    IReadOnlyList<string> DonneesManquantes);
