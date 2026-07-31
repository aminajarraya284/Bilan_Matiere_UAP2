using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public enum SourceTypeMatiere
{
    SortieMp,
    Nomenclature,
    Inconnue
}

public sealed record OfContextData(
    X3OrdreFabrication? EncoursProduct,
    IReadOnlyList<X3RapportMachine> RapportsMachine,
    IReadOnlyList<EntreeOfComposant> EntreesOf,
    IReadOnlyList<SortieMpMaterialLink> SortiesMp);

public interface IOfContextDataRepository
{
    Task<OfContextData> GetAsync(string numeroOf, CancellationToken cancellationToken = default);
}

public interface IOfContextReferencesRepository
{
    Task<IReadOnlyList<RefGammeFabrication>> GetGammesActivesByArticleAsync(
        string codeArticle,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefGammeFabrication>> GetGammesActivesByArticlesAsync(
        IReadOnlyCollection<string> codesArticle,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefNomenclature>> GetNomenclaturesByArticleAndComponentAsync(
        string codeArticleParent,
        string codeComposant,
        CancellationToken cancellationToken = default);
}

public sealed record OfContextResult(
    string NumeroOF,
    string? CodeArticle,
    string? Designation,
    string? TypeMatiere,
    SourceTypeMatiere SourceTypeMatiere,
    string? CodeAtelier,
    string? Groupe,
    bool EligibleCalculUap2,
    bool ExisteDansYrjmch,
    bool ExisteDansEntreeOf,
    bool ExisteDansSortieMp,
    bool ExisteDansEncoursProduct,
    string StatutRapprochement,
    IReadOnlyCollection<string> Anomalies);

public interface IOfContextResolver
{
    Task<OfContextResult> ResolveAsync(
        string numeroOf,
        DateOnly? date,
        CancellationToken cancellationToken = default);
}
