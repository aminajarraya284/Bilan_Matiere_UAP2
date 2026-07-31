using System.Text.Json.Serialization;

namespace BilanMatiereUAP2.Contracts;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total);

public sealed record ImportReferenceResponse(bool Duplicate, int Inserted, int Updated);

public sealed record ImportRapportMachineResponse(
    Guid ImportBatchId,
    bool Duplicate,
    int Inserted,
    int Updated,
    int Unchanged,
    int Rejected,
    int RowsRead,
    int RowsValid,
    int Anomalies,
    string Statut,
    string? ArchiveErrorMessage);

public sealed record RapportMachineResponse(
    Guid Id,
    DateOnly DateProduction,
    string CodePoste,
    string OrigineProcessus,
    string NumeroOperation,
    string CodeArticle,
    string? DesignationArticle,
    string NumeroOF,
    string? Equipe,
    decimal QuantiteTotale,
    decimal QuantiteRebut,
    string? AlternativeGamme,
    string CleMetier,
    bool IsActive);

public sealed record RapportMachineAggregateResponse(
    string NumeroOF,
    string CodeArticle,
    decimal TotalCtrlSci,
    decimal TotalCtrlEmr,
    decimal TotalCtrlTrn,
    decimal TotalRebutSci,
    decimal TotalRebutEmr,
    decimal TotalRebutTrn,
    int NombreLignes,
    int NombreLignesEmr,
    int NombreLignesNonMappees,
    DateOnly? PremiereDate,
    DateOnly? DerniereDate,
    bool Trouve);

public sealed record ReceptionU2ContextResponse(
    string NumeroOF,
    string? CodeArticle,
    string? DesignationArticle,
    string? SituationX3,
    string? AtelierOrigine,
    string? CodeTypeMatiere,
    decimal? QuantiteLienUtilisee,
    string? UniteQuantiteLien,
    decimal? PoidsTheoriqueUnitaireKg,
    decimal? PoidsTheoriqueTotalKg,
    decimal? PoidsPhysiqueUnitaireKg,
    Guid? ControlePoidsFonderieId,
    string? SourcePoidsPhysique,
    decimal? TotalCtrlSci,
    decimal? TotalCtrlEmr,
    decimal? TotalCtrlTrn,
    decimal? TotalRebutSci,
    decimal? TotalRebutEmr,
    decimal? TotalRebutTrn,
    decimal? PiecesReceptionneesU2,
    decimal? QuantiteNonReceptionnee,
    int NombreLignesRapportMachine,
    int NombreLignesRapportMachineNonMappees,
    string StatutArticle,
    string StatutAtelier,
    string StatutTypeMatiere,
    string StatutNomenclature,
    string StatutPoidsPhysique,
    string StatutRapportMachine,
    IReadOnlyList<string> Avertissements,
    string SourceTypeMatiere = "NonResolue",
    bool EligibleAtelier16 = false,
    string StatutContexte = "Incomplet",
    bool ExisteDansYrjmch = false,
    bool ExisteDansEntreeOf = false,
    bool ExisteDansSortieMp = false,
    bool ExisteDansEncoursProduct = false,
    IReadOnlyList<ReceptionControlePoidsCandidateResponse>? CandidatsControlePoids = null,
    string? MessageControlePoids = null,
    string? MessagePoidsTheorique = null)
{
    public string? CodeAtelier => AtelierOrigine;
}

public sealed record ReceptionControlePoidsCandidateResponse(
    Guid Id,
    DateOnly DateControle,
    string? NumeroOF,
    string CodeArticle,
    string CodeTypeMatiere,
    decimal PoidsPhysiqueUnitaireKg,
    string Statut);

public sealed record GammeReferenceResponse(
    string CodeArticle,
    string? DesignationArticle,
    DateOnly? DateFinValidite,
    string? EtapeSuivi,
    string? AlternativeGamme,
    string NumeroOperation,
    string CodeAtelier,
    string CodePoste,
    decimal? NombrePostes,
    string? GroupePosteCharge,
    string? DesignationOperation,
    string? UniteOperation,
    decimal? CoefficientOperationStock,
    decimal? TempsCycle,
    decimal? Trs,
    decimal? TempsAttente,
    string? Jalon,
    decimal? Quantite,
    decimal? Cadence,
    DateTime? DateModificationX3,
    decimal? TempsReglage,
    decimal? CoefficientCcpt,
    string CleMetier,
    bool IsActive,
    Guid ImportBatchId,
    string SourceSystem,
    DateTime ImportedAt);

public sealed record NomenclatureReferenceResponse(
    string CodeArticleParent,
    string? AlternativeNomenclature,
    string CodeParentNiveau,
    string CodeComposant,
    string Niveau,
    string Sequence,
    string? DesignationComposant,
    decimal? QuantiteLien,
    decimal? QuantiteLienCumulee,
    string? UniteQuantite,
    string? CategorieComposant,
    string? TypeLien,
    decimal? PourcentageRebut,
    string CleMetier,
    bool IsActive,
    Guid ImportBatchId,
    string SourceSystem,
    DateTime ImportedAt,
    DateOnly? DateFinValidite,
    string StatutValidite);
