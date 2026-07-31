using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Services;

/// <summary>
/// Regle metier de classification du processus a partir du code poste YRJMCH (colonne B).
///
/// Regle confirmee par SOPAL : seuls les TROIS PREMIERS caracteres du code poste
/// determinent le processus.
///   CTRL (valeur exacte) -> CTRL | SCIxxx -> SCI | EMRxxx -> EMR | TRNxxx -> TRN |
///   tout autre prefixe -> Inconnue
///
/// Cette regle remplace l'ancien referentiel manuel « Postes processus », qui exigeait
/// d'administrer chaque code poste exact (295 codes distincts). Aucun mapping manuel
/// n'est desormais necessaire.
/// </summary>
public static class OrigineProcessusRules
{
    /// <summary>Longueur du prefixe significatif du code poste.</summary>
    public const int LongueurPrefixe = 3;

    private static readonly string[] PrefixesDetectesNonValides = ["NOY", "GRN", "COQ", "CBP"];

    public static OrigineProcessus ClassifierPoste(string? poste)
        => DeterminerOrigineProcessus(poste);

    public static OrigineProcessus DeterminerOrigineProcessus(string? poste)
    {
        var normalise = poste?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalise) || normalise.Length < LongueurPrefixe)
        {
            return OrigineProcessus.Inconnue;
        }

        if (string.Equals(normalise, "CTRL", StringComparison.Ordinal))
        {
            return OrigineProcessus.Ctrl;
        }

        return normalise[..LongueurPrefixe] switch
        {
            "SCI" => OrigineProcessus.Sci,
            "EMR" => OrigineProcessus.Emr,
            "TRN" => OrigineProcessus.Trn,
            _ => OrigineProcessus.Inconnue
        };
    }

    /// <summary>
    /// Détecte les préfixes présents dans les exports mais non validés pour un agrégat.
    /// [À ARBITRER] : ces postes restent hors des agrégats tant que le métier ne confirme
    /// pas leur rattachement.
    /// </summary>
    public static bool EstPrefixeDetecteNonValide(string? poste)
    {
        var normalise = poste?.Trim().ToUpperInvariant() ?? string.Empty;
        return PrefixesDetectesNonValides.Any(prefixe => normalise.StartsWith(prefixe, StringComparison.Ordinal));
    }
}
