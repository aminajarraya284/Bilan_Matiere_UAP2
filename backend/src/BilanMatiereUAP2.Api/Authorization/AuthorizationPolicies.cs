namespace BilanMatiereUAP2.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string ReadTypesMatiere = nameof(ReadTypesMatiere);
    public const string ManageTypesMatiere = nameof(ManageTypesMatiere);
    public const string ManageDechetsFonderie = nameof(ManageDechetsFonderie);
    public const string ReadControlesPoidsFonderie = nameof(ReadControlesPoidsFonderie);
    public const string ManageControlesPoidsFonderie = nameof(ManageControlesPoidsFonderie);
    public const string ManageImportsX3 = nameof(ManageImportsX3);
    public const string ManageReferences = nameof(ManageReferences);
    public const string ReadReferences = nameof(ReadReferences);
    public const string ReadReceptionsUsinage2 = nameof(ReadReceptionsUsinage2);
    public const string ManageReceptionsUsinage2 = nameof(ManageReceptionsUsinage2);
    public const string ReadInventairesFonderie = nameof(ReadInventairesFonderie);
    public const string ManageInventairesFonderie = nameof(ManageInventairesFonderie);
    public const string ManageAdministration = nameof(ManageAdministration);
    public const string ManagePeriodArchives = nameof(ManagePeriodArchives);
}
