using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.ReceptionsUsinage2;
using BilanMatiereUAP2.Contracts.ReceptionsUsinage2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/receptions-usinage2")]
[Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
public sealed class ReceptionsUsinage2Controller(
    IReceptionUsinage2ContextService contextService,
    IReceptionUsinage2Service service,
    IAlerteNonReceptionService alerteService) : ControllerBase
{
    /// <summary>
    /// Alerte journaliere des pieces non receptionnees, une ligne par Numero OF.
    /// L'univers des OF vient des Sorties MP imputees a la date demandee.
    /// </summary>
    [HttpGet("non-receptionnees")]
    [ProducesResponseType<AlerteNonReceptionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNonReceptionnees(
        [FromQuery] DateOnly date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await alerteService.GetAsync(date, page, pageSize, cancellationToken));

    [HttpGet("contexte/{numeroOF}")]
    public async Task<IActionResult> GetContexte(
        string numeroOF,
        [FromQuery] DateOnly? dateReception,
        CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(
            numeroOF,
            dateReception ?? DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);
        return context is null
            ? NotFound(new { message = "OF introuvable" })
            : Ok(context);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? periodeDebut,
        [FromQuery] DateOnly? periodeFin,
        [FromQuery] string? numeroOF,
        [FromQuery] string? codeArticle,
        [FromQuery] string? codeTypeMatiere,
        [FromQuery] string? atelier,
        [FromQuery] string? statut,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await service.GetAllAsync(
            new GetReceptionsUsinage2Query(periodeDebut, periodeFin, numeroOF, codeArticle, codeTypeMatiere, atelier, statut, page, pageSize),
            cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await service.GetByIdAsync(new GetReceptionUsinage2ByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageReceptionsUsinage2)]
    public async Task<IActionResult> Create(CreateReceptionUsinage2Request request, CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(new CreateReceptionUsinage2Command(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageReceptionsUsinage2)]
    public async Task<IActionResult> Update(Guid id, UpdateReceptionUsinage2Request request, CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(new UpdateReceptionUsinage2Command(id, request), cancellationToken));

    [HttpPost("{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageReceptionsUsinage2)]
    public async Task<IActionResult> Valider(Guid id, ValiderReceptionUsinage2Request request, CancellationToken cancellationToken)
        => Ok(await service.ValiderAsync(new ValiderReceptionUsinage2Command(id, request), cancellationToken));

    [HttpPost("{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageReceptionsUsinage2)]
    public async Task<IActionResult> Annuler(Guid id, CancellationToken cancellationToken)
        => Ok(await service.AnnulerAsync(new AnnulerReceptionUsinage2Command(id), cancellationToken));

    [HttpPost("{id:guid}/recalculer-contexte")]
    [Authorize(Policy = AuthorizationPolicies.ManageReceptionsUsinage2)]
    public async Task<IActionResult> RecalculerContexte(Guid id, CancellationToken cancellationToken)
        => Ok(await service.RecalculerContexteAsync(new RecalculerContexteReceptionUsinage2Command(id), cancellationToken));
}
