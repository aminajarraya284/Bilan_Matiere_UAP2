using BilanMatiereUAP2.Contracts.Rapports;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record RapportOfReadData(
    IReadOnlyList<RapportOfReceptionResponse> Receptions,
    IReadOnlyList<RapportOfProcessusResponse> Processus,
    IReadOnlyList<RapportOfMouvementResponse> Mouvements,
    IReadOnlyList<RapportOfControlePoidsResponse> ControlesPoids,
    IReadOnlyList<RapportOfReferenceResponse> References);

public interface IRapportsReadRepository
{
    Task<RapportOfReadData> GetOfAsync(
        string numeroOf,
        string? codeArticle,
        CancellationToken cancellationToken = default);

    Task<PagedResult<RapportImportLigneResponse>> SearchImportsAsync(
        DateTime? dateDepuisUtc,
        DateTime? dateJusquaUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
