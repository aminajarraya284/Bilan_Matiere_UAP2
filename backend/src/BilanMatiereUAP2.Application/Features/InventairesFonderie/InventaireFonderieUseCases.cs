using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
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
    IValidator<CreateInventaireFonderieRequest> createValidator,
    IValidator<UpdateInventaireFonderieRequest> updateValidator) : IInventaireFonderieService
{
    public async Task<PagedInventairesFonderieResponse> GetAllAsync(
        GetInventairesFonderieQuery query,
        CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var result = await repository.GetPagedAsync(
            new InventairesFonderieFilter(
                ParseOptionalPeriod(query.Periode),
                query.TypeMatiereId,
                ParseOptionalStatut(query.Statut),
                query.Page,
                query.PageSize),
            cancellationToken);
        return new PagedInventairesFonderieResponse(
            result.Items.Select(item => ToResponse(item)).ToArray(),
            result.Total,
            query.Page,
            query.PageSize);
    }

    public async Task<InventaireFonderieResponse> GetByIdAsync(
        GetInventaireFonderieByIdQuery query,
        CancellationToken cancellationToken)
        => ToResponse(await GetRequiredAsync(query.Id, cancellationToken));

    public async Task<InventaireFonderieResponse> CreateAsync(
        CreateInventaireFonderieCommand command,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var periode = ParsePeriod(command.Request.Periode);
        var typeMatiere = await GetRequiredActiveTypeMatiereAsync(command.Request.TypeMatiereId, cancellationToken);
        await EnsureNoDuplicateAsync(periode, typeMatiere.Id, null, cancellationToken);
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
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity, typeMatiere);
    }

    public async Task<InventaireFonderieResponse> UpdateAsync(
        UpdateInventaireFonderieCommand command,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        var periode = ParsePeriod(command.Request.Periode);
        var typeMatiere = await GetRequiredActiveTypeMatiereAsync(command.Request.TypeMatiereId, cancellationToken);
        await EnsureNoDuplicateAsync(periode, typeMatiere.Id, entity.Id, cancellationToken);
        entity.Modifier(
            periode,
            typeMatiere.Id,
            command.Request.StockDebutKg,
            command.Request.StockFinKg,
            command.Request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity, typeMatiere);
    }

    public async Task<InventaireFonderieResponse> ValiderAsync(
        ValiderInventaireFonderieCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        await EnsureNoDuplicateAsync(entity.Periode, entity.TypeMatiereId, entity.Id, cancellationToken);
        entity.Valider(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<InventaireFonderieResponse> AnnulerAsync(
        AnnulerInventaireFonderieCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        entity.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    private async Task EnsureNoDuplicateAsync(
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

    private async Task<InventaireFonderie> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Inventaire Fonderie introuvable.");

    private async Task<TypeMatiere> GetRequiredActiveTypeMatiereAsync(Guid id, CancellationToken cancellationToken)
    {
        var typeMatiere = await typeMatiereRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Type matiere introuvable.");
        if (!typeMatiere.EstActif)
        {
            throw new ConflictException("Un type matiere desactive ne peut pas etre utilise.");
        }

        return typeMatiere;
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

    private static StatutInventaireFonderie? ParseOptionalStatut(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<StatutInventaireFonderie>(value, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw Validation("statut", "Le statut doit etre Brouillon, Valide ou Annule.");
        }

        return parsed;
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

    private static InventaireFonderieResponse ToResponse(
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
