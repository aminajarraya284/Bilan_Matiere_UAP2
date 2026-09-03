using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.ImportsX3;
using BilanMatiereUAP2.Contracts.ImportsX3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/consommations-composants")]
[Authorize(Policy = AuthorizationPolicies.ManageImportsX3)]
public sealed class ConsommationsComposantsController(IX3ImportService service) : ControllerBase
{
    public sealed class ImportX3Form
    {
        public IFormFile? EncoursProduct { get; set; }
        public IFormFile? MouvementsStock { get; set; }
    }

    [HttpPost("imports")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ImportX3Response>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ImportX3Response>> Import(
        [FromForm] ImportX3Form form,
        CancellationToken cancellationToken)
    {
        var response = await service.ImportAsync(
            new ImportSortiesMatiereX3Command(
                await ToContentAsync(form.EncoursProduct, cancellationToken),
                await ToContentAsync(form.MouvementsStock, cancellationToken)),
            cancellationToken);
        return CreatedAtAction(nameof(GetImport), new { importBatchId = response.ImportBatchId }, response);
    }

    [HttpGet("imports/{importBatchId:guid}")]
    public async Task<ActionResult<ImportX3Response>> GetImport(Guid importBatchId, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(new GetImportX3ByIdQuery(importBatchId), cancellationToken));

    [HttpGet("imports/{importBatchId:guid}/anomalies")]
    public async Task<ActionResult<IReadOnlyList<ImportX3AnomalyResponse>>> GetAnomalies(Guid importBatchId, CancellationToken cancellationToken) =>
        Ok(await service.GetAnomaliesAsync(new GetImportX3AnomaliesQuery(importBatchId), cancellationToken));

    [HttpGet]
    public async Task<ActionResult<PagedConsommationsComposantsResponse>> GetAll(
        [FromQuery] string? periode, [FromQuery] DateOnly? dateDebut, [FromQuery] DateOnly? dateFin,
        [FromQuery] string? numeroOF, [FromQuery] string? article, [FromQuery] string? composant,
        [FromQuery] string? statut, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetSortiesAsync(new GetSortiesMatiereQuery(
            periode, dateDebut, dateFin, numeroOF, article, composant, statut, page, pageSize, sortBy, sortDirection), cancellationToken));

    [HttpGet("ordres/{numeroOF}")]
    public async Task<ActionResult<OrdreFabricationContexteResponse>> GetOrdre(string numeroOF, CancellationToken cancellationToken) =>
        Ok(await service.GetOrdreContexteAsync(new GetOrdreFabricationContexteQuery(numeroOF), cancellationToken));

    private static async Task<X3ImportFileContent?> ToContentAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return null;
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return new X3ImportFileContent(file.FileName, file.Length, stream.ToArray());
    }
}
