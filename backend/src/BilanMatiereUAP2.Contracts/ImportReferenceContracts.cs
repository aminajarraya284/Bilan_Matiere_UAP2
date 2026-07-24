using System.Text.Json.Serialization;

namespace BilanMatiereUAP2.Contracts;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total);

public sealed record ImportReferenceResponse(bool Duplicate, int Inserted, int Updated);

public sealed record GammeReferenceResponse(
    string CodeArticle,
    string? AlternativeGamme,
    string NumeroOperation,
    string CodePoste,
    string CleMetier,
    bool IsActive);

public sealed record NomenclatureReferenceResponse(
    string CodeArticleParent,
    string? AlternativeNomenclature,
    string CodeParentNiveau,
    string CodeComposant,
    string Niveau,
    string Sequence,
    string CleMetier,
    bool IsActive);
