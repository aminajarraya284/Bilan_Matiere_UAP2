using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Parametres;
using BilanMatiereUAP2.Contracts.Parametres;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/parametres")]
[Route("api/parametres")]
[Authorize(Policy = AuthorizationPolicies.ReadReferences)]
public sealed class ParametresController(
    IParametreApplicationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ParametreApplicationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ParametreApplicationResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(cancellationToken));
    }

    [HttpPut("{cle}")]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    [ProducesResponseType<ParametreApplicationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ParametreApplicationResponse>> Update(
        string cle,
        [FromBody] UpdateParametreApplicationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateAsync(cle, request, cancellationToken));
    }
}
