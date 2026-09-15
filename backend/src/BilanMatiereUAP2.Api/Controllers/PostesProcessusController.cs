using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.PostesProcessus;
using BilanMatiereUAP2.Contracts.PostesProcessus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/postes-processus")]
[Authorize(Policy = AuthorizationPolicies.ReadReferences)]
public sealed class PostesProcessusController(
    IPosteProcessusService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedPostesProcessusResponse> GetAll(
        [FromQuery] string? poste,
        [FromQuery] string? origine,
        [FromQuery] bool? actif,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => service.GetPagedAsync(poste, origine, actif, page, pageSize, cancellationToken);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    public async Task<ActionResult<PosteProcessusResponse>> Create(
        [FromBody] CreatePosteProcessusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { poste = result.Poste }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    public Task<PosteProcessusResponse> Update(
        Guid id,
        [FromBody] UpdatePosteProcessusRequest request,
        CancellationToken cancellationToken)
        => service.UpdateAsync(id, request, cancellationToken);

    [HttpPut("{id:guid}/activation")]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    public Task<PosteProcessusResponse> ChangeActivation(
        Guid id,
        [FromBody] ChangePosteProcessusActivationRequest request,
        CancellationToken cancellationToken)
        => service.ChangeActivationAsync(id, request, cancellationToken);

    [HttpGet("{id:guid}/historique")]
    public Task<IReadOnlyList<PosteProcessusHistoriqueResponse>> GetHistory(
        Guid id,
        CancellationToken cancellationToken)
        => service.GetHistoryAsync(id, cancellationToken);

    [HttpGet("non-mappes")]
    public Task<IReadOnlyList<PosteNonMappeResponse>> GetNonMapped(
        CancellationToken cancellationToken)
        => service.GetNonMappedAsync(cancellationToken);

    [HttpPost("recalculer-classification")]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    public Task<RecalculClassificationPostesResponse> Recalculate(
        CancellationToken cancellationToken)
        => service.RecalculateAsync(cancellationToken);
}
