using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.RebutsUsinage2;
using BilanMatiereUAP2.Contracts.RebutsUsinage2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

/// <summary>
/// Rebuts declares a l'atelier Usinage 2, exprimes en PIECES.
/// Seuls les rebuts valides alimentent le Bilan Usinage 2.
/// </summary>
[ApiController]
[Route("api/v1/rebuts-usinage2")]
public sealed class RebutsUsinage2Controller(IRebutUsinage2Service service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
    [ProducesResponseType<PagedRebutsUsinage2Response>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] int? annee,
        [FromQuery] int? mois,
        [FromQuery] string? numeroOF,
        [FromQuery] string? codeArticle,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 50;
        return Ok(await service.QueryAsync(annee, mois, numeroOF, codeArticle, page, pageSize, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageReceptionsUsinage2)]
    [ProducesResponseType<RebutUsinage2Response>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateRebutUsinage2Request request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageReceptionsUsinage2)]
    [ProducesResponseType<RebutUsinage2Response>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, UpdateRebutUsinage2Request request, CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageReceptionsUsinage2)]
    [ProducesResponseType<RebutUsinage2Response>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Valider(Guid id, CancellationToken cancellationToken)
        => Ok(await service.ValiderAsync(id, cancellationToken));

    [HttpPost("{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageReceptionsUsinage2)]
    [ProducesResponseType<RebutUsinage2Response>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Annuler(Guid id, CancellationToken cancellationToken)
        => Ok(await service.AnnulerAsync(id, cancellationToken));
}
