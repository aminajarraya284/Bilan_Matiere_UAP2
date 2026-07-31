using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.ControlePoidsFonderie;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.ControlePoidsFonderie;

public sealed record GetControlesPoidsFonderieQuery(
    DateOnly? DateDebut,
    DateOnly? DateFin,
    string? NumeroOF,
    string? CodeArticle,
    Guid? TypeMatiereId,
    string? Statut,
    int Page = 1,
    int PageSize = 20);

public sealed record GetControlePoidsFonderieByIdQuery(Guid Id);
public sealed record CreateControlePoidsFonderieCommand(CreateControlePoidsFonderieRequest Request);
public sealed record UpdateControlePoidsFonderieCommand(Guid Id, UpdateControlePoidsFonderieRequest Request);
public sealed record ValiderControlePoidsFonderieCommand(Guid Id);
public sealed record AnnulerControlePoidsFonderieCommand(Guid Id);

public interface IControlePoidsFonderieService
{
    Task<PagedControlesPoidsFonderieResponse> GetAllAsync(GetControlesPoidsFonderieQuery query, CancellationToken cancellationToken);
    Task<ControlePoidsFonderieResponse> GetByIdAsync(GetControlePoidsFonderieByIdQuery query, CancellationToken cancellationToken);
    Task<ControlePoidsFonderieResponse> CreateAsync(CreateControlePoidsFonderieCommand command, CancellationToken cancellationToken);
    Task<ControlePoidsFonderieResponse> UpdateAsync(UpdateControlePoidsFonderieCommand command, CancellationToken cancellationToken);
    Task<ControlePoidsFonderieResponse> ValiderAsync(ValiderControlePoidsFonderieCommand command, CancellationToken cancellationToken);
    Task<ControlePoidsFonderieResponse> AnnulerAsync(AnnulerControlePoidsFonderieCommand command, CancellationToken cancellationToken);
}

public sealed class ControlePoidsFonderieService(
    IControlePoidsFonderieRepository repository,
    ITypeMatiereRepository typeMatiereRepository,
    IReceptionUsinage2Repository receptionRepository,
    IReceptionUsinage2ContextService receptionContextService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider,
    IValidator<CreateControlePoidsFonderieRequest> createValidator,
    IValidator<UpdateControlePoidsFonderieRequest> updateValidator) : IControlePoidsFonderieService
{
    public async Task<PagedControlesPoidsFonderieResponse> GetAllAsync(
        GetControlesPoidsFonderieQuery query,
        CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var result = await repository.GetPagedAsync(
            new ControlesPoidsFonderieFilter(
                query.DateDebut,
                query.DateFin,
                query.NumeroOF,
                query.CodeArticle,
                query.TypeMatiereId,
                ParseOptionalStatut(query.Statut),
                query.Page,
                query.PageSize),
            cancellationToken);

        return new PagedControlesPoidsFonderieResponse(
            result.Items.Select(item => ToResponse(item)).ToArray(),
            result.Total,
            query.Page,
            query.PageSize);
    }

    public async Task<ControlePoidsFonderieResponse> GetByIdAsync(
        GetControlePoidsFonderieByIdQuery query,
        CancellationToken cancellationToken)
    {
        return ToResponse(await GetRequiredAsync(query.Id, cancellationToken));
    }

    public async Task<ControlePoidsFonderieResponse> CreateAsync(
        CreateControlePoidsFonderieCommand command,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var typeMatiere = await GetRequiredActiveTypeMatiereAsync(command.Request.TypeMatiereId, cancellationToken);
        var entity = Domain.Entities.ControlePoidsFonderie.Creer(
            command.Request.DateControle,
            command.Request.NumeroOF,
            command.Request.CodeArticle,
            typeMatiere.Id,
            command.Request.NumeroLotOuSerie,
            command.Request.PoidsPhysiqueUnitaireKg,
            command.Request.NombreEchantillons,
            command.Request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);

        await repository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity, typeMatiere);
    }

    public async Task<ControlePoidsFonderieResponse> UpdateAsync(
        UpdateControlePoidsFonderieCommand command,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        var typeMatiere = await GetRequiredActiveTypeMatiereAsync(command.Request.TypeMatiereId, cancellationToken);
        entity.Modifier(
            command.Request.DateControle,
            command.Request.NumeroOF,
            command.Request.CodeArticle,
            typeMatiere.Id,
            command.Request.NumeroLotOuSerie,
            command.Request.PoidsPhysiqueUnitaireKg,
            command.Request.NombreEchantillons,
            command.Request.Observation,
            dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity, typeMatiere);
    }

    public async Task<ControlePoidsFonderieResponse> ValiderAsync(
        ValiderControlePoidsFonderieCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        var validationDate = dateTimeProvider.UtcNow;
        entity.Valider(currentUser.UserId, currentUser.UserName, validationDate);

        // La resolution de contexte interroge la base : le controle doit donc etre
        // persiste comme Valide avant de recalculer les receptions concernees.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var receptions = await receptionRepository.GetConcernedByControlePoidsAsync(
            entity.CodeArticle,
            entity.TypeMatiere.Code,
            entity.NumeroOF,
            entity.DateControle,
            cancellationToken);
        foreach (var reception in receptions)
        {
            var context = await receptionContextService.GetAsync(
                reception.NumeroOF,
                reception.DateReception,
                cancellationToken);
            if (context is null)
            {
                continue;
            }

            reception.RafraichirContexteAutomatique(
                ToSnapshot(context),
                currentUser.UserId,
                currentUser.UserName,
                validationDate);
        }

        if (receptions.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(entity);
    }

    public async Task<ControlePoidsFonderieResponse> AnnulerAsync(
        AnnulerControlePoidsFonderieCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        entity.Annuler(dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    private async Task<Domain.Entities.ControlePoidsFonderie> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Contrôle poids Fonderie introuvable.");
    }

    private async Task<TypeMatiere> GetRequiredActiveTypeMatiereAsync(Guid id, CancellationToken cancellationToken)
    {
        var typeMatiere = await typeMatiereRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Type matière introuvable.");
        if (!typeMatiere.EstActif)
        {
            throw new ConflictException("Un type matière désactivé ne peut pas être utilisé.");
        }

        return typeMatiere;
    }

    private static StatutControlePoidsFonderie? ParseOptionalStatut(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<StatutControlePoidsFonderie>(value, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw Validation("statut", "Le statut doit être Brouillon, Valide ou Annule.");
        }

        return parsed;
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw Validation("page", "La page doit être supérieure ou égale à 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw Validation("pageSize", "La taille de page doit être comprise entre 1 et 100.");
        }
    }

    private static ValidationException Validation(string propertyName, string message)
    {
        return new ValidationException([new ValidationFailure(propertyName, message)]);
    }

    private static ReceptionUsinage2.ContexteSnapshot ToSnapshot(Contracts.ReceptionU2ContextResponse context)
        => new(
            context.CodeArticle,
            context.DesignationArticle,
            context.SituationX3,
            context.AtelierOrigine,
            context.CodeTypeMatiere,
            context.QuantiteLienUtilisee,
            context.UniteQuantiteLien,
            context.PoidsTheoriqueUnitaireKg,
            context.PoidsPhysiqueUnitaireKg,
            context.ControlePoidsFonderieId,
            context.TotalCtrlSci,
            context.TotalCtrlEmr,
            context.TotalCtrlTrn,
            context.TotalRebutSci,
            context.TotalRebutEmr,
            context.TotalRebutTrn,
            context.StatutArticle,
            context.StatutAtelier,
            context.StatutTypeMatiere,
            context.StatutNomenclature,
            context.StatutPoidsPhysique,
            context.StatutRapportMachine,
            context.Avertissements,
            context.SourceTypeMatiere,
            context.EligibleAtelier16);

    private static ControlePoidsFonderieResponse ToResponse(
        Domain.Entities.ControlePoidsFonderie entity,
        TypeMatiere? typeMatiere = null)
    {
        var material = typeMatiere ?? entity.TypeMatiere;
        return new ControlePoidsFonderieResponse(
            entity.Id,
            entity.DateControle,
            entity.NumeroOF,
            entity.CodeArticle,
            entity.TypeMatiereId,
            material.Code,
            entity.NumeroLotOuSerie,
            entity.PoidsPhysiqueUnitaireKg,
            entity.NombreEchantillons,
            entity.Observation,
            entity.Statut.ToString(),
            entity.CreeParNom ?? entity.CreeParUtilisateurId?.ToString(),
            entity.CreeLeUtc,
            entity.ValideParNom ?? entity.ValideParUtilisateurId?.ToString(),
            entity.ValideLeUtc);
    }
}
