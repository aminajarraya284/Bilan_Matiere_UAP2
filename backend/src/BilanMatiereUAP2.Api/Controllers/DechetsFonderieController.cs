using System.Globalization;
using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.DechetsFonderie;
using BilanMatiereUAP2.Contracts.DechetsFonderie;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/dechets-fonderie")]
[Authorize(Policy = AuthorizationPolicies.ManageDechetsFonderie)]
public sealed class DechetsFonderieController(IDechetFonderieService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedDechetsFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedDechetsFonderieResponse>> GetAll(
        [FromQuery] string? periode,
        [FromQuery] Guid? typeMatiereId,
        [FromQuery] string? typeDechet,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var selectedPeriod = periode ?? DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        return Ok(await service.GetAllAsync(
            new GetDechetsFonderieQuery(selectedPeriod, typeMatiereId, typeDechet, page, pageSize),
            cancellationToken));
    }

    [HttpGet("resume")]
    [ProducesResponseType<ResumeDechetsFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResumeDechetsFonderieResponse>> GetResume(
        [FromQuery] string? periode,
        CancellationToken cancellationToken)
    {
        var selectedPeriod = periode ?? DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        return Ok(await service.GetResumeAsync(new GetResumeDechetsFonderieQuery(selectedPeriod), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DechetFonderieResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DechetFonderieResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.GetByIdAsync(new GetDechetFonderieByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<DechetFonderieResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DechetFonderieResponse>> Create(
        CreateDechetFonderieRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(new CreateDechetFonderieCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<DechetFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DechetFonderieResponse>> Update(
        Guid id,
        UpdateDechetFonderieRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateAsync(new UpdateDechetFonderieCommand(id, request), cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(new DeleteDechetFonderieCommand(id), cancellationToken);
        return NoContent();
    }
}
