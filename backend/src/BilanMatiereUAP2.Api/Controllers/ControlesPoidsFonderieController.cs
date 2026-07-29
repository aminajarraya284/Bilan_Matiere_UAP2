using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.ControlePoidsFonderie;
using BilanMatiereUAP2.Contracts.ControlePoidsFonderie;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/controles-poids-fonderie")]
public sealed class ControlesPoidsFonderieController(IControlePoidsFonderieService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadControlesPoidsFonderie)]
    [ProducesResponseType<PagedControlesPoidsFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedControlesPoidsFonderieResponse>> GetAll(
        [FromQuery] DateOnly? dateControle,
        [FromQuery] string? numeroOF,
        [FromQuery] string? codeArticle,
        [FromQuery] Guid? typeMatiereId,
        [FromQuery] string? statut,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await service.GetAllAsync(
            new GetControlesPoidsFonderieQuery(dateControle, numeroOF, codeArticle, typeMatiereId, statut, page, pageSize),
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadControlesPoidsFonderie)]
    [ProducesResponseType<ControlePoidsFonderieResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ControlePoidsFonderieResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.GetByIdAsync(new GetControlePoidsFonderieByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageControlesPoidsFonderie)]
    [ProducesResponseType<ControlePoidsFonderieResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ControlePoidsFonderieResponse>> Create(
        CreateControlePoidsFonderieRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(new CreateControlePoidsFonderieCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageControlesPoidsFonderie)]
    [ProducesResponseType<ControlePoidsFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ControlePoidsFonderieResponse>> Update(
        Guid id,
        UpdateControlePoidsFonderieRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateAsync(new UpdateControlePoidsFonderieCommand(id, request), cancellationToken));
    }

    [HttpPost("{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageControlesPoidsFonderie)]
    [ProducesResponseType<ControlePoidsFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ControlePoidsFonderieResponse>> Valider(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.ValiderAsync(new ValiderControlePoidsFonderieCommand(id), cancellationToken));
    }

    [HttpPost("{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageControlesPoidsFonderie)]
    [ProducesResponseType<ControlePoidsFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ControlePoidsFonderieResponse>> Annuler(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.AnnulerAsync(new AnnulerControlePoidsFonderieCommand(id), cancellationToken));
    }
}
