using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Dashboard;
using BilanMatiereUAP2.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Policy = DashboardAuthorizationPolicies.Read)]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardResponse>> Get(
        [FromQuery] string periode,
        CancellationToken cancellationToken)
        => Ok(await service.GetAsync(periode, cancellationToken));
}
