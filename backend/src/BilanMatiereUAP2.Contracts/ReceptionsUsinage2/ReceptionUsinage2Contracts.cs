namespace BilanMatiereUAP2.Contracts.ReceptionsUsinage2;

public sealed record CreateReceptionUsinage2Request(
    string NumeroOF,
    DateOnly DateReception,
    decimal NombrePiecesReceptionnees,
    decimal? PoidsTotalReceptionneKg,
    string? Origine,
    string? Observation);

public sealed record UpdateReceptionUsinage2Request(
    decimal NombrePiecesReceptionnees,
    decimal? PoidsTotalReceptionneKg,
    string? Origine,
    string? Observation);

public sealed record ValiderReceptionUsinage2Request(
    bool DerogationAdmin,
    string? DerogationJustification);

public sealed record ReceptionUsinage2Response(
    Guid Id,
    string NumeroOF,
    DateOnly DateReception,
    decimal NombrePiecesReceptionnees,
    decimal? PoidsTotalReceptionneKg,
    string? Origine,
    string? Observation,
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
    decimal? EcartPoidsUnitaireKg,
    decimal? TauxEcartPoidsPct,
    bool? EcartPoidsDepasseSeuil,
    decimal? TotalCtrlSci,
    decimal? TotalCtrlEmr,
    decimal? TotalCtrlTrn,
    decimal? TotalRebutSci,
    decimal? TotalRebutEmr,
    decimal? TotalRebutTrn,
    decimal? TotalPiecesReceptionneesU2MoisCourant,
    decimal? QuantiteNonReceptionnee,
    string StatutArticle,
    string StatutAtelier,
    string StatutTypeMatiere,
    string StatutNomenclature,
    string StatutPoidsPhysique,
    string StatutRapportMachine,
    IReadOnlyList<string> Avertissements,
    string StatutReception,
    string? DerogationJustification,
    string? CreePar,
    DateTime DateCreationUtc,
    string? ModifiePar,
    DateTime? DateModificationUtc,
    string SourceTypeMatiere = "NonResolue",
    bool EligibleAtelier16 = false)
{
    public string? CodeAtelier => AtelierOrigine;
}

public sealed record PagedReceptionsUsinage2Response(
    IReadOnlyList<ReceptionUsinage2Response> Items,
    int Total,
    int Page,
    int PageSize);
