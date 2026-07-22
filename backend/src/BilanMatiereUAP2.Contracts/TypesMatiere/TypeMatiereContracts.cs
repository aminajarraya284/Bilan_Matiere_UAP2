namespace BilanMatiereUAP2.Contracts.TypesMatiere;

public sealed record CreateTypeMatiereRequest(string Code, string Libelle);

public sealed record UpdateTypeMatiereRequest(string Libelle);

public sealed record ChangeTypeMatiereActivationRequest(bool EstActif);

public sealed record TypeMatiereResponse(
    Guid Id,
    string Code,
    string Libelle,
    bool EstActif,
    DateTime DateCreationUtc,
    DateTime? DateModificationUtc);
