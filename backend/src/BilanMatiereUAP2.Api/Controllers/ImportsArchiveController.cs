using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/imports")]
[Authorize(Policy = AuthorizationPolicies.ReadReferences)]
public sealed class ImportsArchiveController(IImportArchiveService service) : ControllerBase
{
    [HttpGet("{batchId:guid}/files")]
    public async Task<IActionResult> GetFiles(Guid batchId, CancellationToken cancellationToken)
        => Ok(await service.GetFilesAsync(batchId, cancellationToken));

    [HttpGet("{batchId:guid}/files/{fileId:guid}/download")]
    public async Task<IActionResult> Download(Guid batchId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await service.DownloadAsync(batchId, fileId, cancellationToken);
        return File(file.Content, file.MimeType, file.NomFichierOriginal);
    }
}
