using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/rapports-machine")]
public sealed class RapportsMachineController(IReportMachineService service) : ControllerBase
{
    [HttpPost("import")]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile file, [FromQuery] string? profilImport, [FromQuery] string? sourceSystem, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest("File is required.");
        await using var stream = file.OpenReadStream();
        var result = await service.ImportAsync(stream, profilImport ?? "YRJMCH_V1", sourceSystem ?? "X3", file.FileName, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? dateDebut,
        [FromQuery] DateOnly? dateFin,
        [FromQuery] string? numeroOF,
        [FromQuery] string? codeArticle,
        [FromQuery] string? codePoste,
        [FromQuery] string? processus,
        [FromQuery] Guid? lotImport,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.QueryConsultationAsync(
            new RapportMachineConsultationQuery(
                dateDebut,
                dateFin,
                numeroOF,
                codeArticle,
                codePoste,
                processus,
                lotImport,
                page,
                pageSize,
                sortBy,
                sortDirection),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("imports/statut")]
    [Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
    public async Task<IActionResult> GetImportTracking(CancellationToken cancellationToken)
        => Ok(await service.GetImportTrackingAsync(cancellationToken));

    [HttpGet("agregat")]
    [Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
    public async Task<IActionResult> GetAggregate([FromQuery] string numeroOF, [FromQuery] string codeArticle, CancellationToken cancellationToken)
        => Ok(await service.AggregateAsync(numeroOF, codeArticle, cancellationToken: cancellationToken));

    [HttpPost("recalculer-classification")]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    public async Task<IActionResult> RecalculerClassification(CancellationToken cancellationToken)
        => Ok(await service.RecalculerOrigineProcessusAsync(cancellationToken));
}
