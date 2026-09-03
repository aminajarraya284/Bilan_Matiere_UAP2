using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Contracts.Bilans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/bilans/uap2")]
[Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
public sealed class BilansUap2Controller(IBilanUap2Service service) : ControllerBase
{
    [HttpGet("periodes")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetPeriodes(CancellationToken cancellationToken)
        => Ok(await service.GetPeriodesDisponiblesAsync(cancellationToken));

    [HttpGet]
    [ProducesResponseType<BilanUap2Response>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BilanUap2Response>> GetCourant(CancellationToken cancellationToken)
        => Ok(await service.GetAsync(DateTime.UtcNow.ToString("yyyy-MM"), cancellationToken));

    [HttpGet("{periode}")]
    [ProducesResponseType<BilanUap2Response>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BilanUap2Response>> Get(
        string periode,
        CancellationToken cancellationToken)
        => Ok(await service.GetAsync(periode, cancellationToken));
}
