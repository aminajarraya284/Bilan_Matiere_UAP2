using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Contracts.Bilans;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Features.Alertes;

public static class CodesAlertes
{
    public const string FonInventaireDebutManquant = "FON_INVENTAIRE_DEBUT_MANQUANT";
    public const string FonInventaireFinManquant = "FON_INVENTAIRE_FIN_MANQUANT";
    public const string FonSortieX3UniteNonConvertible = "FON_SORTIE_X3_UNITE_NON_CONVERTIBLE";
    public const string FonReceptionU2PoidsManquant = "FON_RECEPTION_U2_POIDS_MANQUANT";
    public const string FonReceptionU2TypeMatiereNonResolu = "FON_RECEPTION_U2_TYPE_MATIERE_NON_RESOLU";
    public const string U2QuantiteNonReceptionnee = "U2_QUANTITE_NON_RECEPTIONNEE";
    public const string U2ReceptionSuperieureEmr = "U2_RECEPTION_SUPERIEURE_EMR";
    public const string U2ArticleOfIntrouvable = "U2_ARTICLE_OF_INTROUVABLE";
    public const string U2EntreeOfIntrouvable = "U2_ENTREE_OF_INTROUVABLE";
    public const string U2EntreeOfUniteIncompatible = "U2_ENTREE_OF_UNITE_INCOMPATIBLE";
    public const string U2TypeMatiereIntrouvable = "U2_TYPE_MATIERE_INTROUVABLE";
    public const string U2PoidsPhysiqueManquant = "U2_POIDS_PHYSIQUE_MANQUANT";
    public const string U2PoidsTheoriqueManquant = "U2_POIDS_THEORIQUE_MANQUANT";
    public const string Uap2CtrlManquant = "UAP2_CTRL_MANQUANT";
    public const string Uap2EntreeOfIntrouvable = "UAP2_ENTREE_OF_INTROUVABLE";
    public const string Uap2EntreeOfUniteIncompatible = "UAP2_ENTREE_OF_UNITE_INCOMPATIBLE";
    public const string Uap2PoidsPhysiqueManquant = "UAP2_POIDS_PHYSIQUE_MANQUANT";
    public const string Uap2BilanAVerifier = "UAP2_BILAN_A_VERIFIER";
    public const string Uap2ParametreSeuilManquant = "UAP2_PARAMETRE_SEUIL_MANQUANT";
    public const string X3ImportEchec = "X3_IMPORT_ECHEC";
    public const string X3ImportPartiel = "X3_IMPORT_PARTIEL";
}

public static class SourcesAlertes
{
    public const string BilanFonderie = "Bilan Fonderie";
    public const string BilanUsinage2 = "Bilan Usinage 2";
    public const string BilanUap2 = "Bilan UAP2";
    public const string ImportX3 = "Import X3";
}

public sealed record AlerteDetection(
    string Code,
    TypeAlerte Type,
    SeveriteAlerte Severite,
    string Titre,
    string Description,
    string Source,
    string Periode,
    string? NumeroOF,
    string? CodeArticle,
    string? TypeMatiere,
    bool ResolutionAutomatique,
    IReadOnlyDictionary<string, string?> DonneesAssociees);

public interface IAlerteDetectionService
{
    Task<IReadOnlyList<AlerteDetection>> DetecterAsync(
        string periode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Traduit exclusivement les anomalies et statuts déjà produits par les bilans et
/// agrégats SQL existants. Aucun seuil ni calcul de bilan n'est reproduit ici.
/// </summary>
public sealed class AlerteDetectionService(
    IBilanFonderieService bilanFonderieService,
    IBilanUsinage2Service bilanUsinage2Service,
    IBilanUap2Service bilanUap2Service,
    IDashboardReadRepository dashboardReadRepository) : IAlerteDetectionService
{
    public async Task<IReadOnlyList<AlerteDetection>> DetecterAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        var premierJour = AlerteValidation.ParsePeriode(periode);

        // Même DbContext scoped : appels séquentiels obligatoires.
        var fonderie = await bilanFonderieService.GetAsync(periode, cancellationToken);
        var usinage2 = await bilanUsinage2Service.GetAsync(periode, cancellationToken);
        var uap2 = await bilanUap2Service.GetAsync(periode, cancellationToken);
        var imports = await dashboardReadRepository.GetImportsAsync(premierJour, cancellationToken);

        var detections = new List<AlerteDetection>();
        AjouterFonderie(detections, fonderie);
        AjouterUsinage2(detections, usinage2);
        AjouterUap2(detections, uap2);
        AjouterImports(detections, periode, imports);
        return detections;
    }

    private static void AjouterFonderie(ICollection<AlerteDetection> cible, BilanFonderieResponse bilan)
    {
        foreach (var ligne in bilan.Lignes)
        {
            if (Contient(ligne.Anomalies, BilanFonderieService.AnomalieStockDebut))
            {
                cible.Add(Creer(
                    CodesAlertes.FonInventaireDebutManquant,
                    TypeAlerte.DonneeManquante,
                    "Inventaire de début indisponible",
                    "Le Bilan Fonderie ne dispose pas du stock de début requis.",
                    SourcesAlertes.BilanFonderie,
                    bilan.Periode,
                    typeMatiere: ligne.CodeTypeMatiere,
                    donnees: Donnees(("unite", "kg"))));
            }

            if (Contient(ligne.Anomalies, BilanFonderieService.AnomalieStockFin))
            {
                cible.Add(Creer(
                    CodesAlertes.FonInventaireFinManquant,
                    TypeAlerte.DonneeManquante,
                    "Inventaire de fin indisponible",
                    "Le Bilan Fonderie ne dispose pas du stock de fin requis.",
                    SourcesAlertes.BilanFonderie,
                    bilan.Periode,
                    typeMatiere: ligne.CodeTypeMatiere,
                    donnees: Donnees(("unite", "kg"))));
            }

            var anomalieUnite = TrouverPrefixe(ligne.Anomalies, BilanFonderieService.AnomalieSortiesUnites);
            if (anomalieUnite is not null)
            {
                cible.Add(Creer(
                    CodesAlertes.FonSortieX3UniteNonConvertible,
                    TypeAlerte.AnomalieTechnique,
                    "Sortie X3 non convertible en kilogrammes",
                    "Une ou plusieurs sorties X3 ne sont pas exprimées en kg et sont exclues du total.",
                    SourcesAlertes.BilanFonderie,
                    bilan.Periode,
                    typeMatiere: ligne.CodeTypeMatiere,
                    donnees: Donnees(("uniteBilan", "kg"), ("anomalieSource", anomalieUnite))));
            }

            var anomaliePoids = TrouverPrefixe(ligne.Anomalies, BilanFonderieService.AnomalieReceptionsPoids);
            if (anomaliePoids is not null)
            {
                cible.Add(Creer(
                    CodesAlertes.FonReceptionU2PoidsManquant,
                    TypeAlerte.DonneeManquante,
                    "Poids de réception U2 manquant",
                    "Une réception U2 ne peut pas contribuer au Bilan Fonderie en kg faute de poids exploitable.",
                    SourcesAlertes.BilanFonderie,
                    bilan.Periode,
                    typeMatiere: ligne.CodeTypeMatiere,
                    donnees: Donnees(("unite", "kg"), ("anomalieSource", anomaliePoids))));
            }
        }

        var anomalieType = bilan.Lignes
            .SelectMany(ligne => ligne.Anomalies)
            .FirstOrDefault(anomalie => anomalie.StartsWith(
                BilanFonderieService.AnomalieReceptionsTypeMatiere,
                StringComparison.OrdinalIgnoreCase));
        if (anomalieType is not null)
        {
            cible.Add(Creer(
                CodesAlertes.FonReceptionU2TypeMatiereNonResolu,
                TypeAlerte.DonneeManquante,
                "Type matière de réception U2 non résolu",
                "Une réception U2 ne peut pas être affectée à un type matière du Bilan Fonderie.",
                SourcesAlertes.BilanFonderie,
                bilan.Periode,
                donnees: Donnees(("anomalieSource", anomalieType))));
        }
    }

    private static void AjouterUsinage2(ICollection<AlerteDetection> cible, BilanUsinage2Response bilan)
    {
        foreach (var ligne in bilan.Lignes)
        {
            if (ligne.QuantiteNonReceptionneePieces is > 0m)
            {
                cible.Add(CreerU2(
                    CodesAlertes.U2QuantiteNonReceptionnee,
                    TypeAlerte.AnomalieMetier,
                    "Quantité U2 non réceptionnée",
                    "Le service Bilan Usinage 2 signale une quantité non réceptionnée positive.",
                    bilan.Periode,
                    ligne,
                    Donnees(("unite", "pieces"), ("quantite", Decimal(ligne.QuantiteNonReceptionneePieces)))));
            }
            else if (ligne.QuantiteNonReceptionneePieces is < 0m)
            {
                cible.Add(CreerU2(
                    CodesAlertes.U2ReceptionSuperieureEmr,
                    TypeAlerte.AnomalieMetier,
                    "Réception U2 supérieure à l’EMR nette",
                    "Le service Bilan Usinage 2 signale une sur-réception.",
                    bilan.Periode,
                    ligne,
                    Donnees(("unite", "pieces"), ("ecartSigne", Decimal(ligne.QuantiteNonReceptionneePieces)))));
            }

            AjouterAnomalieU2(cible, bilan.Periode, ligne, "ARTICLE_OF_INTROUVABLE", CodesAlertes.U2ArticleOfIntrouvable,
                TypeAlerte.DonneeManquante, "Article principal de l’OF introuvable");
            AjouterAnomalieU2(cible, bilan.Periode, ligne, "ENTREE_OF_INTROUVABLE", CodesAlertes.U2EntreeOfIntrouvable,
                TypeAlerte.DonneeManquante, "Entrée OF introuvable");
            AjouterAnomalieU2(cible, bilan.Periode, ligne, "ENTREE_OF_UNITE_INCOMPATIBLE", CodesAlertes.U2EntreeOfUniteIncompatible,
                TypeAlerte.AnomalieTechnique, "Unité de l’entrée OF incompatible");
            AjouterAnomalieU2(cible, bilan.Periode, ligne, "TYPE_MATIERE_SORTIE_MP_INTROUVABLE", CodesAlertes.U2TypeMatiereIntrouvable,
                TypeAlerte.DonneeManquante, "Type matière de la sortie MP introuvable");

            if (ligne.EligibleAtelier16 && ligne.PoidsPhysiqueUnitaireKg is null)
            {
                cible.Add(CreerU2(
                    CodesAlertes.U2PoidsPhysiqueManquant,
                    TypeAlerte.DonneeManquante,
                    "Poids physique unitaire manquant",
                    "Aucun poids physique unitaire applicable n’est disponible pour cet OF.",
                    bilan.Periode,
                    ligne,
                    Donnees(("unite", "kg/piece"))));
            }

            if (ligne.EligibleAtelier16 && ligne.PoidsTheoriqueUnitaireKg is null)
            {
                cible.Add(CreerU2(
                    CodesAlertes.U2PoidsTheoriqueManquant,
                    TypeAlerte.DonneeManquante,
                    "Poids théorique unitaire manquant",
                    "Aucun poids théorique unitaire applicable n’est disponible pour cet OF.",
                    bilan.Periode,
                    ligne,
                    Donnees(("unite", "kg/piece"))));
            }
        }
    }

    private static void AjouterUap2(ICollection<AlerteDetection> cible, BilanUap2Response bilan)
    {
        foreach (var ligne in bilan.Lignes)
        {
            if (Contient(ligne.Anomalies, BilanUap2Service.AnomalieCtrlAbsent))
            {
                cible.Add(CreerUap2(
                    CodesAlertes.Uap2CtrlManquant,
                    TypeAlerte.DonneeManquante,
                    "Quantité CTRL de référence absente",
                    "La quantité CTRL requise comme référence du Bilan UAP2 est absente.",
                    bilan.Periode,
                    ligne,
                    Donnees(("unite", "pieces"))));
            }

            if (Contient(ligne.Anomalies, BilanUap2Service.AnomaliePoidsPhysique))
            {
                cible.Add(CreerUap2(
                    CodesAlertes.Uap2PoidsPhysiqueManquant,
                    TypeAlerte.DonneeManquante,
                    "Poids physique UAP2 non résolu",
                    "La conversion de la sortie MP en kg est impossible faute de poids physique unitaire.",
                    bilan.Periode,
                    ligne,
                    Donnees(("uniteSortie", "kg"), ("uniteEcart", "pieces"))));
            }

            if (Contient(ligne.Anomalies, BilanUap2Service.AnomalieEntreeOfIntrouvable))
            {
                cible.Add(CreerUap2(
                    CodesAlertes.Uap2EntreeOfIntrouvable,
                    TypeAlerte.DonneeManquante,
                    "Entrée OF UAP2 introuvable",
                    "Le calcul UAP2 est suspendu car aucune Entrée OF exploitable n’est disponible.",
                    bilan.Periode,
                    ligne,
                    Donnees(("unite", "UN"))));
            }

            if (Contient(ligne.Anomalies, BilanUap2Service.AnomalieEntreeOfUniteIncompatible))
            {
                cible.Add(CreerUap2(
                    CodesAlertes.Uap2EntreeOfUniteIncompatible,
                    TypeAlerte.AnomalieTechnique,
                    "Unité Entrée OF UAP2 incompatible",
                    "L’Entrée OF est ignorée car son unité n’est pas UN (pièces).",
                    bilan.Periode,
                    ligne,
                    Donnees(("uniteAttendue", "UN"))));
            }

            if (Contient(ligne.Anomalies, BilanUap2Service.AnomalieHorsSeuil))
            {
                cible.Add(CreerUap2(
                    CodesAlertes.Uap2BilanAVerifier,
                    TypeAlerte.AnomalieMetier,
                    "Bilan UAP2 à vérifier",
                    "Le service Bilan UAP2 a classé cette ligne À vérifier selon son paramètre métier existant.",
                    bilan.Periode,
                    ligne,
                    Donnees(("unite", "pieces"), ("statutSource", ligne.Statut))));
            }
        }

        if (bilan.Lignes.Any(ligne => Contient(ligne.Anomalies, BilanUap2Service.AnomalieParametreSeuil)))
        {
            cible.Add(Creer(
                CodesAlertes.Uap2ParametreSeuilManquant,
                TypeAlerte.AnomalieTechnique,
                "Paramètre du seuil UAP2 manquant",
                "Le service Bilan UAP2 signale l’absence du paramètre de seuil et utilise son comportement existant.",
                SourcesAlertes.BilanUap2,
                bilan.Periode,
                donnees: Donnees(("parametre", "SEUIL_ECART_UAP2"))));
        }
    }

    private static void AjouterImports(
        ICollection<AlerteDetection> cible,
        string periode,
        DashboardImportsData imports)
    {
        if (imports.Echoues > 0)
        {
            cible.Add(Creer(
                CodesAlertes.X3ImportEchec,
                TypeAlerte.AnomalieTechnique,
                "Import X3 échoué",
                "Un ou plusieurs imports ont le statut source Echec sur la période.",
                SourcesAlertes.ImportX3,
                periode,
                severite: SeveriteAlerte.Erreur,
                resolutionAutomatique: false,
                donnees: Donnees(("nombreImports", imports.Echoues.ToString(CultureInfo.InvariantCulture)))));
        }

        if (imports.Partiels > 0)
        {
            cible.Add(Creer(
                CodesAlertes.X3ImportPartiel,
                TypeAlerte.AnomalieTechnique,
                "Import X3 partiel",
                "Un ou plusieurs imports ont le statut source Partiel sur la période.",
                SourcesAlertes.ImportX3,
                periode,
                resolutionAutomatique: false,
                donnees: Donnees(("nombreImports", imports.Partiels.ToString(CultureInfo.InvariantCulture)))));
        }
    }

    private static void AjouterAnomalieU2(
        ICollection<AlerteDetection> cible,
        string periode,
        BilanUsinage2LigneResponse ligne,
        string anomalie,
        string code,
        TypeAlerte type,
        string titre)
    {
        if (!Contient(ligne.Anomalies, anomalie)) return;
        cible.Add(CreerU2(
            code,
            type,
            titre,
            $"Le service Bilan Usinage 2 signale l’anomalie source {anomalie}.",
            periode,
            ligne,
            Donnees(("anomalieSource", anomalie))));
    }

    private static AlerteDetection CreerU2(
        string code,
        TypeAlerte type,
        string titre,
        string description,
        string periode,
        BilanUsinage2LigneResponse ligne,
        IReadOnlyDictionary<string, string?> donnees)
        => Creer(
            code,
            type,
            titre,
            description,
            SourcesAlertes.BilanUsinage2,
            periode,
            ligne.NumeroOF,
            ligne.CodeArticle,
            ligne.TypeMatiere,
            donnees: donnees);

    private static AlerteDetection CreerUap2(
        string code,
        TypeAlerte type,
        string titre,
        string description,
        string periode,
        BilanUap2LigneResponse ligne,
        IReadOnlyDictionary<string, string?> donnees)
        => Creer(
            code,
            type,
            titre,
            description,
            SourcesAlertes.BilanUap2,
            periode,
            ligne.NumeroOF,
            ligne.CodeArticle,
            ligne.CodeTypeMatiere,
            donnees: donnees);

    private static AlerteDetection Creer(
        string code,
        TypeAlerte type,
        string titre,
        string description,
        string source,
        string periode,
        string? numeroOf = null,
        string? codeArticle = null,
        string? typeMatiere = null,
        SeveriteAlerte severite = SeveriteAlerte.Avertissement,
        bool resolutionAutomatique = true,
        IReadOnlyDictionary<string, string?>? donnees = null)
        => new(
            code,
            type,
            severite,
            titre,
            description,
            source,
            periode,
            numeroOf,
            codeArticle,
            typeMatiere,
            resolutionAutomatique,
            donnees ?? new Dictionary<string, string?>());

    private static bool Contient(IEnumerable<string> anomalies, string code)
        => anomalies.Any(anomalie => string.Equals(anomalie, code, StringComparison.OrdinalIgnoreCase));

    private static string? TrouverPrefixe(IEnumerable<string> anomalies, string code)
        => anomalies.FirstOrDefault(anomalie => anomalie.StartsWith(code, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyDictionary<string, string?> Donnees(params (string Cle, string? Valeur)[] valeurs)
        => valeurs.ToDictionary(item => item.Cle, item => item.Valeur, StringComparer.Ordinal);

    private static string? Decimal(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture);
}
