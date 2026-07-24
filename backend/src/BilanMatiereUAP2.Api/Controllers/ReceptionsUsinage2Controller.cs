using System.Globalization;
using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/receptions-usinage2")]
[Authorize(Policy = AuthorizationPolicies.ReadReceptionsUsinage2)]
public sealed class ReceptionsUsinage2Controller : ControllerBase
{
    private readonly IX3ImportRepository _x3Repository;
    private readonly IManufacturingReferencesRepository _refsRepository;
    private readonly ManufacturingReferencesOptions _options;

    public ReceptionsUsinage2Controller(
        IX3ImportRepository x3Repository,
        IManufacturingReferencesRepository refsRepository,
        IOptions<ManufacturingReferencesOptions> options)
    {
        _x3Repository = x3Repository;
        _refsRepository = refsRepository;
        _options = options.Value;
    }

    [HttpGet("/api/v1/receptions-usinage2/contexte/{numeroOF}")]
    public async Task<IActionResult> GetContexte(string numeroOF, [FromQuery] string? codeTypeMatiere, CancellationToken cancellationToken)
    {
        var ordre = await _x3Repository.GetOrdreContexteAsync(numeroOF, cancellationToken);
        if (ordre is null) return NotFound(new { message = "OF introuvable" });

        var article = ordre.ArticlePrincipal;
        if (string.IsNullOrWhiteSpace(article)) return BadRequest(new { message = "Article introuvable pour l'OF" });

        // Operation d'origine Usinage 2 (configurable via ManufacturingReferences:NumeroOperationOrigineUsinage2).
        var numeroOperation = _options.NumeroOperationOrigineUsinage2.ToString(CultureInfo.InvariantCulture);
        var gammes = await _refsRepository.GetGammesActivesByArticleAsync(article, numeroOperation, cancellationToken);
        var nombreNomenclatures = await _refsRepository.CountNomenclaturesActivesByParentAsync(article, cancellationToken);

        // Ateliers distincts (CodePoste) disponibles : on ne choisit jamais arbitrairement en cas d'ambiguite.
        var ateliers = gammes
            .Select(g => g.CodePoste)
            .Where(codePoste => !string.IsNullOrWhiteSpace(codePoste))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var warnings = new List<string>();
        string? atelier = null;
        var ambiguite = false;

        switch (ateliers.Length)
        {
            case 0:
                warnings.Add($"Aucune gamme active (operation {numeroOperation}) pour l'article {article}.");
                break;
            case 1:
                atelier = ateliers[0];
                break;
            default:
                ambiguite = true;
                warnings.Add($"Plusieurs ateliers possibles pour l'article {article} : {string.Join(", ", ateliers)}. Selection requise.");
                break;
        }

        if (nombreNomenclatures == 0)
        {
            warnings.Add($"Aucune nomenclature active pour l'article {article}.");
        }

        var response = new
        {
            numeroOF = ordre.NumeroOF,
            article,
            gammeTrouvee = gammes.Count > 0,
            nomenclatureTrouvee = nombreNomenclatures > 0,
            atelier,
            ateliersDisponibles = ateliers,
            ambiguiteAtelier = ambiguite,
            requiresMaterialSelection = ambiguite,
            warnings = warnings.ToArray()
        };

        return Ok(response);
    }
}
