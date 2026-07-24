using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/references")]
public sealed class ReferencesController : ControllerBase
{
    private readonly IManufacturingReferencesService _service;

    public ReferencesController(IManufacturingReferencesService service)
    {
        _service = service;
    }

    [HttpPost("/api/v1/references/gammes/import")]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    public async Task<IActionResult> ImportGammes([FromForm] IFormFile file, [FromQuery] string? profilImport, [FromQuery] string? sourceSystem, [FromQuery] bool isCompleteExport = false)
    {
        if (file is null) return BadRequest("File is required.");
        using var stream = file.OpenReadStream();
        var result = await _service.ImportGammesAsync(stream, profilImport ?? string.Empty, sourceSystem ?? "", isCompleteExport);
        return Ok(result);
    }

    [HttpPost("/api/v1/references/nomenclatures/import")]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    public async Task<IActionResult> ImportNomenclatures([FromForm] IFormFile file, [FromQuery] string? profilImport, [FromQuery] string? sourceSystem, [FromQuery] bool isCompleteExport = false)
    {
        if (file is null) return BadRequest("File is required.");
        using var stream = file.OpenReadStream();
        var result = await _service.ImportNomenclaturesAsync(stream, profilImport ?? string.Empty, sourceSystem ?? "", isCompleteExport);
        return Ok(result);
    }

    [HttpGet("/api/v1/references/gammes")]
    [Authorize(Policy = AuthorizationPolicies.ReadReferences)]
    public async Task<IActionResult> GetGammes([FromQuery] string? codeArticle, [FromQuery] string? atelier, [FromQuery] string? numeroOperation, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 50;
        var repo = HttpContext.RequestServices.GetRequiredService<IManufacturingReferencesRepository>();
        var items = await repo.QueryGammesAsync(codeArticle, atelier, numeroOperation, page, pageSize);
        return Ok(new PagedResponse<GammeReferenceResponse>(items.Select(g => new GammeReferenceResponse(g.CodeArticle, g.AlternativeGamme, g.NumeroOperation, g.CodePoste, g.CleMetier, g.IsActive)).ToList(), items.Count));
    }

    [HttpGet("/api/v1/references/nomenclatures")]
    [Authorize(Policy = AuthorizationPolicies.ReadReferences)]
    public async Task<IActionResult> GetNomenclatures([FromQuery] string? codeArticleParent, [FromQuery] string? codeComposant, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 50;
        var repo = HttpContext.RequestServices.GetRequiredService<IManufacturingReferencesRepository>();
        var items = await repo.QueryNomenclaturesAsync(codeArticleParent, codeComposant, page, pageSize);
        return Ok(new PagedResponse<NomenclatureReferenceResponse>(items.Select(n => new NomenclatureReferenceResponse(n.CodeArticleParent, n.AlternativeNomenclature, n.CodeParentNiveau, n.CodeComposant, n.Niveau, n.Sequence, n.CleMetier, n.IsActive)).ToList(), items.Count));
    }
}
