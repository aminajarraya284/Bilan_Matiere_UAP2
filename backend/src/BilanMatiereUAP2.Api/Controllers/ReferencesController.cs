using System.Globalization;
using System.Text;
using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/references")]
public sealed class ReferencesController : ControllerBase
{
    private readonly IManufacturingReferencesService _service;
    private readonly IManufacturingReferencesRepository _repository;

    public ReferencesController(
        IManufacturingReferencesService service,
        IManufacturingReferencesRepository repository)
    {
        _service = service;
        _repository = repository;
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
    public async Task<IActionResult> GetGammes(
        [FromQuery] GammeReferencesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!PaginationValide(query.Page, query.PageSize))
        {
            return ValidationProblem("La page doit etre positive et pageSize compris entre 1 et 200.");
        }

        var result = await _repository.SearchGammesPagedAsync(ToFilter(query), cancellationToken);
        return Ok(new PagedResponse<GammeReferenceResponse>(
            result.Items.Select(ToResponse).ToArray(),
            result.Total));
    }

    [HttpGet("/api/v1/references/nomenclatures")]
    [Authorize(Policy = AuthorizationPolicies.ReadReferences)]
    public async Task<IActionResult> GetNomenclatures(
        [FromQuery] NomenclatureReferencesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!PaginationValide(query.Page, query.PageSize))
        {
            return ValidationProblem("La page doit etre positive et pageSize compris entre 1 et 200.");
        }

        var result = await _repository.SearchNomenclaturesPagedAsync(ToFilter(query), cancellationToken);
        return Ok(new PagedResponse<NomenclatureReferenceResponse>(
            result.Items.Select(ToResponse).ToArray(),
            result.Total));
    }

    [HttpGet("/api/v1/references/gammes/export")]
    [Authorize(Policy = AuthorizationPolicies.ReadReferences)]
    public async Task ExportGammes(
        [FromQuery] GammeReferencesQuery query,
        CancellationToken cancellationToken)
    {
        PrepareCsvResponse("gammes-fabrication.csv");
        await using var writer = CreateCsvWriter();
        await writer.WriteLineAsync(
            "Article;Designation;FinValidite;Alternative;Operation;Atelier;Poste;Groupe;DesignationOperation;Unite;LotImport;Source;ImporteLe;Actif");

        const int exportPageSize = 2000;
        var page = 1;
        PagedResult<RefGammeFabrication> result;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = await _repository.SearchGammesPagedAsync(
                ToFilter(query) with { Page = page, PageSize = exportPageSize },
                cancellationToken);
            foreach (var row in result.Items)
            {
                await writer.WriteLineAsync(string.Join(';',
                    Csv(row.CodeArticle),
                    Csv(row.DesignationArticle),
                    Csv(row.DateFinValidite?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    Csv(row.AlternativeGamme),
                    Csv(row.NumeroOperation),
                    Csv(row.CodeAtelier),
                    Csv(row.CodePoste),
                    Csv(row.GroupePosteCharge),
                    Csv(row.DesignationOperation),
                    Csv(row.UniteOperation),
                    Csv(row.ImportBatchId),
                    Csv(row.SourceSystem),
                    Csv(row.ImportedAt.ToString("O", CultureInfo.InvariantCulture)),
                    Csv(row.IsActive)));
            }
            await writer.FlushAsync(cancellationToken);
            page++;
        }
        while ((page - 1) * exportPageSize < result.Total);
    }

    [HttpGet("/api/v1/references/nomenclatures/export")]
    [Authorize(Policy = AuthorizationPolicies.ReadReferences)]
    public async Task ExportNomenclatures(
        [FromQuery] NomenclatureReferencesQuery query,
        CancellationToken cancellationToken)
    {
        PrepareCsvResponse("nomenclatures.csv");
        await using var writer = CreateCsvWriter();
        await writer.WriteLineAsync(
            "ArticleParent;Composant;Designation;Coefficient;QuantiteLien;Unite;Alternative;FinValidite;StatutValidite;LotImport;Source;ImporteLe;Actif");

        const int exportPageSize = 2000;
        var page = 1;
        PagedResult<RefNomenclature> result;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = await _repository.SearchNomenclaturesPagedAsync(
                ToFilter(query) with { Page = page, PageSize = exportPageSize },
                cancellationToken);
            foreach (var row in result.Items)
            {
                await writer.WriteLineAsync(string.Join(';',
                    Csv(row.CodeArticleParent),
                    Csv(row.CodeComposant),
                    Csv(row.DesignationComposant),
                    Csv(row.QuantiteLienCumulee),
                    Csv(row.QuantiteLien),
                    Csv(row.UniteQuantite),
                    Csv(row.AlternativeNomenclature),
                    Csv(null),
                    Csv("Regle metier a valider"),
                    Csv(row.ImportBatchId),
                    Csv(row.SourceSystem),
                    Csv(row.ImportedAt.ToString("O", CultureInfo.InvariantCulture)),
                    Csv(row.IsActive)));
            }
            await writer.FlushAsync(cancellationToken);
            page++;
        }
        while ((page - 1) * exportPageSize < result.Total);
    }

    private static GammeReferencesFilter ToFilter(GammeReferencesQuery query)
        => new(
            Normalize(query.Recherche),
            Normalize(query.CodeArticle),
            Normalize(query.Designation),
            query.ValideLe,
            Normalize(query.Alternative),
            Normalize(query.NumeroOperation),
            Normalize(query.Atelier),
            Normalize(query.Poste),
            Normalize(query.Groupe),
            Normalize(query.Unite),
            Normalize(query.Source),
            query.LotImport,
            query.Actif,
            query.Page,
            query.PageSize);

    private static NomenclatureReferencesFilter ToFilter(NomenclatureReferencesQuery query)
        => new(
            Normalize(query.Recherche),
            Normalize(query.CodeArticleParent),
            Normalize(query.CodeComposant),
            Normalize(query.Designation),
            Normalize(query.Alternative),
            Normalize(query.Niveau),
            Normalize(query.Unite),
            Normalize(query.Source),
            query.LotImport,
            query.Actif,
            query.Page,
            query.PageSize);

    private static GammeReferenceResponse ToResponse(RefGammeFabrication row)
        => new(
            row.CodeArticle,
            row.DesignationArticle,
            row.DateFinValidite,
            row.EtapeSuivi,
            row.AlternativeGamme,
            row.NumeroOperation,
            row.CodeAtelier,
            row.CodePoste,
            row.NombrePostes,
            row.GroupePosteCharge,
            row.DesignationOperation,
            row.UniteOperation,
            row.CoefficientOperationStock,
            row.TempsCycle,
            row.Trs,
            row.TempsAttente,
            row.Jalon,
            row.Quantite,
            row.Cadence,
            row.DateModificationX3,
            row.TempsReglage,
            row.CoefficientCcpt,
            row.CleMetier,
            row.IsActive,
            row.ImportBatchId,
            row.SourceSystem,
            row.ImportedAt);

    private static NomenclatureReferenceResponse ToResponse(RefNomenclature row)
        => new(
            row.CodeArticleParent,
            row.AlternativeNomenclature,
            row.CodeParentNiveau,
            row.CodeComposant,
            row.Niveau,
            row.Sequence,
            row.DesignationComposant,
            row.QuantiteLien,
            row.QuantiteLienCumulee,
            row.UniteQuantite,
            row.CategorieComposant,
            row.TypeLien,
            row.PourcentageRebut,
            row.CleMetier,
            row.IsActive,
            row.ImportBatchId,
            row.SourceSystem,
            row.ImportedAt,
            null,
            "RegleMetierAValider");

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static bool PaginationValide(int page, int pageSize)
        => page >= 1 && pageSize is >= 1 and <= 200;

    private void PrepareCsvResponse(string fileName)
    {
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
    }

    private StreamWriter CreateCsvWriter()
        => new(
            Response.Body,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            bufferSize: 16 * 1024,
            leaveOpen: true);

    private static string Csv(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
}
