using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Services;

/// <summary>
/// Regle metier de classification du processus a partir du code poste YRJMCH (colonne B).
///
/// Regle confirmee par SOPAL : seuls les TROIS PREMIERS caracteres du code poste
/// determinent le processus.
///   SCIxxx -> SCI | EMRxxx -> EMR | TRNxxx -> TRN | tout autre prefixe -> Inconnue
///
/// Cette regle remplace l'ancien referentiel manuel « Postes processus », qui exigeait
/// d'administrer chaque code poste exact (295 codes distincts). Aucun mapping manuel
/// n'est desormais necessaire.
/// </summary>
public static class OrigineProcessusRules
{
    /// <summary>Longueur du prefixe significatif du code poste.</summary>
    public const int LongueurPrefixe = 3;

    public static OrigineProcessus DeterminerOrigineProcessus(string? poste)
    {
        var normalise = poste?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalise) || normalise.Length < LongueurPrefixe)
        {
            return OrigineProcessus.Inconnue;
        }

        return normalise[..LongueurPrefixe] switch
        {
            "SCI" => OrigineProcessus.Sci,
            "EMR" => OrigineProcessus.Emr,
            "TRN" => OrigineProcessus.Trn,
            _ => OrigineProcessus.Inconnue
        };
    }
}
