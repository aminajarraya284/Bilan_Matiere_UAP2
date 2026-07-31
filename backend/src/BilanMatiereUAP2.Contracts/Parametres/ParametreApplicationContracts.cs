namespace BilanMatiereUAP2.Contracts.Parametres;

public sealed record ParametreApplicationResponse(
    string Cle,
    string Valeur,
    string Description,
    bool Modifiable = true,
    string TypeValeur = "Texte");

public sealed record UpdateParametreApplicationRequest(string Valeur);
