using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Contracts.ReceptionsUsinage2;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.ReceptionsUsinage2;

public sealed record GetReceptionsUsinage2Query(
    DateOnly? PeriodeDebut,
    DateOnly? PeriodeFin,
    string? NumeroOF,
    string? CodeArticle,
    string? CodeTypeMatiere,
    string? Atelier,
    string? Statut,
    int Page = 1,
    int PageSize = 20);

public sealed record GetReceptionUsinage2ByIdQuery(Guid Id);
public sealed record CreateReceptionUsinage2Command(CreateReceptionUsinage2Request Request);
public sealed record UpdateReceptionUsinage2Command(Guid Id, UpdateReceptionUsinage2Request Request);
public sealed record ValiderReceptionUsinage2Command(Guid Id, ValiderReceptionUsinage2Request Request);
public sealed record AnnulerReceptionUsinage2Command(Guid Id);
public sealed record RecalculerContexteReceptionUsinage2Command(Guid Id);

public interface IReceptionUsinage2Service
{
    Task<PagedReceptionsUsinage2Response> GetAllAsync(GetReceptionsUsinage2Query query, CancellationToken cancellationToken);
    Task<ReceptionUsinage2Response> GetByIdAsync(GetReceptionUsinage2ByIdQuery query, CancellationToken cancellationToken);
    Task<ReceptionUsinage2Response> CreateAsync(CreateReceptionUsinage2Command command, CancellationToken cancellationToken);
    Task<ReceptionUsinage2Response> UpdateAsync(UpdateReceptionUsinage2Command command, CancellationToken cancellationToken);
    Task<ReceptionUsinage2Response> ValiderAsync(ValiderReceptionUsinage2Command command, CancellationToken cancellationToken);
    Task<ReceptionUsinage2Response> AnnulerAsync(AnnulerReceptionUsinage2Command command, CancellationToken cancellationToken);
    Task<ReceptionUsinage2Response> RecalculerContexteAsync(RecalculerContexteReceptionUsinage2Command command, CancellationToken cancellationToken);
}

public sealed class ReceptionUsinage2Service(
    IReceptionUsinage2Repository repository,
    IReceptionUsinage2ContextService contextService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider,
    IParametreApplicationProvider parametres,
    IValidator<CreateReceptionUsinage2Request> createValidator,
    IValidator<UpdateReceptionUsinage2Request> updateValidator) : IReceptionUsinage2Service
{
    public async Task<PagedReceptionsUsinage2Response> GetAllAsync(GetReceptionsUsinage2Query query, CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var result = await repository.GetPagedAsync(
            new ReceptionsUsinage2Filter(
                query.PeriodeDebut,
                query.PeriodeFin,
                NormalizeOptional(query.NumeroOF),
                NormalizeOptional(query.CodeArticle),
                NormalizeOptional(query.CodeTypeMatiere),
                NormalizeOptional(query.Atelier),
                query.Statut,
                query.Page,
                query.PageSize),
            cancellationToken);

        var items = new List<ReceptionUsinage2Response>();
        foreach (var item in result.Items)
        {
            items.Add(await ToResponseAsync(item, cancellationToken));
        }

        return new PagedReceptionsUsinage2Response(items, result.Total, query.Page, query.PageSize);
    }

    public async Task<ReceptionUsinage2Response> GetByIdAsync(GetReceptionUsinage2ByIdQuery query, CancellationToken cancellationToken)
        => await ToResponseAsync(await GetRequiredAsync(query.Id, cancellationToken), cancellationToken);

    public async Task<ReceptionUsinage2Response> CreateAsync(CreateReceptionUsinage2Command command, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(command.Request, cancellationToken);

        var context = await contextService.GetAsync(command.Request.NumeroOF, command.Request.DateReception, cancellationToken)
            ?? ContextIntrouvable(command.Request.NumeroOF);
        if (context.Avertissements.Any(avertissement =>
                string.Equals(avertissement, "OF_X3_INTROUVABLE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(avertissement, "OF_INTROUVABLE", StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(context.StatutContexte, "OF_X3_INTROUVABLE", StringComparison.OrdinalIgnoreCase))
        {
            throw Validation("numeroOF", "OF_X3_INTROUVABLE");
        }

        var entity = ReceptionUsinage2.Creer(
            command.Request.NumeroOF,
            command.Request.DateReception,
            command.Request.NombrePiecesReceptionnees,
            command.Request.PoidsTotalReceptionneKg,
            command.Request.Origine,
            command.Request.Observation,
            ToSnapshot(context),
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);

        await repository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(entity, cancellationToken);
    }

    public async Task<ReceptionUsinage2Response> UpdateAsync(UpdateReceptionUsinage2Command command, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        entity.Modifier(
            command.Request.NombrePiecesReceptionnees,
            command.Request.PoidsTotalReceptionneKg,
            command.Request.Origine,
            command.Request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(entity, cancellationToken);
    }

    public async Task<ReceptionUsinage2Response> ValiderAsync(ValiderReceptionUsinage2Command command, CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        var estAdmin = string.Equals(currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        var derogation = command.Request.DerogationAdmin && estAdmin;
        if (command.Request.DerogationAdmin && !estAdmin)
        {
            throw Validation("derogationAdmin", "Seul un administrateur peut appliquer une dérogation.");
        }

        entity.Valider(derogation, command.Request.DerogationJustification, currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(entity, cancellationToken);
    }

    public async Task<ReceptionUsinage2Response> AnnulerAsync(AnnulerReceptionUsinage2Command command, CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        entity.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(entity, cancellationToken);
    }

    public async Task<ReceptionUsinage2Response> RecalculerContexteAsync(RecalculerContexteReceptionUsinage2Command command, CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        var context = await contextService.GetAsync(entity.NumeroOF, entity.DateReception, cancellationToken)
            ?? ContextIntrouvable(entity.NumeroOF);
        entity.RecalculerContexte(ToSnapshot(context), currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(entity, cancellationToken);
    }

    private async Task<ReceptionUsinage2Response> ToResponseAsync(ReceptionUsinage2 entity, CancellationToken cancellationToken)
    {
        decimal? poidsTheoriqueTotal = entity.PoidsTheoriqueUnitaireKg is not null
            ? entity.NombrePiecesReceptionnees * entity.PoidsTheoriqueUnitaireKg.Value
            : null;

        decimal? ecartUnitaire = entity.PoidsPhysiqueUnitaireKg is not null && entity.PoidsTheoriqueUnitaireKg is not null
            ? entity.PoidsPhysiqueUnitaireKg.Value - entity.PoidsTheoriqueUnitaireKg.Value
            : null;

        decimal? tauxEcartPct = BilanMetierRules.CalculerTaux(
            ecartUnitaire,
            entity.PoidsTheoriqueUnitaireKg,
            valeurAbsolueNumerateur: true);

        var seuilEcartFonderie = await parametres.GetDecimalAsync(
            ParametresApplicationCles.SeuilEcartFonderie,
            cancellationToken);
        bool? depasseSeuil = tauxEcartPct is not null && seuilEcartFonderie is not null
            ? tauxEcartPct.Value > seuilEcartFonderie.Value
            : null;

        decimal? totalPiecesMois = null;
        decimal? quantiteNonReceptionnee = null;
        if (!string.IsNullOrWhiteSpace(entity.CodeArticle))
        {
            var receptionsDuMois = await repository.GetValidesByOfAndArticleAsync(
                entity.NumeroOF, entity.CodeArticle, entity.DateReception.Year, entity.DateReception.Month, cancellationToken);
            totalPiecesMois = receptionsDuMois.Sum(r => r.NombrePiecesReceptionnees);
            if (entity.TotalCtrlEmr is not null && entity.TotalRebutEmr is not null)
            {
                quantiteNonReceptionnee = entity.TotalCtrlEmr.Value - entity.TotalRebutEmr.Value - totalPiecesMois.Value;
            }
        }

        var avertissements = string.IsNullOrWhiteSpace(entity.AvertissementsJson)
            ? Array.Empty<string>()
            : System.Text.Json.JsonSerializer.Deserialize<string[]>(entity.AvertissementsJson) ?? Array.Empty<string>();

        return new ReceptionUsinage2Response(
            entity.Id,
            entity.NumeroOF,
            entity.DateReception,
            entity.NombrePiecesReceptionnees,
            entity.PoidsTotalReceptionneKg,
            entity.Origine,
            entity.Observation,
            entity.CodeArticle,
            entity.DesignationArticle,
            entity.SituationX3,
            entity.AtelierOrigine,
            entity.CodeTypeMatiere,
            entity.QuantiteLienUtilisee,
            entity.UniteQuantiteLien,
            entity.PoidsTheoriqueUnitaireKg,
            poidsTheoriqueTotal,
            entity.PoidsPhysiqueUnitaireKg,
            entity.ControlePoidsFonderieId,
            ecartUnitaire,
            tauxEcartPct,
            depasseSeuil,
            entity.TotalCtrlSci,
            entity.TotalCtrlEmr,
            entity.TotalCtrlTrn,
            entity.TotalRebutSci,
            entity.TotalRebutEmr,
            entity.TotalRebutTrn,
            totalPiecesMois,
            quantiteNonReceptionnee,
            entity.StatutArticle,
            entity.StatutAtelier,
            entity.StatutTypeMatiere,
            entity.StatutNomenclature,
            entity.StatutPoidsPhysique,
            entity.StatutRapportMachine,
            avertissements,
            entity.StatutReception.ToString(),
            entity.DerogationJustification,
            entity.CreeParNom ?? entity.CreeParUtilisateurId?.ToString(),
            entity.DateCreationUtc,
            entity.ModifieParNom ?? entity.ModifieParUtilisateurId?.ToString(),
            entity.DateModificationUtc,
            entity.SourceTypeMatiere,
            entity.EligibleAtelier16);
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

    private async Task<ReceptionUsinage2> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Réception Usinage 2 introuvable.");

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static ReceptionU2ContextResponse ContextIntrouvable(string numeroOF)
        => new(
            numeroOF.Trim().ToUpperInvariant(), null, null, null, null, null,
            null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, 0, 0,
            "ArticleIntrouvable", "AtelierIntrouvable", "MouvementAbsent", "NomenclatureIntrouvable", "PoidsPhysiqueManquant", "RapportMachineIntrouvable",
            ["OF_X3_INTROUVABLE", "ARTICLE_OF_INTROUVABLE"],
            StatutContexte: "OF_X3_INTROUVABLE");

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1) throw Validation("page", "La page doit être supérieure ou égale à 1.");
        if (pageSize is < 1 or > 100) throw Validation("pageSize", "La taille de page doit être comprise entre 1 et 100.");
    }

    private static ValidationException Validation(string propertyName, string message)
        => new([new ValidationFailure(propertyName, message)]);
}
