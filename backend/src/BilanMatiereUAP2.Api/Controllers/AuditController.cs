using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Contracts.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
[Authorize(Policy = AuditAuthorizationPolicies.Read)]
public sealed class AuditController(IAuditQueryService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedAuditResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedAuditResponse>> Search(
        [FromQuery] DateOnly? dateDepuis,
        [FromQuery] DateOnly? dateJusqua,
        [FromQuery] string? utilisateur,
        [FromQuery] string? role,
        [FromQuery] string? action,
        [FromQuery] string? domaine,
        [FromQuery] string? entite,
        [FromQuery] string? referenceMetier,
        [FromQuery] string? correlationId,
        [FromQuery] string? recherche,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.SearchAsync(
            new GetAuditQuery(
                dateDepuis, dateJusqua, utilisateur, role, action, domaine, entite,
                referenceMetier, correlationId, recherche, page, pageSize),
            cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AuditDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditDetailResponse>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(id, cancellationToken));
}
