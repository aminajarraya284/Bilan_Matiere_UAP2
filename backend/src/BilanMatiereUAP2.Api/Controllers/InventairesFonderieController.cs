using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.InventairesFonderie;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/inventaires-fonderie")]
[Authorize(Policy = AuthorizationPolicies.ManageDechetsFonderie)]
public sealed class InventairesFonderieController(IInventaireFonderieService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedInventairesFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedInventairesFonderieResponse>> GetAll(
        [FromQuery] string? periode,
        [FromQuery] Guid? typeMatiereId,
        [FromQuery] string? statut,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await service.GetAllAsync(
            new GetInventairesFonderieQuery(periode, typeMatiereId, statut, page, pageSize),
            cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventaireFonderieResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.GetByIdAsync(new GetInventaireFonderieByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InventaireFonderieResponse>> Create(
        CreateInventaireFonderieRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(new CreateInventaireFonderieCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireFonderieResponse>> Update(
        Guid id,
        UpdateInventaireFonderieRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(new UpdateInventaireFonderieCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/valider")]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireFonderieResponse>> Valider(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.ValiderAsync(new ValiderInventaireFonderieCommand(id), cancellationToken));

    [HttpPost("{id:guid}/annuler")]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireFonderieResponse>> Annuler(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.AnnulerAsync(new AnnulerInventaireFonderieCommand(id), cancellationToken));
}
