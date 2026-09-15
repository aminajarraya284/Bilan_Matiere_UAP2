using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Infrastructure.Services;

/// <summary>
/// Signatures d'en-tete relevees sur les fichiers X3 REELS presents dans les archives
/// d'import. Elles ne sont pas devinees : chaque libelle ci-dessous a ete observe.
///
/// GAMMEFAB « GAMMEFAB (34) (1).xls » : 22 colonnes, en-tete en ligne 1.
/// MVTSTOTRSVCR « MVTSTOTRSVCR (9).xls » : 28 colonnes, en-tete en ligne 10,
/// precedee d'un bloc de titre et de criteres de selection.
/// </summary>
internal static class GammeFabricationSignature
{
    /// <summary>Libelles normalises attendus dans l'en-tete d'un export GAMMEFAB.</summary>
    public static readonly string[] Attendus =
    [
        "article", "designation", "datfval", "etpsuivi", "nooper", "nbpostes",
        "uniteop", "atelier", "postetrv", "grppc", "designationoper"
    ];

    /// <summary>
    /// Nombre minimal de libelles GAMMEFAB devant etre reconnus. Volontairement
    /// inferieur au total pour tolerer une variante d'export, mais suffisamment eleve
    /// pour qu'aucun autre etat X3 ne puisse l'atteindre par hasard.
    /// </summary>
    public const int MinCorrespondances = 6;

    /// <summary>
    /// Libelles caracteristiques de MVTSTOTRSVCR. Aucun d'eux n'existe dans GAMMEFAB :
    /// leur presence signe un etat « mouvements de stock ».
    /// </summary>
    public static readonly string[] Mvt =
    [
        "dateimput", "datecrea", "heurecrea", "utilisateur",
        "transaction", "typepce", "pieceorigine"
    ];

    /// <summary>Seuil de detection MVT : trois libelles concordants suffisent.</summary>
    public const int MinMvt = 3;
}

public sealed class GammeFabricationExcelReader : IGammeFabricationReader
{
    public Task<IEnumerable<RefGammeFabrication>> ReadAsync(
        Stream excelStream,
        string sourceSystem,
        Guid importBatchId,
        CancellationToken cancellationToken = default)
    {
        var content = ReadAllBytes(excelStream);
        var file = new X3ImportFileContent("GAMMEFAB.xls", content.Length, content);
        // Un classeur illisible remonte son erreur : renvoyer une liste vide marquerait
        // l'import « termine avec 0 ligne », ce qui masquerait le probleme.
        var sheets = ExcelWorkbook.Read(file, cancellationToken);
        return Task.FromResult<IEnumerable<RefGammeFabrication>>(
            LireFeuilles(sheets, sourceSystem, importBatchId, cancellationToken));
    }

    /// <summary>
    /// Coeur de lecture, isole du format de fichier pour etre directement testable
    /// sur des feuilles construites en memoire.
    /// </summary>
    internal static IReadOnlyList<RefGammeFabrication> LireFeuilles(
        IReadOnlyList<ExcelWorkbook.Sheet> sheets,
        string sourceSystem,
        Guid importBatchId,
        CancellationToken cancellationToken = default)
    {
        var result = new List<RefGammeFabrication>();

        // Aucune ligne n'est lue tant que la structure du classeur n'est pas reconnue :
        // un etat X3 different (MVTSTOTRSVCR notamment) doit etre REJETE, jamais
        // interprete positionnellement comme une gamme.
        var feuilleRetenue = -1;
        var ligneEntete = -1;
        for (var index = 0; index < sheets.Count; index++)
        {
            var candidate = ExcelWorkbook.DetecterLigneEntete(
                sheets[index].Rows,
                GammeFabricationSignature.Attendus,
                GammeFabricationSignature.MinCorrespondances);
            if (candidate >= 0)
            {
                feuilleRetenue = index;
                ligneEntete = candidate;
                break;
            }
        }

        if (feuilleRetenue < 0)
        {
            throw new ImportFileException(ConstruireErreurStructure(sheets));
        }

        var feuille = sheets[feuilleRetenue];
        for (var numeroLigne = ligneEntete + 1; numeroLigne < feuille.Rows.Count; numeroLigne++)
        {
            {
                var row = feuille.Rows[numeroLigne];
                cancellationToken.ThrowIfCancellationRequested();
                if (!ExcelWorkbook.HasData(row)) continue;

                var codeArticle = NormalizeText(ExcelWorkbook.Text(GetAt(row, 0)));
                var numeroOperationRaw = ExcelWorkbook.Text(GetAt(row, 4));
                var numeroOperation = NormalizeText(numeroOperationRaw);

                // MAPPING POSITIONNEL ASSUME ET VERIFIE.
                // Dans l'export GAMMEFAB reel, l'ordre des LIBELLES d'en-tete differe de
                // l'ordre des DONNEES : l'en-tete commence par « ETP Suivi, No Opér, Nb
                // Postes... » alors que les donnees commencent par « Article, Désignation,
                // DatFVal... ». Resoudre les colonnes par leur nom decalerait donc toutes
                // les valeurs. Les index ci-dessous ont ete releves sur le fichier reel ;
                // c'est la SIGNATURE d'en-tete, verifiee plus haut, qui garantit qu'on ne
                // les applique jamais a un autre etat X3.
                // [À ARBITRER] Un export GAMMEFAB dont les colonnes seraient reordonnees
                // ne serait pas detecte : il faudrait pour cela une correspondance
                // nom -> position fournie par X3, qui n'est pas disponible a ce jour.
                if (string.IsNullOrWhiteSpace(codeArticle) ||
                    string.IsNullOrWhiteSpace(numeroOperation) ||
                    !X3ImportParsing.TryParseDecimal(numeroOperationRaw, out _))
                {
                    continue;
                }

                var designationArticle = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 1)));
                var dateFinValidite = ReadDate(GetAt(row, 2));
                var etapeSuivi = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 3)));
                var codeAtelier = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 5)));
                var codePoste = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 6)));
                var nombrePostes = ReadDecimal(GetAt(row, 7));
                var groupePosteCharge = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 8)));
                var designationOperation = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 9)));
                var alternativeGamme = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 10)));
                var uniteOperation = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 11)));
                var coefficientOperationStock = ReadDecimal(GetAt(row, 12));
                var tempsCycle = ReadDecimal(GetAt(row, 13));
                var trs = ReadDecimal(GetAt(row, 14));
                var tempsAttente = ReadDecimal(GetAt(row, 15));
                var jalon = NormalizeOptionalText(ExcelWorkbook.Text(GetAt(row, 16)));
                var quantite = ReadDecimal(GetAt(row, 17));
                var cadence = ReadDecimal(GetAt(row, 18));
                var dateModificationX3 = ReadDateTime(GetAt(row, 19));
                var tempsReglage = ReadDecimal(GetAt(row, 20));
                var coefficientCcpt = ReadDecimal(GetAt(row, 21));

                result.Add(RefGammeFabrication.Creer(
                    codeArticle,
                    designationArticle,
                    dateFinValidite,
                    etapeSuivi,
                    alternativeGamme,
                    numeroOperation,
                    codeAtelier,
                    codePoste,
                    nombrePostes,
                    groupePosteCharge,
                    designationOperation,
                    uniteOperation,
                    coefficientOperationStock,
                    tempsCycle,
                    trs,
                    tempsAttente,
                    jalon,
                    quantite,
                    cadence,
                    dateModificationX3,
                    tempsReglage,
                    coefficientCcpt,
                    sourceSystem,
                    importBatchId,
                    DateTime.UtcNow));
            }
        }

        return result;
    }

    /// <summary>
    /// Erreur metier structuree : feuille inspectee, ligne d'en-tete, colonnes trouvees,
    /// colonnes attendues absentes, et signature MVTSTOTRSVCR detectee ou non.
    /// </summary>
    private static string ConstruireErreurStructure(IReadOnlyList<ExcelWorkbook.Sheet> sheets)
    {
        var premiere = sheets.Count > 0 ? sheets[0] : new ExcelWorkbook.Sheet("(aucune)", []);
        var ligneMvt = ExcelWorkbook.DetecterLigneEntete(
            premiere.Rows,
            GammeFabricationSignature.Mvt,
            GammeFabricationSignature.MinMvt);
        var signatureMvt = ligneMvt >= 0;

        var detail = ExcelWorkbook.DiagnosticEntete(premiere.Rows, GammeFabricationSignature.Attendus);
        var origine = signatureMvt
            ? $"Signature MVTSTOTRSVCR détectée en ligne {ligneMvt + 1} : ce fichier est un état de " +
              "mouvements de stock, il doit être importé via MVTSTOTRSVCR et non via GAMMEFAB."
            : "Aucune signature MVTSTOTRSVCR détectée : structure de fichier inconnue.";

        return $"FICHIER_GAMMEFAB_STRUCTURE_INVALIDE. Feuille: {premiere.Name}. {detail} {origine}";
    }

    private static object? GetAt(object?[] row, int index) => index < row.Length ? row[index] : null;

    private static string NormalizeText(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal? ReadDecimal(object? value)
        => string.IsNullOrWhiteSpace(ExcelWorkbook.Text(value))
            ? null
            : X3ImportParsing.TryParseDecimal(value, out var parsed) ? parsed : null;

    private static DateOnly? ReadDate(object? value)
        => string.IsNullOrWhiteSpace(ExcelWorkbook.Text(value))
            ? null
            : X3ImportParsing.TryParseDate(value, out var parsed) ? parsed : null;

    private static DateTime? ReadDateTime(object? value)
    {
        if (value is DateTime dateTime) return dateTime;
        if (value is DateOnly dateOnly) return dateOnly.ToDateTime(TimeOnly.MinValue);
        return ReadDate(value)?.ToDateTime(TimeOnly.MinValue);
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
