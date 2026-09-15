using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Contracts.DechetsFonderie;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.DechetsFonderie;

public sealed record GetDechetsFonderieQuery(
    string Periode,
    Guid? TypeMatiereId,
    string? TypeDechet,
    string? Origine = null,
    string? Statut = null,
    int Page = 1,
    int PageSize = 20);

public sealed record GetDechetFonderieByIdQuery(Guid Id);
public sealed record CreateDechetFonderieCommand(CreateDechetFonderieRequest Request);
public sealed record UpdateDechetFonderieCommand(Guid Id, UpdateDechetFonderieRequest Request);
public sealed record DeleteDechetFonderieCommand(Guid Id);
public sealed record ValiderDechetFonderieCommand(Guid Id);
public sealed record AnnulerDechetFonderieCommand(Guid Id);
public sealed record GetResumeDechetsFonderieQuery(string Periode);

public interface IDechetFonderieService
{
    Task<PagedDechetsFonderieResponse> GetAllAsync(GetDechetsFonderieQuery query, CancellationToken cancellationToken);
    Task<DechetFonderieResponse> GetByIdAsync(GetDechetFonderieByIdQuery query, CancellationToken cancellationToken);
    Task<DechetFonderieResponse> CreateAsync(CreateDechetFonderieCommand command, CancellationToken cancellationToken);
    Task<DechetFonderieResponse> UpdateAsync(UpdateDechetFonderieCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(DeleteDechetFonderieCommand command, CancellationToken cancellationToken);
    Task<DechetFonderieResponse> ValiderAsync(ValiderDechetFonderieCommand command, CancellationToken cancellationToken);
    Task<DechetFonderieResponse> AnnulerAsync(AnnulerDechetFonderieCommand command, CancellationToken cancellationToken);
    Task<ResumeDechetsFonderieResponse> GetResumeAsync(
        GetResumeDechetsFonderieQuery query,
        CancellationToken cancellationToken);
}

public sealed class DechetFonderieService(
    IDechetFonderieRepository dechetRepository,
    ITypeMatiereRepository typeMatiereRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider,
    IValidator<CreateDechetFonderieRequest> createValidator,
    IValidator<UpdateDechetFonderieRequest> updateValidator,
    IAuditService? auditService = null) : IDechetFonderieService
{
    public async Task<PagedDechetsFonderieResponse> GetAllAsync(
        GetDechetsFonderieQuery query,
        CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var (start, end) = ParsePeriod(query.Periode);
        var typeDechet = ParseOptionalTypeDechet(query.TypeDechet);
        var origine = ParseOptionalOrigine(query.Origine);
        var statut = ParseOptionalStatut(query.Statut);
        var result = await dechetRepository.GetPagedAsync(
            new DechetsFonderieFilter(
                start,
                end,
                query.TypeMatiereId,
                typeDechet,
                origine,
                statut,
                query.Page,
                query.PageSize),
            cancellationToken);

        return new PagedDechetsFonderieResponse(
            result.Items.Select(element => ToResponse(element)).ToArray(),
            result.Total,
            query.Page,
            query.PageSize);
    }

    public async Task<DechetFonderieResponse> GetByIdAsync(
        GetDechetFonderieByIdQuery query,
        CancellationToken cancellationToken)
    {
        return ToResponse(await GetRequiredAsync(query.Id, cancellationToken));
    }

    public async Task<DechetFonderieResponse> CreateAsync(
        CreateDechetFonderieCommand command,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var typeMatiere = await GetRequiredActiveTypeMatiereAsync(command.Request.TypeMatiereId, cancellationToken);
        var entity = DechetFonderie.Creer(
            command.Request.DateDeclaration,
            typeMatiere.Id,
            ParseTypeDechet(command.Request.TypeDechet),
            ParseOrigine(command.Request.Origine),
            command.Request.PoidsKg,
            command.Request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);

        await dechetRepository.AddAsync(entity, cancellationToken);
        await AuditerAsync(ActionAudit.Creation, entity, null, Snapshot(entity), "Creation d'un dechet Fonderie.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity, typeMatiere);
    }

    public async Task<DechetFonderieResponse> UpdateAsync(
        UpdateDechetFonderieCommand command,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        var typeMatiere = await GetRequiredActiveTypeMatiereAsync(command.Request.TypeMatiereId, cancellationToken);
        var avant = Snapshot(entity);
        entity.Modifier(
            command.Request.DateDeclaration,
            typeMatiere.Id,
            ParseTypeDechet(command.Request.TypeDechet),
            ParseOrigine(command.Request.Origine),
            command.Request.PoidsKg,
            command.Request.Observation,
            dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Modification, entity, avant, Snapshot(entity), "Modification d'un dechet Fonderie.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity, typeMatiere);
    }

    public async Task DeleteAsync(DeleteDechetFonderieCommand command, CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        var avant = Snapshot(entity);
        entity.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Annulation, entity, avant, Snapshot(entity), "Annulation d'un dechet Fonderie.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<DechetFonderieResponse> ValiderAsync(
        ValiderDechetFonderieCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        var avant = Snapshot(entity);
        entity.Valider(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Validation, entity, avant, Snapshot(entity), "Validation d'un dechet Fonderie.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<DechetFonderieResponse> AnnulerAsync(
        AnnulerDechetFonderieCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        var avant = Snapshot(entity);
        entity.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await AuditerAsync(ActionAudit.Annulation, entity, avant, Snapshot(entity), "Annulation d'un dechet Fonderie.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<ResumeDechetsFonderieResponse> GetResumeAsync(
        GetResumeDechetsFonderieQuery query,
        CancellationToken cancellationToken)
    {
        var (start, end) = ParsePeriod(query.Periode);
        var data = await dechetRepository.GetResumeAsync(start, end, cancellationToken);
        var items = data.Select(item => new ResumeDechetsFonderieItemResponse(
            item.TypeMatiereId,
            item.TypeMatiereCode,
            item.TotalBavureKg,
            item.TotalPoudreKg,
            item.TotalBavureKg + item.TotalPoudreKg)).ToArray();

        return new ResumeDechetsFonderieResponse(query.Periode, items, items.Sum(item => item.TotalKg));
    }

    private async Task<DechetFonderie> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dechetRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Déchet Fonderie introuvable.");
    }

    private async Task<TypeMatiere> GetRequiredActiveTypeMatiereAsync(Guid id, CancellationToken cancellationToken)
    {
        var typeMatiere = await typeMatiereRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Type matière introuvable.");
        if (!typeMatiere.EstActif)
        {
            throw new ConflictException("Un type matière désactivé ne peut pas être utilisé pour un déchet.");
        }

        return typeMatiere;
    }

    private static (DateOnly Start, DateOnly End) ParsePeriod(string periode)
    {
        if (!DateTime.TryParseExact(
                periode,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            throw Validation("periode", "La période doit respecter le format YYYY-MM.");
        }

        var start = new DateOnly(parsed.Year, parsed.Month, 1);
        return (start, start.AddMonths(1));
    }

    private static TypeDechet? ParseOptionalTypeDechet(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<TypeDechet>(value, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw Validation("typeDechet", "Le type de déchet doit être Bavure ou Poudre.");
        }

        return parsed;
    }

    private static TypeDechet ParseTypeDechet(string value)
    {
        return Enum.Parse<TypeDechet>(value, true);
    }

    private static OrigineDechet? ParseOptionalOrigine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<OrigineDechet>(value, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw Validation("origine", "L'origine doit etre Fonderie, SCI, EMR, TRN ou U2.");
        }

        return parsed;
    }

    private static OrigineDechet ParseOrigine(string value)
        => ParseOptionalOrigine(value)
            ?? throw Validation("origine", "L'origine est obligatoire.");

    private static StatutDechet? ParseOptionalStatut(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<StatutDechet>(value, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw Validation("statut", "Le statut doit etre Brouillon, Valide ou Annule.");
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

    private Task AuditerAsync(
        ActionAudit action,
        DechetFonderie entity,
        IReadOnlyDictionary<string, object?>? avant,
        IReadOnlyDictionary<string, object?> apres,
        string resume,
        CancellationToken cancellationToken)
        => auditService is null
            ? Task.CompletedTask
            : auditService.EnregistrerAsync(
                new AuditEntry(
                    action,
                    DomaineAudit.DechetFonderie,
                    nameof(DechetFonderie),
                    entity.Id.ToString(),
                    $"{entity.DateDeclaration:yyyy-MM}|{entity.TypeMatiereId}|{entity.Id}",
                    nameof(DechetFonderieService),
                    resume,
                    avant,
                    apres),
                cancellationToken);

    private static IReadOnlyDictionary<string, object?> Snapshot(DechetFonderie entity)
        => new Dictionary<string, object?>
        {
            ["date"] = entity.DateDeclaration,
            ["typeMatiereId"] = entity.TypeMatiereId,
            ["typeDechet"] = entity.TypeDechet.ToString(),
            ["origine"] = entity.Origine.ToString(),
            ["poidsKg"] = entity.PoidsKg,
            ["statut"] = entity.Statut.ToString()
        };

    private static DechetFonderieResponse ToResponse(DechetFonderie entity, TypeMatiere? typeMatiere = null)
    {
        var material = typeMatiere ?? entity.TypeMatiere;
        return new DechetFonderieResponse(
            entity.Id,
            entity.DateDeclaration,
            entity.SemaineIso,
            entity.TypeMatiereId,
            material.Code,
            material.Libelle,
            entity.TypeDechet.ToString(),
            entity.PoidsKg,
            entity.Observation,
            entity.Statut.ToString(),
            entity.CreeParNom ?? entity.CreeParUtilisateurId?.ToString(),
            entity.DateCreationUtc,
            entity.DateModificationUtc,
            entity.Origine.ToString(),
            entity.ValideParNom ?? entity.ValideParUtilisateurId?.ToString(),
            entity.ValideLeUtc,
            entity.AnnuleParNom ?? entity.AnnuleParUtilisateurId?.ToString(),
            entity.AnnuleLeUtc);
    }
}
