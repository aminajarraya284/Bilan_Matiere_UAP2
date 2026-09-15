using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.InventairesFonderie;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/inventaires-fonderie")]
public sealed class InventairesFonderieController(
    IInventaireFonderieService service,
    IInventaireComptageService comptageService) : ControllerBase
{
    // ==========================================
    // COMPTAGE PHYSIQUE / PROJECTION
    // ==========================================
    [HttpGet("comptage/{periode}")]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<InventaireComptagePeriodeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireComptagePeriodeResponse>> GetComptagePeriode(
        string periode,
        CancellationToken cancellationToken)
        => Ok(await comptageService.GetPeriodeAsync(periode, cancellationToken));

    [HttpPost("comptage/conteneurs")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireComptageConteneurResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InventaireComptageConteneurResponse>> CreateComptageConteneur(
        CreateInventaireComptageConteneurRequest request,
        CancellationToken cancellationToken)
        => StatusCode(
            StatusCodes.Status201Created,
            await comptageService.CreerConteneurAsync(request, cancellationToken));

    [HttpPost("comptage/conteneurs/{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    public async Task<ActionResult<InventaireComptageConteneurResponse>> ValidateComptageConteneur(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await comptageService.ValiderConteneurAsync(id, cancellationToken));

    [HttpPost("comptage/conteneurs/{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    public async Task<ActionResult<InventaireComptageConteneurResponse>> CancelComptageConteneur(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await comptageService.AnnulerConteneurAsync(id, cancellationToken));

    [HttpPost("comptage/categories")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireComptageCategorieResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InventaireComptageCategorieResponse>> CreateComptageCategorie(
        CreateInventaireComptageCategorieRequest request,
        CancellationToken cancellationToken)
        => StatusCode(
            StatusCodes.Status201Created,
            await comptageService.CreerCategorieAsync(request, cancellationToken));

    [HttpPost("comptage/categories/{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    public async Task<ActionResult<InventaireComptageCategorieResponse>> ValidateComptageCategorie(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await comptageService.ValiderCategorieAsync(id, cancellationToken));

    [HttpPost("comptage/categories/{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    public async Task<ActionResult<InventaireComptageCategorieResponse>> CancelComptageCategorie(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await comptageService.AnnulerCategorieAsync(id, cancellationToken));

    [HttpPost("comptage/fours")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireComptageFourResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InventaireComptageFourResponse>> CreateComptageFour(
        CreateInventaireComptageFourRequest request,
        CancellationToken cancellationToken)
        => StatusCode(
            StatusCodes.Status201Created,
            await comptageService.CreerFourAsync(request, cancellationToken));

    [HttpPost("comptage/fours/{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    public async Task<ActionResult<InventaireComptageFourResponse>> ValidateComptageFour(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await comptageService.ValiderFourAsync(id, cancellationToken));

    [HttpPost("comptage/fours/{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    public async Task<ActionResult<InventaireComptageFourResponse>> CancelComptageFour(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await comptageService.AnnulerFourAsync(id, cancellationToken));

    // ==========================================
    // PARTIE 1 : CONTENEURS / OF
    // ==========================================
    [HttpGet("conteneurs")]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<PagedInventairesConteneursResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedInventairesConteneursResponse>> SearchConteneurs(
        [FromQuery] int? mois,
        [FromQuery] int? annee,
        [FromQuery] string? codeTypeMatiere,
        [FromQuery] string? statut,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.SearchConteneursAsync(mois, annee, codeTypeMatiere, statut, search, page, pageSize, cancellationToken));

    [HttpGet("conteneurs/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<InventaireConteneurResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventaireConteneurResponse>> GetConteneurById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.GetConteneurByIdAsync(id, cancellationToken));

    [HttpPost("conteneurs")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireConteneurResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InventaireConteneurResponse>> CreateConteneur(
        CreateInventaireConteneurRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateConteneurAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetConteneurById), new { id = response.Id }, response);
    }

    [HttpPut("conteneurs/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireConteneurResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireConteneurResponse>> UpdateConteneur(
        Guid id,
        UpdateInventaireConteneurRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateConteneurAsync(id, request, cancellationToken));

    [HttpPost("conteneurs/{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireConteneurResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireConteneurResponse>> ValidateConteneur(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.ValidateConteneurAsync(id, cancellationToken));

    [HttpPost("conteneurs/{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireConteneurResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireConteneurResponse>> CancelConteneur(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.CancelConteneurAsync(id, cancellationToken));

    [HttpDelete("conteneurs/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteConteneur(
        Guid id,
        CancellationToken cancellationToken)
    {
        await service.DeleteConteneurAsync(id, cancellationToken);
        return NoContent();
    }

    // ==========================================
    // PARTIE 2 : CATÉGORIES
    // ==========================================
    [HttpGet("categories")]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<PagedInventairesCategoriesResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedInventairesCategoriesResponse>> SearchCategories(
        [FromQuery] int? mois,
        [FromQuery] int? annee,
        [FromQuery] string? codeTypeMatiere,
        [FromQuery] string? statut,
        [FromQuery] string? categorie,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.SearchCategoriesAsync(mois, annee, codeTypeMatiere, statut, categorie, page, pageSize, cancellationToken));

    [HttpGet("categories/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<InventaireCategorieResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventaireCategorieResponse>> GetCategorieById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.GetCategorieByIdAsync(id, cancellationToken));

    [HttpPost("categories")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireCategorieResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InventaireCategorieResponse>> CreateCategorie(
        CreateInventaireCategorieRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateCategorieAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCategorieById), new { id = response.Id }, response);
    }

    [HttpPut("categories/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireCategorieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireCategorieResponse>> UpdateCategorie(
        Guid id,
        UpdateInventaireCategorieRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateCategorieAsync(id, request, cancellationToken));

    [HttpPost("categories/{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireCategorieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireCategorieResponse>> ValidateCategorie(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.ValidateCategorieAsync(id, cancellationToken));

    [HttpPost("categories/{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireCategorieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireCategorieResponse>> CancelCategorie(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.CancelCategorieAsync(id, cancellationToken));

    [HttpDelete("categories/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCategorie(
        Guid id,
        CancellationToken cancellationToken)
    {
        await service.DeleteCategorieAsync(id, cancellationToken);
        return NoContent();
    }

    // ==========================================
    // PARTIE 3 : FOURS
    // ==========================================
    [HttpGet("fours")]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<PagedInventairesFoursResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedInventairesFoursResponse>> SearchFours(
        [FromQuery] int? mois,
        [FromQuery] int? annee,
        [FromQuery] string? codeTypeMatiere,
        [FromQuery] string? statut,
        [FromQuery] string? codeFour,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await service.SearchFoursAsync(mois, annee, codeTypeMatiere, statut, codeFour, page, pageSize, cancellationToken));

    [HttpGet("fours/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<InventaireFourResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventaireFourResponse>> GetFourById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.GetFourByIdAsync(id, cancellationToken));

    [HttpPost("fours")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireFourResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InventaireFourResponse>> CreateFour(
        CreateInventaireFourRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateFourAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetFourById), new { id = response.Id }, response);
    }

    [HttpPut("fours/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireFourResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireFourResponse>> UpdateFour(
        Guid id,
        UpdateInventaireFourRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateFourAsync(id, request, cancellationToken));

    [HttpPost("fours/{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireFourResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireFourResponse>> ValidateFour(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.ValidateFourAsync(id, cancellationToken));

    [HttpPost("fours/{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireFourResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireFourResponse>> CancelFour(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.CancelFourAsync(id, cancellationToken));

    [HttpDelete("fours/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteFour(
        Guid id,
        CancellationToken cancellationToken)
    {
        await service.DeleteFourAsync(id, cancellationToken);
        return NoContent();
    }

    // ==========================================
    // SYNTHÈSE MENSUELLE
    // ==========================================
    [HttpGet("synthese")]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<InventaireSyntheseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireSyntheseDto>> GetSynthese(
        [FromQuery] int mois,
        [FromQuery] int annee,
        [FromQuery] string? codeTypeMatiere,
        CancellationToken cancellationToken)
        => Ok(await service.GetSyntheseAsync(mois, annee, codeTypeMatiere, cancellationToken));

    // ==========================================
    // ANCIEN MODÈLE / RÉTROCOMPATIBILITÉ
    // ==========================================
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<PagedInventairesFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedInventairesFonderieResponse>> GetAll(
        [FromQuery] string? periode,
        [FromQuery] Guid? typeMatiereId,
        [FromQuery] string? statut,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await service.GetAllAsync(
            new GetInventairesFonderieQuery(periode, typeMatiereId, statut, page, pageSize),
            cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReadInventairesFonderie)]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventaireFonderieResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.GetByIdAsync(new GetInventaireFonderieByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InventaireFonderieResponse>> Create(
        CreateInventaireFonderieRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateAsync(new CreateInventaireFonderieCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireFonderieResponse>> Update(
        Guid id,
        UpdateInventaireFonderieRequest request,
        CancellationToken cancellationToken)
        => Ok(await service.UpdateAsync(new UpdateInventaireFonderieCommand(id, request), cancellationToken));

    [HttpPost("{id:guid}/valider")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireFonderieResponse>> Valider(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.ValiderAsync(new ValiderInventaireFonderieCommand(id), cancellationToken));

    [HttpPost("{id:guid}/annuler")]
    [Authorize(Policy = AuthorizationPolicies.ManageInventairesFonderie)]
    [ProducesResponseType<InventaireFonderieResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InventaireFonderieResponse>> Annuler(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await service.AnnulerAsync(new AnnulerInventaireFonderieCommand(id), cancellationToken));
}
