namespace BilanMatiereUAP2.Application.Options;

public sealed class ReceptionUsinage2Options
{
    public const string SectionName = "ReceptionUsinage2";

    // TODO_METIER: seuil confirme par SOPAL. Valeur par defaut prudente en attendant validation.
    public decimal SeuilAlerteEcartPoidsPct { get; init; } = 5m;
}
