namespace BilanMatiereUAP2.Contracts.Bilans;

/// <summary>
/// Une ligne de Bilan Usinage 2, pour une cle « Numero OF + Code article » et une periode.
///
/// CONVENTION D'UNITES, stricte : chaque champ porte son unite dans son nom.
///   * suffixe <c>Pieces</c>   -> quantite en pieces ;
///   * suffixe <c>Kg</c>       -> poids total en kilogrammes ;
///   * suffixe <c>UnitaireKg</c> -> poids unitaire en kilogrammes PAR PIECE ;
///   * suffixe <c>Pourcent</c> -> pourcentage.
/// Les pieces et les kilogrammes ne sont jamais melanges dans un meme calcul :
/// le controle des poids n'intervient pas dans le Bilan Usinage 2 en pieces.
/// </summary>
public sealed record BilanUsinage2LigneResponse(
    string NumeroOF,
    string CodeArticle,
    string? DesignationArticle,
    string? AtelierOrigine,
    string? TypeMatiere,
    string Periode,

    // --- Bloc pieces -------------------------------------------------------
    /// <summary>Somme des quantites CTRL des lignes YRJMCH classees EMR, sur la periode.</summary>
    decimal? QuantiteCtrlEmrPieces,
    /// <summary>Somme des rebuts des lignes YRJMCH classees EMR, sur la periode.</summary>
    decimal? RebutEmrPieces,
    /// <summary>QuantiteCtrlEmrPieces - RebutEmrPieces. Remplace l'ancien « EMR nette ».</summary>
    decimal? NombreDePieces,
    /// <summary>Somme des receptions U2 VALIDEES de la periode (brouillons et annulees exclus).</summary>
    decimal QuantiteReceptionneePieces,
    /// <summary>NombreDePieces - QuantiteReceptionneePieces. Jamais de Math.Abs, jamais ramene a zero.</summary>
    decimal? QuantiteNonReceptionneePieces,
    /// <summary>Somme des rebuts Usinage 2 VALIDES de la periode.</summary>
    decimal RebutUsinage2Pieces,
    /// <summary>Somme des quantites Entree OF SIGNEES (MVTSTOTRSVCR). Jamais de Math.Abs.</summary>
    decimal? EntreeOfPieces,
    /// <summary>QuantiteReceptionneePieces - RebutUsinage2Pieces - EntreeOfPieces.</summary>
    decimal? BilanUsinage2Pieces,

    // --- Taux (pourcentages, jamais integres au bilan en pieces) -----------
    decimal? TauxReceptionPourcent,
    decimal? TauxNonReceptionPourcent,

    // --- Bloc controle des poids -------------------------------------------
    /// <summary>Poids total mesure a la reception, en kg (somme des receptions validees).</summary>
    decimal? PoidsReceptionneTotalKg,
    /// <summary>Poids unitaire issu du Controle poids Fonderie valide, en kg/piece.</summary>
    decimal? PoidsPhysiqueUnitaireKg,
    /// <summary>Poids unitaire theorique issu de la nomenclature, en kg/piece.</summary>
    decimal? PoidsTheoriqueUnitaireKg,
    decimal? EcartPoidsUnitaireKg,
    decimal? TauxEcartPoidsPourcent,

    string StatutReception,
    string StatutPoids,
    IReadOnlyList<string> Anomalies,
    bool EligibleAtelier16 = false,
    string Statut = "Incomplet")
{
    public string? CodeAtelier => AtelierOrigine;
}

public sealed record BilanUsinage2Response(
    string Periode,
    IReadOnlyList<BilanUsinage2LigneResponse> Lignes,
    /// <summary>Quantite totale receptionnee U2 sur la periode, en pieces.</summary>
    decimal QuantiteTotaleReceptionneePieces,
    int NombreLignes,
    int NombreLignesDonneesManquantes,
    decimal TotalRebutUsinage2Pieces = 0,
    decimal TotalEntreeOfPieces = 0,
    decimal? TotalBilanUsinage2Pieces = null,
    int NombreAnomalies = 0,
    int Total = 0,
    int Page = 1,
    int PageSize = 50,
    int NombreLignesOk = 0,
    int NombreLignesAVerifier = 0,
    int NombreLignesExclues = 0,
    int NombreOf = 0,
    int NombreOfNonReceptionnes = 0,
    int NombreReferencesManquantes = 0);
