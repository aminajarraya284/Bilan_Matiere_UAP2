using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Rapports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/rapports")]
[Authorize(Policy = RapportsAuthorizationPolicies.Read)]
public sealed class RapportsController(IRapportsService service) : ControllerBase
{
    [HttpGet("catalogue")]
    public IActionResult GetCatalogue() => Ok(service.GetCatalogue());

    [HttpGet("fonderie/{periode}")]
    public async Task<IActionResult> GetFonderie(string periode, CancellationToken cancellationToken)
        => Ok(await service.GetBilanFonderieAsync(periode, cancellationToken));

    [HttpGet("usinage2/{periode}")]
    public async Task<IActionResult> GetUsinage2(
        string periode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.GetBilanUsinage2Async(periode, page, pageSize, cancellationToken));

    [HttpGet("uap2/{periode}")]
    public async Task<IActionResult> GetUap2(
        string periode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.GetBilanUap2Async(periode, page, pageSize, cancellationToken));

    [HttpGet("of/{numeroOF}")]
    public async Task<IActionResult> GetOf(
        string numeroOF,
        [FromQuery] string periode,
        CancellationToken cancellationToken)
        => Ok(await service.GetOfAsync(numeroOF, periode, cancellationToken));

    [HttpGet("alertes")]
    public async Task<IActionResult> GetAlertes(
        [FromQuery] string? periode,
        [FromQuery] string? statut,
        [FromQuery] string? severite,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.GetAlertesAsync(periode, statut, severite, page, pageSize, cancellationToken));

    [HttpGet("inventaire/{periode}")]
    public async Task<IActionResult> GetInventaire(
        string periode,
        [FromQuery] string? codeTypeMatiere,
        CancellationToken cancellationToken)
        => Ok(await service.GetInventaireAsync(periode, codeTypeMatiere, cancellationToken));

    [HttpGet("imports")]
    public async Task<IActionResult> GetImports(
        [FromQuery] DateOnly? dateDepuis,
        [FromQuery] DateOnly? dateJusqua,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.GetImportsAsync(dateDepuis, dateJusqua, page, pageSize, cancellationToken));

    [HttpGet("receptions-non-receptionnees")]
    public async Task<IActionResult> GetNonReceptionnees(
        [FromQuery] DateOnly date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.GetNonReceptionneesAsync(date, page, pageSize, cancellationToken));

    [HttpGet("{code}/export")]
    public async Task<IActionResult> Export(
        string code,
        [FromQuery] string? periode,
        [FromQuery] string? numeroOF,
        [FromQuery] DateOnly? date,
        [FromQuery] string? statut,
        [FromQuery] string? severite,
        [FromQuery] string? codeTypeMatiere,
        [FromQuery] DateOnly? dateDepuis,
        [FromQuery] DateOnly? dateJusqua,
        CancellationToken cancellationToken = default)
    {
        var export = await service.ExportCsvAsync(
            code,
            periode,
            numeroOF,
            date,
            statut,
            severite,
            codeTypeMatiere,
            dateDepuis,
            dateJusqua,
            cancellationToken);
        return File(export.Content, export.ContentType, export.FileName);
    }
}
