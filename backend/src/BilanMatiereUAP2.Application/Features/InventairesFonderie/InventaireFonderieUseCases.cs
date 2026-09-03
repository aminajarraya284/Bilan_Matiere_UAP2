using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.InventairesFonderie;

public sealed record GetInventairesFonderieQuery(
    string? Periode,
    Guid? TypeMatiereId,
    string? Statut,
    int Page = 1,
    int PageSize = 20);

public sealed record GetInventaireFonderieByIdQuery(Guid Id);
public sealed record CreateInventaireFonderieCommand(CreateInventaireFonderieRequest Request);
public sealed record UpdateInventaireFonderieCommand(Guid Id, UpdateInventaireFonderieRequest Request);
public sealed record ValiderInventaireFonderieCommand(Guid Id);
public sealed record AnnulerInventaireFonderieCommand(Guid Id);

public interface IInventaireFonderieService
{
    // --- Partie 1 : Conteneurs / OF ---
    Task<PagedInventairesConteneursResponse> SearchConteneursAsync(
        int? mois,
        int? annee,
        string? codeTypeMatiere,
        string? statut,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<InventaireConteneurResponse> GetConteneurByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<InventaireConteneurResponse> CreateConteneurAsync(
        CreateInventaireConteneurRequest request,
        CancellationToken cancellationToken);

    Task<InventaireConteneurResponse> UpdateConteneurAsync(
        Guid id,
        UpdateInventaireConteneurRequest request,
        CancellationToken cancellationToken);

    Task<InventaireConteneurResponse> ValidateConteneurAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<InventaireConteneurResponse> CancelConteneurAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task DeleteConteneurAsync(
        Guid id,
        CancellationToken cancellationToken);

    // --- Partie 2 : Catégories ---
    Task<PagedInventairesCategoriesResponse> SearchCategoriesAsync(
        int? mois,
        int? annee,
        string? codeTypeMatiere,
        string? statut,
        string? categorie,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<InventaireCategorieResponse> GetCategorieByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<InventaireCategorieResponse> CreateCategorieAsync(
        CreateInventaireCategorieRequest request,
        CancellationToken cancellationToken);

    Task<InventaireCategorieResponse> UpdateCategorieAsync(
        Guid id,
        UpdateInventaireCategorieRequest request,
        CancellationToken cancellationToken);

    Task<InventaireCategorieResponse> ValidateCategorieAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<InventaireCategorieResponse> CancelCategorieAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task DeleteCategorieAsync(
        Guid id,
        CancellationToken cancellationToken);

    // --- Partie 3 : Fours ---
    Task<PagedInventairesFoursResponse> SearchFoursAsync(
        int? mois,
        int? annee,
        string? codeTypeMatiere,
        string? statut,
        string? codeFour,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<InventaireFourResponse> GetFourByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<InventaireFourResponse> CreateFourAsync(
        CreateInventaireFourRequest request,
        CancellationToken cancellationToken);

    Task<InventaireFourResponse> UpdateFourAsync(
        Guid id,
        UpdateInventaireFourRequest request,
        CancellationToken cancellationToken);

    Task<InventaireFourResponse> ValidateFourAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<InventaireFourResponse> CancelFourAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task DeleteFourAsync(
        Guid id,
        CancellationToken cancellationToken);

    // --- Synthèse ---
    Task<InventaireSyntheseDto> GetSyntheseAsync(
        int mois,
        int annee,
        string? codeTypeMatiere,
        CancellationToken cancellationToken);

    // --- Legacy / Ancien modèle ---
    Task<PagedInventairesFonderieResponse> GetAllAsync(GetInventairesFonderieQuery query, CancellationToken cancellationToken);
    Task<InventaireFonderieResponse> GetByIdAsync(GetInventaireFonderieByIdQuery query, CancellationToken cancellationToken);
    Task<InventaireFonderieResponse> CreateAsync(CreateInventaireFonderieCommand command, CancellationToken cancellationToken);
    Task<InventaireFonderieResponse> UpdateAsync(UpdateInventaireFonderieCommand command, CancellationToken cancellationToken);
    Task<InventaireFonderieResponse> ValiderAsync(ValiderInventaireFonderieCommand command, CancellationToken cancellationToken);
    Task<InventaireFonderieResponse> AnnulerAsync(AnnulerInventaireFonderieCommand command, CancellationToken cancellationToken);
}

public sealed class InventaireFonderieService(
    IInventaireFonderieRepository repository,
    ITypeMatiereRepository typeMatiereRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider,
    IValidator<CreateInventaireConteneurRequest> createConteneurValidator,
    IValidator<UpdateInventaireConteneurRequest> updateConteneurValidator,
    IValidator<CreateInventaireCategorieRequest> createCategorieValidator,
    IValidator<UpdateInventaireCategorieRequest> updateCategorieValidator,
    IValidator<CreateInventaireFourRequest> createFourValidator,
    IValidator<UpdateInventaireFourRequest> updateFourValidator,
    IValidator<CreateInventaireFonderieRequest> createLegacyValidator,
    IValidator<UpdateInventaireFonderieRequest> updateLegacyValidator,
    IAuditService? auditService = null) : IInventaireFonderieService
{
    // ==========================================
    // PARTIE 1 : CONTENEURS / OF
    // ==========================================
    public async Task<PagedInventairesConteneursResponse> SearchConteneursAsync(
        int? mois,
        int? annee,
        string? codeTypeMatiere,
        string? statut,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        var criteria = new InventaireConteneurCriteria(
            mois,
            annee,
            codeTypeMatiere,
            ParseOptionalStatutInventaire(statut),
            search);

        var paged = await repository.SearchConteneursAsync(criteria, page, pageSize, cancellationToken);
        return new PagedInventairesConteneursResponse(
            paged.Items.Select(ToResponse).ToList(),
            paged.Total,
            page,
            pageSize);
    }

    public async Task<InventaireConteneurResponse> GetConteneurByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetConteneurByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire conteneur introuvable.");
        return ToResponse(entity);
    }

    public async Task<InventaireConteneurResponse> CreateConteneurAsync(
        CreateInventaireConteneurRequest request,
        CancellationToken cancellationToken)
    {
        await createConteneurValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureValidActiveTypeMatiereAsync(request.CodeTypeMatiere, cancellationToken);

        var entity = InventaireConteneurOf.Creer(
            request.DateInventaire,
            request.NumeroConteneur,
            request.NumeroOF,
            request.CodeTypeMatiere,
            request.PoidsReelKg,
            request.Observation,
            dateTimeProvider.UtcNow);

        await repository.AddConteneurAsync(entity, cancellationToken);
        await AuditerAsync(ActionAudit.Creation, entity, null, Snapshot(entity), "Creation d'une ligne d'inventaire conteneur.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireConteneurResponse> UpdateConteneurAsync(
        Guid id,
        UpdateInventaireConteneurRequest request,
        CancellationToken cancellationToken)
    {
        await updateConteneurValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureValidActiveTypeMatiereAsync(request.CodeTypeMatiere, cancellationToken);

        var entity = await repository.GetConteneurByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire conteneur introuvable.");

        var avant = Snapshot(entity);
        entity.Modifier(
            request.DateInventaire,
            request.NumeroConteneur,
            request.NumeroOF,
            request.CodeTypeMatiere,
            request.PoidsReelKg,
            request.Observation,
            dateTimeProvider.UtcNow);

        await AuditerAsync(ActionAudit.Modification, entity, avant, Snapshot(entity), "Modification d'une ligne d'inventaire conteneur.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireConteneurResponse> ValidateConteneurAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetConteneurByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire conteneur introuvable.");

        var avant = Snapshot(entity);
        entity.Valider(currentUser.UserName ?? currentUser.UserId?.ToString() ?? "Système", dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Validation, entity, avant, Snapshot(entity), "Validation d'une ligne d'inventaire conteneur.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireConteneurResponse> CancelConteneurAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetConteneurByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire conteneur introuvable.");

        var avant = Snapshot(entity);
        entity.Annuler(currentUser.UserName ?? currentUser.UserId?.ToString() ?? "Système", dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Annulation, entity, avant, Snapshot(entity), "Annulation d'une ligne d'inventaire conteneur.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task DeleteConteneurAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetConteneurByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire conteneur introuvable.");

        if (entity.Statut != StatutInventaire.Brouillon)
        {
            throw new ConflictException("Seule une ligne Brouillon peut etre supprimee physiquement.");
        }

        await repository.RemoveConteneurAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // PARTIE 2 : CATÉGORIES
    // ==========================================
    public async Task<PagedInventairesCategoriesResponse> SearchCategoriesAsync(
        int? mois,
        int? annee,
        string? codeTypeMatiere,
        string? statut,
        string? categorie,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        var criteria = new InventaireCategorieCriteria(
            mois,
            annee,
            codeTypeMatiere,
            ParseOptionalStatutInventaire(statut),
            ParseOptionalCategorie(categorie));

        var paged = await repository.SearchCategoriesAsync(criteria, page, pageSize, cancellationToken);
        return new PagedInventairesCategoriesResponse(
            paged.Items.Select(ToResponse).ToList(),
            paged.Total,
            page,
            pageSize);
    }

    public async Task<InventaireCategorieResponse> GetCategorieByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetCategorieByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire categorie introuvable.");
        return ToResponse(entity);
    }

    public async Task<InventaireCategorieResponse> CreateCategorieAsync(
        CreateInventaireCategorieRequest request,
        CancellationToken cancellationToken)
    {
        await createCategorieValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureValidActiveTypeMatiereAsync(request.CodeTypeMatiere, cancellationToken);

        var entity = InventaireCategorie.Creer(
            request.DateInventaire,
            request.Categorie,
            request.CodeTypeMatiere,
            request.PoidsReelKg,
            request.Commentaire,
            dateTimeProvider.UtcNow);

        await repository.AddCategorieAsync(entity, cancellationToken);
        await AuditerAsync(ActionAudit.Creation, entity, null, Snapshot(entity), "Creation d'une ligne d'inventaire categorie.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireCategorieResponse> UpdateCategorieAsync(
        Guid id,
        UpdateInventaireCategorieRequest request,
        CancellationToken cancellationToken)
    {
        await updateCategorieValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureValidActiveTypeMatiereAsync(request.CodeTypeMatiere, cancellationToken);

        var entity = await repository.GetCategorieByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire categorie introuvable.");

        var avant = Snapshot(entity);
        entity.Modifier(
            request.DateInventaire,
            request.Categorie,
            request.CodeTypeMatiere,
            request.PoidsReelKg,
            request.Commentaire,
            dateTimeProvider.UtcNow);

        await AuditerAsync(ActionAudit.Modification, entity, avant, Snapshot(entity), "Modification d'une ligne d'inventaire categorie.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireCategorieResponse> ValidateCategorieAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetCategorieByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire categorie introuvable.");

        var avant = Snapshot(entity);
        entity.Valider(currentUser.UserName ?? currentUser.UserId?.ToString() ?? "Système", dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Validation, entity, avant, Snapshot(entity), "Validation d'une ligne d'inventaire categorie.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireCategorieResponse> CancelCategorieAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetCategorieByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire categorie introuvable.");

        var avant = Snapshot(entity);
        entity.Annuler(currentUser.UserName ?? currentUser.UserId?.ToString() ?? "Système", dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Annulation, entity, avant, Snapshot(entity), "Annulation d'une ligne d'inventaire categorie.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task DeleteCategorieAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetCategorieByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire categorie introuvable.");

        if (entity.Statut != StatutInventaire.Brouillon)
        {
            throw new ConflictException("Seule une ligne Brouillon peut etre supprimee physiquement.");
        }

        await repository.RemoveCategorieAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // PARTIE 3 : FOURS
    // ==========================================
    public async Task<PagedInventairesFoursResponse> SearchFoursAsync(
        int? mois,
        int? annee,
        string? codeTypeMatiere,
        string? statut,
        string? codeFour,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        var criteria = new InventaireFourCriteria(
            mois,
            annee,
            codeTypeMatiere,
            ParseOptionalStatutInventaire(statut),
            codeFour);

        var paged = await repository.SearchFoursAsync(criteria, page, pageSize, cancellationToken);
        return new PagedInventairesFoursResponse(
            paged.Items.Select(ToResponse).ToList(),
            paged.Total,
            page,
            pageSize);
    }

    public async Task<InventaireFourResponse> GetFourByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetFourByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire four introuvable.");
        return ToResponse(entity);
    }

    public async Task<InventaireFourResponse> CreateFourAsync(
        CreateInventaireFourRequest request,
        CancellationToken cancellationToken)
    {
        await createFourValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureValidActiveTypeMatiereAsync(request.CodeTypeMatiere, cancellationToken);

        var entity = InventaireFour.Creer(
            request.DateInventaire,
            request.CodeFour,
            request.CodeTypeMatiere,
            request.QuantiteKg,
            request.Observation,
            dateTimeProvider.UtcNow);

        await repository.AddFourAsync(entity, cancellationToken);
        await AuditerAsync(ActionAudit.Creation, entity, null, Snapshot(entity), "Creation d'une ligne d'inventaire four.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireFourResponse> UpdateFourAsync(
        Guid id,
        UpdateInventaireFourRequest request,
        CancellationToken cancellationToken)
    {
        await updateFourValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureValidActiveTypeMatiereAsync(request.CodeTypeMatiere, cancellationToken);

        var entity = await repository.GetFourByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire four introuvable.");

        var avant = Snapshot(entity);
        entity.Modifier(
            request.DateInventaire,
            request.CodeFour,
            request.CodeTypeMatiere,
            request.QuantiteKg,
            request.Observation,
            dateTimeProvider.UtcNow);

        await AuditerAsync(ActionAudit.Modification, entity, avant, Snapshot(entity), "Modification d'une ligne d'inventaire four.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireFourResponse> ValidateFourAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetFourByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire four introuvable.");

        var avant = Snapshot(entity);
        entity.Valider(currentUser.UserName ?? currentUser.UserId?.ToString() ?? "Système", dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Validation, entity, avant, Snapshot(entity), "Validation d'une ligne d'inventaire four.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireFourResponse> CancelFourAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetFourByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire four introuvable.");

        var avant = Snapshot(entity);
        entity.Annuler(currentUser.UserName ?? currentUser.UserId?.ToString() ?? "Système", dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Annulation, entity, avant, Snapshot(entity), "Annulation d'une ligne d'inventaire four.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task DeleteFourAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetFourByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire four introuvable.");

        if (entity.Statut != StatutInventaire.Brouillon)
        {
            throw new ConflictException("Seule une ligne Brouillon peut etre supprimee physiquement.");
        }

        await repository.RemoveFourAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    // ==========================================
    // SYNTHÈSE MENSUELLE
    // ==========================================
    public async Task<InventaireSyntheseDto> GetSyntheseAsync(
        int mois,
        int annee,
        string? codeTypeMatiere,
        CancellationToken cancellationToken)
    {
        if (mois is < 1 or > 12)
        {
            throw Validation("mois", "Le mois doit etre compris entre 1 et 12.");
        }

        if (annee is < 2000 or > 2100)
        {
            throw Validation("annee", "L'annee est invalide.");
        }

        var rows = await repository.GetSyntheseAsync(mois, annee, codeTypeMatiere, cancellationToken);

        var lignes = rows.Select(r =>
        {
            var totalRapport = r.Partie1Kg + r.Partie2Kg + r.Partie3FourKg;
            var totalBilan = r.Partie1Kg + r.Partie2Kg;
            return new InventaireSyntheseLigneDto(
                r.CodeTypeMatiere,
                r.Partie1Kg,
                r.Partie2Kg,
                r.Partie3FourKg,
                totalRapport,
                totalBilan,
                "Valide");
        }).ToList();

        var totalPartie1 = lignes.Sum(l => l.Partie1Kg);
        var totalPartie2 = lignes.Sum(l => l.Partie2Kg);
        var totalPartie3 = lignes.Sum(l => l.Partie3FourKg);
        var totalRapportGen = totalPartie1 + totalPartie2 + totalPartie3;
        var totalBilanGen = totalPartie1 + totalPartie2;

        return new InventaireSyntheseDto(
            mois,
            annee,
            lignes,
            new InventaireSyntheseTotauxDto(
                totalPartie1,
                totalPartie2,
                totalPartie3,
                totalRapportGen,
                totalBilanGen));
    }

    // ==========================================
    // ANCIEN MODÈLE / RÉTROCOMPATIBILITÉ
    // ==========================================
    public async Task<PagedInventairesFonderieResponse> GetAllAsync(
        GetInventairesFonderieQuery query,
        CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var result = await repository.GetPagedAsync(
            new InventairesFonderieFilter(
                ParseOptionalPeriod(query.Periode),
                query.TypeMatiereId,
                ParseOptionalLegacyStatut(query.Statut),
                query.Page,
                query.PageSize),
            cancellationToken);
        return new PagedInventairesFonderieResponse(
            result.Items.Select(item => ToLegacyResponse(item)).ToArray(),
            result.Total,
            query.Page,
            query.PageSize);
    }

    public async Task<InventaireFonderieResponse> GetByIdAsync(
        GetInventaireFonderieByIdQuery query,
        CancellationToken cancellationToken)
        => ToLegacyResponse(await GetRequiredLegacyAsync(query.Id, cancellationToken));

    public async Task<InventaireFonderieResponse> CreateAsync(
        CreateInventaireFonderieCommand command,
        CancellationToken cancellationToken)
    {
        await createLegacyValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var periode = ParsePeriod(command.Request.Periode);
        var typeMatiere = await GetRequiredActiveTypeMatiereByIdAsync(command.Request.TypeMatiereId, cancellationToken);
        await EnsureNoDuplicateLegacyAsync(periode, typeMatiere.Id, null, cancellationToken);
        var entity = InventaireFonderie.Creer(
            periode,
            typeMatiere.Id,
            command.Request.StockDebutKg,
            command.Request.StockFinKg,
            command.Request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);
        await repository.AddAsync(entity, cancellationToken);
        await AuditerAsync(ActionAudit.Creation, entity, null, Snapshot(entity), "Creation d'un inventaire Fonderie.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToLegacyResponse(entity, typeMatiere);
    }

    public async Task<InventaireFonderieResponse> UpdateAsync(
        UpdateInventaireFonderieCommand command,
        CancellationToken cancellationToken)
    {
        await updateLegacyValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var entity = await GetRequiredLegacyAsync(command.Id, cancellationToken);
        var periode = ParsePeriod(command.Request.Periode);
        var typeMatiere = await GetRequiredActiveTypeMatiereByIdAsync(command.Request.TypeMatiereId, cancellationToken);
        await EnsureNoDuplicateLegacyAsync(periode, typeMatiere.Id, entity.Id, cancellationToken);
        var avant = Snapshot(entity);
        entity.Modifier(
            periode,
            typeMatiere.Id,
            command.Request.StockDebutKg,
            command.Request.StockFinKg,
            command.Request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Modification, entity, avant, Snapshot(entity), "Modification d'un inventaire Fonderie.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToLegacyResponse(entity, typeMatiere);
    }

    public async Task<InventaireFonderieResponse> ValiderAsync(
        ValiderInventaireFonderieCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await GetRequiredLegacyAsync(command.Id, cancellationToken);
        await EnsureNoDuplicateLegacyAsync(entity.Periode, entity.TypeMatiereId, entity.Id, cancellationToken);
        var avant = Snapshot(entity);
        entity.Valider(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Validation, entity, avant, Snapshot(entity), "Validation d'un inventaire Fonderie.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToLegacyResponse(entity);
    }

    public async Task<InventaireFonderieResponse> AnnulerAsync(
        AnnulerInventaireFonderieCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await GetRequiredLegacyAsync(command.Id, cancellationToken);
        var avant = Snapshot(entity);
        entity.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Annulation, entity, avant, Snapshot(entity), "Annulation d'un inventaire Fonderie.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToLegacyResponse(entity);
    }

    // ==========================================
    // HELPERS
    // ==========================================
    private async Task EnsureValidActiveTypeMatiereAsync(string codeTypeMatiere, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codeTypeMatiere))
        {
            throw Validation("codeTypeMatiere", "Le code type matiere est obligatoire.");
        }

        var normalizedCode = codeTypeMatiere.Trim().ToUpperInvariant();
        var typeMatiere = await typeMatiereRepository.GetByCodeAsync(normalizedCode, cancellationToken)
            ?? throw new NotFoundException($"Le type matiere '{codeTypeMatiere}' est introuvable.");

        if (!typeMatiere.EstActif)
        {
            throw new ConflictException($"Le type matiere '{codeTypeMatiere}' est desactive.");
        }
    }

    private async Task<TypeMatiere> GetRequiredActiveTypeMatiereByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var typeMatiere = await typeMatiereRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Type matiere introuvable.");
        if (!typeMatiere.EstActif)
        {
            throw new ConflictException("Un type matiere desactive ne peut pas etre utilise.");
        }

        return typeMatiere;
    }

    private async Task EnsureNoDuplicateLegacyAsync(
        DateOnly periode,
        Guid typeMatiereId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (await repository.ExistsActiveAsync(periode, typeMatiereId, excludeId, cancellationToken))
        {
            throw new ConflictException(
                "Un inventaire non annule existe deja pour cette periode et ce type matiere.");
        }
    }

    private async Task<InventaireFonderie> GetRequiredLegacyAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire Fonderie introuvable.");

    private static StatutInventaire? ParseOptionalStatutInventaire(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<StatutInventaire>(value, true, out var result) ? result : null;
    }

    private static CategorieInventaireFonderie? ParseOptionalCategorie(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<CategorieInventaireFonderie>(value, true, out var result) ? result : null;
    }

    private static DateOnly? ParseOptionalPeriod(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : ParsePeriod(value);

    private static DateOnly ParsePeriod(string value)
    {
        if (!DateTime.TryParseExact(
                value,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            throw Validation("periode", "La periode doit respecter le format YYYY-MM.");
        }

        return new DateOnly(parsed.Year, parsed.Month, 1);
    }

    private static StatutInventaireFonderie? ParseOptionalLegacyStatut(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<StatutInventaireFonderie>(value, true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw Validation("page", "La page doit etre superieure ou egale a 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw Validation("pageSize", "La taille de page doit etre comprise entre 1 et 100.");
        }
    }

    private static ValidationException Validation(string propertyName, string message)
        => new([new ValidationFailure(propertyName, message)]);

    private Task AuditerAsync(
        ActionAudit action,
        InventaireConteneurOf entity,
        IReadOnlyDictionary<string, object?>? avant,
        IReadOnlyDictionary<string, object?> apres,
        string resume,
        CancellationToken cancellationToken)
        => EnregistrerAuditAsync(
            action,
            nameof(InventaireConteneurOf),
            entity.Id,
            $"{entity.DateInventaire:yyyy-MM}|{entity.CodeTypeMatiere}|{entity.Id}",
            avant,
            apres,
            resume,
            cancellationToken);

    private Task AuditerAsync(
        ActionAudit action,
        InventaireCategorie entity,
        IReadOnlyDictionary<string, object?>? avant,
        IReadOnlyDictionary<string, object?> apres,
        string resume,
        CancellationToken cancellationToken)
        => EnregistrerAuditAsync(
            action,
            nameof(InventaireCategorie),
            entity.Id,
            $"{entity.DateInventaire:yyyy-MM}|{entity.CodeTypeMatiere}|{entity.Id}",
            avant,
            apres,
            resume,
            cancellationToken);

    private Task AuditerAsync(
        ActionAudit action,
        InventaireFour entity,
        IReadOnlyDictionary<string, object?>? avant,
        IReadOnlyDictionary<string, object?> apres,
        string resume,
        CancellationToken cancellationToken)
        => EnregistrerAuditAsync(
            action,
            nameof(InventaireFour),
            entity.Id,
            $"{entity.DateInventaire:yyyy-MM}|{entity.CodeTypeMatiere}|{entity.Id}",
            avant,
            apres,
            resume,
            cancellationToken);

    private Task AuditerAsync(
        ActionAudit action,
        InventaireFonderie entity,
        IReadOnlyDictionary<string, object?>? avant,
        IReadOnlyDictionary<string, object?> apres,
        string resume,
        CancellationToken cancellationToken)
        => EnregistrerAuditAsync(
            action,
            nameof(InventaireFonderie),
            entity.Id,
            $"{entity.Periode:yyyy-MM}|{entity.TypeMatiereId}|{entity.Id}",
            avant,
            apres,
            resume,
            cancellationToken);

    private Task EnregistrerAuditAsync(
        ActionAudit action,
        string entite,
        Guid id,
        string referenceMetier,
        IReadOnlyDictionary<string, object?>? avant,
        IReadOnlyDictionary<string, object?> apres,
        string resume,
        CancellationToken cancellationToken)
        => auditService is null
            ? Task.CompletedTask
            : auditService.EnregistrerAsync(
                new AuditEntry(
                    action,
                    DomaineAudit.InventaireFonderie,
                    entite,
                    id.ToString(),
                    referenceMetier,
                    nameof(InventaireFonderieService),
                    resume,
                    avant,
                    apres),
                cancellationToken);

    private static IReadOnlyDictionary<string, object?> Snapshot(InventaireConteneurOf entity)
        => new Dictionary<string, object?>
        {
            ["periode"] = entity.DateInventaire.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            ["date"] = entity.DateInventaire,
            ["numeroConteneur"] = entity.NumeroConteneur,
            ["numeroOF"] = entity.NumeroOF,
            ["typeMatiere"] = entity.CodeTypeMatiere,
            ["poidsKg"] = entity.PoidsReelKg,
            ["statut"] = entity.Statut.ToString()
        };

    private static IReadOnlyDictionary<string, object?> Snapshot(InventaireCategorie entity)
        => new Dictionary<string, object?>
        {
            ["periode"] = entity.DateInventaire.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            ["date"] = entity.DateInventaire,
            ["categorie"] = entity.Categorie.ToString(),
            ["typeMatiere"] = entity.CodeTypeMatiere,
            ["poidsKg"] = entity.PoidsReelKg,
            ["statut"] = entity.Statut.ToString()
        };

    private static IReadOnlyDictionary<string, object?> Snapshot(InventaireFour entity)
        => new Dictionary<string, object?>
        {
            ["periode"] = entity.DateInventaire.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            ["date"] = entity.DateInventaire,
            ["codeFour"] = entity.CodeFour,
            ["typeMatiere"] = entity.CodeTypeMatiere,
            ["poidsKg"] = entity.QuantiteKg,
            ["statut"] = entity.Statut.ToString()
        };

    private static IReadOnlyDictionary<string, object?> Snapshot(InventaireFonderie entity)
        => new Dictionary<string, object?>
        {
            ["periode"] = entity.Periode.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            ["typeMatiereId"] = entity.TypeMatiereId,
            ["stockDebutKg"] = entity.StockDebutKg,
            ["stockFinKg"] = entity.StockFinKg,
            ["statut"] = entity.Statut.ToString()
        };

    private static InventaireConteneurResponse ToResponse(InventaireConteneurOf entity)
        => new(
            entity.Id,
            entity.DateInventaire,
            entity.NumeroConteneur,
            entity.NumeroOF,
            entity.CodeTypeMatiere,
            entity.PoidsReelKg,
            entity.Observation,
            entity.Statut.ToString(),
            entity.CreeLeUtc,
            entity.ModifieLeUtc,
            entity.ValideLeUtc,
            entity.ValidePar,
            entity.AnnuleLeUtc,
            entity.AnnulePar);

    private static InventaireCategorieResponse ToResponse(InventaireCategorie entity)
        => new(
            entity.Id,
            entity.DateInventaire,
            entity.Categorie.ToString(),
            entity.CodeTypeMatiere,
            entity.PoidsReelKg,
            entity.Commentaire,
            entity.Statut.ToString(),
            entity.InclusBilanFonderie,
            entity.VisibleRapportInventaire,
            entity.CreeLeUtc,
            entity.ModifieLeUtc,
            entity.ValideLeUtc,
            entity.ValidePar,
            entity.AnnuleLeUtc,
            entity.AnnulePar);

    private static InventaireFourResponse ToResponse(InventaireFour entity)
        => new(
            entity.Id,
            entity.DateInventaire,
            entity.Mois,
            entity.Annee,
            entity.CodeFour,
            entity.CodeTypeMatiere,
            entity.QuantiteKg,
            entity.Observation,
            entity.Statut.ToString(),
            entity.InclusBilanFonderie,
            entity.VisibleRapportInventaire,
            entity.CreeLeUtc,
            entity.ModifieLeUtc,
            entity.ValideLeUtc,
            entity.ValidePar,
            entity.AnnuleLeUtc,
            entity.AnnulePar);

    private static InventaireFonderieResponse ToLegacyResponse(
        InventaireFonderie entity,
        TypeMatiere? typeMatiere = null)
    {
        var material = typeMatiere ?? entity.TypeMatiere;
        return new InventaireFonderieResponse(
            entity.Id,
            entity.Periode.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            entity.TypeMatiereId,
            material.Code,
            material.Libelle,
            entity.StockDebutKg,
            entity.StockFinKg,
            entity.Observation,
            entity.Statut.ToString(),
            entity.CreeParNom ?? entity.CreeParUtilisateurId?.ToString(),
            entity.CreeLeUtc,
            entity.ModifieParNom ?? entity.ModifieParUtilisateurId?.ToString(),
            entity.ModifieLeUtc,
            entity.ValideParNom ?? entity.ValideParUtilisateurId?.ToString(),
            entity.ValideLeUtc,
            entity.AnnuleParNom ?? entity.AnnuleParUtilisateurId?.ToString(),
            entity.AnnuleLeUtc);
    }
}
