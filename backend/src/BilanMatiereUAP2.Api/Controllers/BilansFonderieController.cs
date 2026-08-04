using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Contracts.Bilans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/bilans/fonderie")]
[Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
public sealed class BilansFonderieController(IBilanFonderieService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<BilanFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BilanFonderieResponse>> GetCourant(CancellationToken cancellationToken)
        => Ok(await service.GetAsync(DateTime.UtcNow.ToString("yyyy-MM"), cancellationToken));

    [HttpGet("{periode}")]
    [ProducesResponseType<BilanFonderieResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BilanFonderieResponse>> Get(
        string periode,
        CancellationToken cancellationToken)
        => Ok(await service.GetAsync(periode, cancellationToken));
}
