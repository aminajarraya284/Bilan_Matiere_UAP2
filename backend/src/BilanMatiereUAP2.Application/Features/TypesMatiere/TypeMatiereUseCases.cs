using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.TypesMatiere;
using BilanMatiereUAP2.Domain.Entities;
using FluentValidation;

namespace BilanMatiereUAP2.Application.Features.TypesMatiere;

public sealed record GetTypesMatiereQuery(bool? Actif);
public sealed record GetTypeMatiereByIdQuery(Guid Id);
public sealed record CreateTypeMatiereCommand(CreateTypeMatiereRequest Request);
public sealed record UpdateTypeMatiereCommand(Guid Id, UpdateTypeMatiereRequest Request);
public sealed record ChangeTypeMatiereActivationCommand(Guid Id, ChangeTypeMatiereActivationRequest Request);

public interface ITypeMatiereService
{
    Task<IReadOnlyList<TypeMatiereResponse>> GetAllAsync(GetTypesMatiereQuery query, CancellationToken cancellationToken);
    Task<TypeMatiereResponse> GetByIdAsync(GetTypeMatiereByIdQuery query, CancellationToken cancellationToken);
    Task<TypeMatiereResponse> CreateAsync(CreateTypeMatiereCommand command, CancellationToken cancellationToken);
    Task<TypeMatiereResponse> UpdateAsync(UpdateTypeMatiereCommand command, CancellationToken cancellationToken);
    Task<TypeMatiereResponse> ChangeActivationAsync(
        ChangeTypeMatiereActivationCommand command,
        CancellationToken cancellationToken);
}

public sealed class TypeMatiereService(
    ITypeMatiereRepository repository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IValidator<CreateTypeMatiereRequest> createValidator,
    IValidator<UpdateTypeMatiereRequest> updateValidator) : ITypeMatiereService
{
    public async Task<IReadOnlyList<TypeMatiereResponse>> GetAllAsync(
        GetTypesMatiereQuery query,
        CancellationToken cancellationToken)
    {
        var items = await repository.GetAllAsync(query.Actif, cancellationToken);
        return items.Select(ToResponse).ToArray();
    }

    public async Task<TypeMatiereResponse> GetByIdAsync(
        GetTypeMatiereByIdQuery query,
        CancellationToken cancellationToken)
    {
        return ToResponse(await GetRequiredAsync(query.Id, cancellationToken));
    }

    public async Task<TypeMatiereResponse> CreateAsync(
        CreateTypeMatiereCommand command,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var normalizedCode = TypeMatiere.NormaliserCode(command.Request.Code);
        if (await repository.GetByCodeAsync(normalizedCode, cancellationToken) is not null)
        {
            throw new ConflictException($"Le type matière « {normalizedCode} » existe déjà.");
        }

        var entity = TypeMatiere.Creer(command.Request.Code, command.Request.Libelle, dateTimeProvider.UtcNow);
        await repository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<TypeMatiereResponse> UpdateAsync(
        UpdateTypeMatiereCommand command,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        entity.Modifier(command.Request.Libelle, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<TypeMatiereResponse> ChangeActivationAsync(
        ChangeTypeMatiereActivationCommand command,
        CancellationToken cancellationToken)
    {
        var entity = await GetRequiredAsync(command.Id, cancellationToken);
        if (command.Request.EstActif)
        {
            entity.Activer(dateTimeProvider.UtcNow);
        }
        else
        {
            entity.Desactiver(dateTimeProvider.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    private async Task<TypeMatiere> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Type matière introuvable.");
    }

    private static TypeMatiereResponse ToResponse(TypeMatiere entity)
    {
        return new TypeMatiereResponse(
            entity.Id,
            entity.Code,
            entity.Libelle,
            entity.EstActif,
            entity.DateCreationUtc,
            entity.DateModificationUtc);
    }
}
