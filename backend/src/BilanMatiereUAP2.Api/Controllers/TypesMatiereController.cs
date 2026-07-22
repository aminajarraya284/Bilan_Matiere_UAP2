using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.TypesMatiere;
using BilanMatiereUAP2.Contracts.TypesMatiere;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/types-matiere")]
public sealed class TypesMatiereController(ITypeMatiereService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadTypesMatiere)]
    [ProducesResponseType<IReadOnlyList<TypeMatiereResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TypeMatiereResponse>>> GetAll(
        [FromQuery] bool? actif,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(new GetTypesMatiereQuery(actif), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadTypesMatiere)]
    [ProducesResponseType<TypeMatiereResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TypeMatiereResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await service.GetByIdAsync(new GetTypeMatiereByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageTypesMatiere)]
    [ProducesResponseType<TypeMatiereResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TypeMatiereResponse>> Create(
        CreateTypeMatiereRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(new CreateTypeMatiereCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageTypesMatiere)]
    [ProducesResponseType<TypeMatiereResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TypeMatiereResponse>> Update(
        Guid id,
        UpdateTypeMatiereRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateAsync(new UpdateTypeMatiereCommand(id, request), cancellationToken));
    }

    [HttpPatch("{id:guid}/activation")]
    [Authorize(Policy = AuthorizationPolicies.ManageTypesMatiere)]
    [ProducesResponseType<TypeMatiereResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TypeMatiereResponse>> ChangeActivation(
        Guid id,
        ChangeTypeMatiereActivationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ChangeActivationAsync(
            new ChangeTypeMatiereActivationCommand(id, request),
            cancellationToken));
    }
}
