using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Infrastructure.Services;

namespace BilanMatiereUAP2.UnitTests.Infrastructure;

/// <summary>
/// Garde-fou de structure du lecteur GAMMEFAB.
///
/// Contexte : un etat MVTSTOTRSVCR a ete importe via l'endpoint GAMMEFAB et a insere
/// 78 410 lignes de mouvements de stock dans RefGammesFabrication. Le lecteur lisait des
/// colonnes par index sans jamais verifier que le classeur ETAIT une gamme.
///
/// Les en-tetes utilises ici sont ceux des fichiers X3 REELS :
///   * GAMMEFAB (34) (1).xls  : 22 colonnes, en-tete en ligne 1 ;
///   * MVTSTOTRSVCR (9).xls   : 28 colonnes, en-tete en ligne 10.
/// </summary>
public sealed class GammeFabricationExcelReaderTests
{
    private static readonly Guid BatchId = Guid.NewGuid();

    // En-tete GAMMEFAB reel. Les libelles sont multi-lignes dans le fichier d'origine.
    private static object?[] EnteteGammefab() =>
    [
        "ETP\nSuivi", "No\nOpér", "Nb\nPostes", "Unité\nOp", "Coef.\nOp/Sto", "Article",
        "Désignation", "DatFVal", "Atelier", "Poste Trv", "Grp PC.", "Désignation opér",
        "Alt", "T cycle", "TRS", "Att", "Jalon", "Qté", "Cad.", "D.Mod", "TR", "CCPT"
    ];

    // Ligne de donnees reelle. L'ordre des DONNEES differe de l'ordre des LIBELLES :
    // Article, Désignation, DatFVal, ETP Suivi, No Opér, Atelier, Poste Trv, Nb Postes, Grp PC...
    private static object?[] LigneGammefab(string article = "42A5203-1", string atelier = "16") =>
    [
        article, "RONDELLE D25", "31/12/1599 00:00:00", "2", "5", atelier,
        "TOU39", "1", "TOU3940", "USINAGE", "1", "UN",
        null, null, null, null, null, null, null, null, null, null
    ];

    // En-tete MVTSTOTRSVCR reel.
    private static object?[] EnteteMvt() =>
    [
        "Date imput", "Code", "Désignation", "Date crea", "Heure crea", "Utilisateur",
        "Quantité", "Unité", "Transaction", "Type Pce", "Lot", "Pièce Origine"
    ];

    // Ligne MVT reelle : c'est exactement ce qui avait pollue RefGammesFabrication.
    private static object?[] LigneMvt() =>
    [
        "06/02/2026 00:00:00", "0103A03-1", "VANNE BS 1/2\"", "04/03/2026 00:00:00", "0905",
        "FDAHM", "-980", "UN", "Entrée OF", "Déclaration production", "", "OF25120001"
    ];

    private static IReadOnlyList<ExcelWorkbook.Sheet> Classeur(params object?[][] lignes)
        => [new ExcelWorkbook.Sheet("Sheet1", lignes)];

    private static IReadOnlyList<RefGammeFabrication> Lire(IReadOnlyList<ExcelWorkbook.Sheet> feuilles)
        => GammeFabricationExcelReader.LireFeuilles(feuilles, "X3", BatchId);

    // ------------------------------------------------------------------ acceptations

    [Fact]
    public void FichierGammefabReel_EstAccepte()
    {
        var gammes = Lire(Classeur(EnteteGammefab(), LigneGammefab()));

        var gamme = Assert.Single(gammes);
        Assert.Equal("42A5203-1", gamme.CodeArticle);
        Assert.Equal("16", gamme.CodeAtelier);
        Assert.Equal("TOU39", gamme.CodePoste);
        Assert.Equal("TOU3940", gamme.GroupePosteCharge);
    }

    [Fact]
    public void LigneDEnteteDecalee_EstTrouveeEtLesTitresSontIgnores()
    {
        // Bloc de titre et de criteres avant l'en-tete, comme dans les exports X3.
        var gammes = Lire(Classeur(
            ["Gammes de fabrication", "06/07/2026", null],
            ["SELECTIONS :", null, null],
            [null, null, null],
            EnteteGammefab(),
            LigneGammefab()));

        Assert.Single(gammes);
    }

    [Fact]
    public void EnteteSansAccents_EstNormalisee()
    {
        var entete = EnteteGammefab();
        entete[6] = "Designation";
        entete[11] = "Designation oper";
        entete[8] = "ATELIER";

        Assert.Single(Lire(Classeur(entete, LigneGammefab())));
    }

    [Fact]
    public void EnteteAvecEspacesInsecablesEtPonctuation_EstNormalisee()
    {
        var entete = EnteteGammefab();
        entete[5] = "  Article  ";          // espace insecable + espaces multiples
        entete[9] = "Poste Trv.";           // espace fine insecable + point
        entete[10] = "Grp   PC";                 // espaces multiples

        Assert.Single(Lire(Classeur(entete, LigneGammefab())));
    }

    [Fact]
    public void LignesVidesApresLEntete_SontIgnorees()
    {
        var gammes = Lire(Classeur(
            EnteteGammefab(),
            LigneGammefab(),
            [null, null, null],
            ["", "", ""],
            LigneGammefab("0250303-1")));

        Assert.Equal(2, gammes.Count);
    }

    // ------------------------------------------------------------------ rejets

    [Fact]
    public void FichierMvtPresenteCommeGammefab_EstRejete()
    {
        // Reproduction fidele du fichier fautif : titre, criteres, puis en-tete MVT.
        var erreur = Assert.Throws<ImportFileException>(() => Lire(Classeur(
            ["Mvt de stock par transaction", "06/07/2026 00:00:00", null],
            ["SELECTIONS :", null, null],
            ["CRITERE(S) DE TRI :", null, null],
            EnteteMvt(),
            LigneMvt())));

        Assert.Contains("FICHIER_GAMMEFAB_STRUCTURE_INVALIDE", erreur.Message);
        Assert.Contains("MVTSTOTRSVCR", erreur.Message);
    }

    [Fact]
    public void FichierMvt_NeProduitAucuneLigneAInserer()
    {
        // Le rejet doit survenir AVANT toute construction d'entite : aucune ecriture SQL
        // ne peut donc avoir lieu, l'import etant enveloppe dans une transaction.
        Assert.Throws<ImportFileException>(() => Lire(Classeur(EnteteMvt(), LigneMvt())));
    }

    [Fact]
    public void ColonneArticleAbsente_EstRejetee()
    {
        var entete = EnteteGammefab();
        entete[5] = null;   // Article
        entete[7] = null;   // DatFVal
        entete[8] = null;   // Atelier
        entete[9] = null;   // Poste Trv
        entete[10] = null;  // Grp PC.
        entete[11] = null;  // Désignation opér

        var erreur = Assert.Throws<ImportFileException>(() => Lire(Classeur(entete, LigneGammefab())));

        Assert.Contains("FICHIER_GAMMEFAB_STRUCTURE_INVALIDE", erreur.Message);
    }

    [Fact]
    public void ClasseurSansAucunEntete_EstRejete()
    {
        var erreur = Assert.Throws<ImportFileException>(() => Lire(Classeur(
            ["valeur", "valeur", "valeur"],
            ["autre", "autre", "autre"])));

        Assert.Contains("FICHIER_GAMMEFAB_STRUCTURE_INVALIDE", erreur.Message);
        Assert.Contains("Aucune signature MVTSTOTRSVCR", erreur.Message);
    }

    [Fact]
    public void MessageDErreur_EstStructureEtExploitable()
    {
        var erreur = Assert.Throws<ImportFileException>(() => Lire(Classeur(EnteteMvt(), LigneMvt())));

        Assert.Contains("Feuille: Sheet1", erreur.Message);          // feuille inspectee
        Assert.Contains("Lignes testées", erreur.Message);           // fenetre de detection
        Assert.Contains("Colonnes trouvées", erreur.Message);        // colonnes vues
        Assert.Contains("Colonnes attendues absentes", erreur.Message); // colonnes manquantes
    }
}
