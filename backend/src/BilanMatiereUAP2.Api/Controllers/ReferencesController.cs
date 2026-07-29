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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportGammes(IFormFile file, [FromQuery] string? profilImport, [FromQuery] string? sourceSystem, [FromQuery] bool isCompleteExport = false)
    {
        if (file is null) return BadRequest("File is required.");
        using var stream = file.OpenReadStream();
        var result = await _service.ImportGammesAsync(stream, profilImport ?? string.Empty, sourceSystem ?? "", file.FileName, isCompleteExport);
        return Ok(result);
    }

    [HttpPost("/api/v1/references/nomenclatures/import")]
    [Authorize(Policy = AuthorizationPolicies.ManageReferences)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportNomenclatures(IFormFile file, [FromQuery] string? profilImport, [FromQuery] string? sourceSystem, [FromQuery] bool isCompleteExport = false)
    {
        if (file is null) return BadRequest("File is required.");
        using var stream = file.OpenReadStream();
        var result = await _service.ImportNomenclaturesAsync(stream, profilImport ?? string.Empty, sourceSystem ?? "", file.FileName, isCompleteExport);
        return Ok(result);
    }

    [HttpGet("/api/v1/references/gammes")]
    [Authorize(Policy = AuthorizationPolicies.ReadReferences)]
    public async Task<IActionResult> GetGammes([FromQuery] string? codeArticle, [FromQuery] string? atelier, [FromQuery] string? numeroOperation, [FromQuery] bool? actif, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 50;
        var repo = HttpContext.RequestServices.GetRequiredService<IManufacturingReferencesRepository>();
        var result = await repo.QueryGammesPagedAsync(codeArticle?.Trim().ToUpperInvariant(), atelier?.Trim().ToUpperInvariant(), numeroOperation?.Trim().ToUpperInvariant(), actif, page, pageSize, cancellationToken);
        return Ok(new PagedResponse<GammeReferenceResponse>(result.Items.Select(g => new GammeReferenceResponse(
            g.CodeArticle,
            g.DesignationArticle,
            g.DateFinValidite,
            g.EtapeSuivi,
            g.AlternativeGamme,
            g.NumeroOperation,
            g.CodeAtelier,
            g.CodePoste,
            g.NombrePostes,
            g.GroupePosteCharge,
            g.DesignationOperation,
            g.UniteOperation,
            g.CoefficientOperationStock,
            g.TempsCycle,
            g.Trs,
            g.TempsAttente,
            g.Jalon,
            g.Quantite,
            g.Cadence,
            g.DateModificationX3,
            g.TempsReglage,
            g.CoefficientCcpt,
            g.CleMetier,
            g.IsActive)).ToList(), result.Total));
    }

    [HttpGet("/api/v1/references/nomenclatures")]
    [Authorize(Policy = AuthorizationPolicies.ReadReferences)]
    public async Task<IActionResult> GetNomenclatures([FromQuery] string? codeArticleParent, [FromQuery] string? codeComposant, [FromQuery] string? niveau, [FromQuery] bool? actif, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 50;
        var repo = HttpContext.RequestServices.GetRequiredService<IManufacturingReferencesRepository>();
        var result = await repo.QueryNomenclaturesPagedAsync(codeArticleParent?.Trim().ToUpperInvariant(), codeComposant?.Trim().ToUpperInvariant(), niveau?.Trim().ToUpperInvariant(), actif, page, pageSize, cancellationToken);
        return Ok(new PagedResponse<NomenclatureReferenceResponse>(result.Items.Select(n => new NomenclatureReferenceResponse(
            n.CodeArticleParent,
            n.AlternativeNomenclature,
            n.CodeParentNiveau,
            n.CodeComposant,
            n.Niveau,
            n.Sequence,
            n.DesignationComposant,
            n.QuantiteLien,
            n.QuantiteLienCumulee,
            n.UniteQuantite,
            n.CategorieComposant,
            n.TypeLien,
            n.PourcentageRebut,
            n.CleMetier,
            n.IsActive)).ToList(), result.Total));
    }
}
