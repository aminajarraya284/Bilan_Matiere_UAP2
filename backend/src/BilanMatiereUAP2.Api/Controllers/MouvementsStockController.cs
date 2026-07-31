using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.EntreeOf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/mouvements-stock")]
[Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
public sealed class MouvementsStockController(IEntreeOfImportService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? dateDebut,
        [FromQuery] DateOnly? dateFin,
        [FromQuery] string? numeroOF,
        [FromQuery] string? codeArticle,
        [FromQuery] string? designation,
        [FromQuery] string? transaction,
        [FromQuery] string? typePiece,
        [FromQuery] string? pieceOrigine,
        [FromQuery] string? nature,
        [FromQuery] Guid? lotImport,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
        => Ok(await service.QueryMouvementsAsync(
            new MouvementStockConsultationQuery(
                dateDebut,
                dateFin,
                numeroOF,
                codeArticle,
                designation,
                transaction,
                typePiece,
                pieceOrigine,
                nature,
                lotImport,
                page,
                pageSize,
                sortBy,
                sortDirection),
            cancellationToken));

    [HttpGet("imports/statut")]
    public async Task<IActionResult> GetImportTracking(CancellationToken cancellationToken)
        => Ok(await service.GetImportTrackingAsync(cancellationToken));
}
