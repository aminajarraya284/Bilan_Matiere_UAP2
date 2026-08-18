using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Alertes;
using BilanMatiereUAP2.Contracts.Alertes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/alertes")]
[Authorize(Policy = AlertesAuthorizationPolicies.Read)]
public sealed class AlertesController(IAlerteApplicationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedAlertesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedAlertesResponse>> Search(
        [FromQuery] string? periode,
        [FromQuery] string? statut,
        [FromQuery] string? severite,
        [FromQuery] string? source,
        [FromQuery] string? numeroOF,
        [FromQuery] string? codeArticle,
        [FromQuery] string? recherche,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.SearchAsync(
            new GetAlertesQuery(
                periode,
                statut,
                severite,
                source,
                numeroOF,
                codeArticle,
                recherche,
                page,
                pageSize),
            cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AlerteDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlerteDetailResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost("recalculer")]
    [Authorize(Policy = AlertesAuthorizationPolicies.Manage)]
    [ProducesResponseType<AlerteRecalculResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlerteRecalculResponse>> Recalculer(
        [FromQuery] string periode,
        CancellationToken cancellationToken)
        => Ok(await service.RecalculerAsync(periode, cancellationToken));

    [HttpPost("{id:guid}/acquitter")]
    [Authorize(Policy = AlertesAuthorizationPolicies.Manage)]
    [ProducesResponseType<AlerteDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlerteDetailResponse>> Acquitter(
        Guid id,
        AcquitterAlerteRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.AcquitterAsync(id, request.Commentaire, cancellationToken));

    [HttpPost("{id:guid}/resoudre")]
    [Authorize(Policy = AlertesAuthorizationPolicies.Manage)]
    [ProducesResponseType<AlerteDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlerteDetailResponse>> Resoudre(
        Guid id,
        ResoudreAlerteRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.ResoudreAsync(id, request.Commentaire, cancellationToken));

    [HttpPost("{id:guid}/ignorer")]
    [Authorize(Policy = AlertesAuthorizationPolicies.Manage)]
    [ProducesResponseType<AlerteDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlerteDetailResponse>> Ignorer(
        Guid id,
        IgnorerAlerteRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.IgnorerAsync(id, request.Commentaire, cancellationToken));
}
