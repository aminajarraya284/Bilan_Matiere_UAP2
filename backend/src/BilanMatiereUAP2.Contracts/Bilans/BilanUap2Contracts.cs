namespace BilanMatiereUAP2.Contracts.Bilans;

/// <summary>
/// Une ligne de Bilan UAP2, pour une cle « Numero OF + Code article » et une periode.
///
/// CONVENTION D'UNITES, stricte : chaque champ porte son unite dans son nom.
/// La PARTIE 2 (ecart) est integralement EN PIECES : aucune conversion n'y intervient,
/// donc aucun risque de melanger pieces et kilogrammes (RM-COM-005). Seule la PARTIE 1
/// convertit en kilogrammes, via le poids physique unitaire du Controle poids Fonderie.
///
/// CTRL est la QUANTITE DE REFERENCE (poste de code exact « CTRL », colonne H de
/// YRJMCH), et non un quatrieme processus qui s'ajouterait a SCI, EMR et TRN.
/// </summary>
public sealed record BilanUap2LigneResponse(
    string NumeroOF,
    string CodeArticle,
    string? DesignationArticle,
    string? CodeTypeMatiere,
    string Periode,

    // --- Partie 1 : sortie matiere (RM-UAP2-001) ---------------------------
    /// <summary>Somme des quantites controlees (colonne H) des lignes de poste « CTRL ».</summary>
    decimal QuantiteCtrlPieces,
    /// <summary>Somme des rebuts (colonne I) du processus SCI.</summary>
    decimal RebutSciPieces,
    decimal RebutEmrPieces,
    decimal RebutTrnPieces,
    /// <summary>QuantiteCtrlPieces - (RebutSci + RebutEmr + RebutTrn).</summary>
    decimal QuantiteSortieMpPieces,
    /// <summary>Poids unitaire issu du Controle poids Fonderie valide, en kg/piece.</summary>
    decimal? PoidsPhysiqueUnitaireKg,
    /// <summary>QuantiteSortieMpPieces x PoidsPhysiqueUnitaireKg. Null si le poids manque.</summary>
    decimal? SortieMpKg,

    // --- Partie 2 : ecart UAP2, EN PIECES (RM-UAP2-002) --------------------
    /// <summary>Quantite nette signee des entrees OF de la periode, en pieces.</summary>
    decimal? EntreeOfPieces,
    /// <summary>Rebuts Usinage 2 VALIDES de la periode, en pieces.</summary>
    decimal RebutUsinage2Pieces,
    /// <summary>
    /// QuantiteCtrl - (RebutSci + RebutEmr + RebutTrn + EntreeOf + RebutU2).
    /// Jamais de Math.Abs : le signe distingue un manquant d'un excedent.
    /// </summary>
    decimal? EcartUap2Pieces,
    /// <summary>EcartUap2Pieces / QuantiteCtrlPieces x 100. Null si la reference est nulle.</summary>
    decimal? TauxEcartPourcent,

    IReadOnlyList<string> Anomalies,
    /// <summary><c>OK</c>, <c>AVerifier</c>, <c>Anomalie</c> ou <c>DonneesManquantes</c>.</summary>
    string Statut);

public sealed record BilanUap2Response(
    string Periode,
    IReadOnlyList<BilanUap2LigneResponse> Lignes,
    decimal TotalQuantiteCtrlPieces,
    decimal? TotalEcartUap2Pieces,
    /// <summary>Null des qu'une ligne n'a pas de poids physique : un total partiel tromperait.</summary>
    decimal? TotalSortieMpKg,
    /// <summary>Seuil applique, issu du parametre <c>SEUIL_ECART_UAP2</c>.</summary>
    decimal SeuilEcartPourcent,
    int NombreLignes,
    int NombreLignesHorsSeuil,
    int NombreLignesDonneesManquantes,
    int NombreAnomalies,
    int Total = 0,
    int Page = 1,
    int PageSize = 50);
