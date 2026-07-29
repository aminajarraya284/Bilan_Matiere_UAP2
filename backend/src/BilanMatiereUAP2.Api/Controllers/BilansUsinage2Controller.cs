using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Bilans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/bilans/usinage2")]
[Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
public sealed class BilansUsinage2Controller(IBilanUsinage2Service service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourant(CancellationToken cancellationToken)
        => Ok(await service.GetAsync(DateTime.UtcNow.ToString("yyyy-MM"), cancellationToken));

    [HttpGet("{periode}")]
    public async Task<IActionResult> Get(string periode, CancellationToken cancellationToken)
        => Ok(await service.GetAsync(periode, cancellationToken));

    [HttpGet("{periode}/details")]
    public async Task<IActionResult> GetDetails(string periode, CancellationToken cancellationToken)
    {
        var bilan = await service.GetAsync(periode, cancellationToken);
        return Ok(bilan.Lignes);
    }

    [HttpGet("{periode}/export")]
    public async Task<IActionResult> Export(string periode, CancellationToken cancellationToken)
    {
        var csv = await service.ExportCsvAsync(periode, cancellationToken);
        return File(csv, "text/csv", $"bilan-usinage2-{periode}.csv");
    }
}
