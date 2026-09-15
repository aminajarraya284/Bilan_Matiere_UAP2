using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Services;

public static class BilanMetierRules
{
    public static string NormaliserCle(string? value)
        => value?.Trim().ToUpperInvariant() ?? string.Empty;

    public static OrigineProcessus ClassifierPoste(string? poste)
        => OrigineProcessusRules.DeterminerOrigineProcessus(poste);

    public static decimal ToQuantiteMetier(decimal quantiteX3Signee)
        => -quantiteX3Signee;

    public static decimal? CalculerTaux(decimal? numerateur, decimal? denominateur, bool valeurAbsolueNumerateur = false)
    {
        if (numerateur is null || denominateur is null || denominateur.Value == 0m)
        {
            return null;
        }

        var valeur = valeurAbsolueNumerateur ? Math.Abs(numerateur.Value) : numerateur.Value;
        return valeur / denominateur.Value * 100m;
    }

    public static decimal CalculerBilanUsinage2(
        decimal quantiteReceptionneePieces,
        decimal rebutUsinage2Pieces,
        decimal entreeOfPieces)
        => quantiteReceptionneePieces - rebutUsinage2Pieces - entreeOfPieces;

    public static string DeterminerStatutNonReception(decimal? quantiteNonReceptionneePieces)
        => quantiteNonReceptionneePieces switch
        {
            null => "DonneesManquantes",
            0 => "OK",
            > 0 => "AVerifier",
            _ => "Anomalie"
        };
}
