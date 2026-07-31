namespace BilanMatiereUAP2.Contracts;

public sealed class GammeReferencesQuery
{
    public string? Recherche { get; init; }
    public string? CodeArticle { get; init; }
    public string? Designation { get; init; }
    public DateOnly? ValideLe { get; init; }
    public string? Alternative { get; init; }
    public string? NumeroOperation { get; init; }
    public string? Atelier { get; init; }
    public string? Poste { get; init; }
    public string? Groupe { get; init; }
    public string? Unite { get; init; }
    public string? Source { get; init; }
    public Guid? LotImport { get; init; }
    public bool? Actif { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class NomenclatureReferencesQuery
{
    public string? Recherche { get; init; }
    public string? CodeArticleParent { get; init; }
    public string? CodeComposant { get; init; }
    public string? Designation { get; init; }
    public string? Alternative { get; init; }
    public string? Niveau { get; init; }
    public string? Unite { get; init; }
    public string? Source { get; init; }
    public Guid? LotImport { get; init; }
    public bool? Actif { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
