using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.EntreeOf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/mouvements-entree-of")]
public sealed class MouvementsEntreeOFController(IEntreeOfImportService service) : ControllerBase
{
    [HttpPost("import")]
    [Authorize(Policy = AuthorizationPolicies.ManageImportsX3)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile file, [FromQuery] string? sourceSystem, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest("File is required.");
        await using var stream = file.OpenReadStream();
        var result = await service.ImportAsync(stream, sourceSystem ?? "X3", file.FileName, cancellationToken);
        return Ok(result);
    }

    [HttpGet("resolution/{numeroOF}")]
    [Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
    public async Task<IActionResult> GetResolution(
        string numeroOF,
        [FromQuery] int annee,
        [FromQuery] int mois,
        [FromQuery] string? codeArticle,
        CancellationToken cancellationToken)
        => Ok(await service.ResolveAsync(numeroOF, annee, mois, codeArticle, cancellationToken));
}
