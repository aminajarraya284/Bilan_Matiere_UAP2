using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Archivage;
using BilanMatiereUAP2.Contracts.Archivage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/periodes-bilan")]
[Authorize(Policy = AuthorizationPolicies.ManagePeriodArchives)]
public sealed class PeriodesBilanController(IPeriodeBilanArchiveService service) : ControllerBase
{
    [HttpPost("{periode}/enregistrer")]
    [ProducesResponseType<PeriodeBilanArchiveDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PeriodeBilanArchiveDetailResponse>> Enregistrer(
        string periode,
        CancellationToken cancellationToken)
    {
        var archive = await service.EnregistrerAsync(periode, cancellationToken);
        return CreatedAtAction(nameof(Get), new { periode = archive.Metadata.Periode }, archive);
    }

    [HttpGet]
    [ProducesResponseType<PagedPeriodesBilanArchiveesResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedPeriodesBilanArchiveesResponse>> Search(
        [FromQuery] string? periode,
        [FromQuery] string? statut,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await service.SearchAsync(new(periode, statut, page, pageSize), cancellationToken));

    [HttpGet("{periode}")]
    [ProducesResponseType<PeriodeBilanArchiveDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PeriodeBilanArchiveDetailResponse>> Get(
        string periode,
        CancellationToken cancellationToken)
        => Ok(await service.GetAsync(periode, cancellationToken));

    [HttpGet("{periode}/rapport")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rapport(string periode, CancellationToken cancellationToken)
    {
        var rapport = await service.GetRapportAsync(periode, cancellationToken);
        return File(rapport.Content, rapport.ContentType, rapport.FileName);
    }
}
