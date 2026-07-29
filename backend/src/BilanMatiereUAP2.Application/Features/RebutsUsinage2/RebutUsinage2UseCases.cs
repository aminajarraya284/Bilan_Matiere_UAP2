using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.RebutsUsinage2;
using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Features.RebutsUsinage2;

public interface IRebutUsinage2Service
{
    Task<PagedRebutsUsinage2Response> QueryAsync(int? annee, int? mois, string? numeroOF, string? codeArticle, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RebutUsinage2Response> CreateAsync(CreateRebutUsinage2Request request, CancellationToken cancellationToken = default);
    Task<RebutUsinage2Response> UpdateAsync(Guid id, UpdateRebutUsinage2Request request, CancellationToken cancellationToken = default);
    Task<RebutUsinage2Response> ValiderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RebutUsinage2Response> AnnulerAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class RebutUsinage2Service(
    IRebutUsinage2Repository repository,
    ICurrentUserService currentUser) : IRebutUsinage2Service
{
    public async Task<PagedRebutsUsinage2Response> QueryAsync(
        int? annee,
        int? mois,
        string? numeroOF,
        string? codeArticle,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await repository.QueryAsync(annee, mois, numeroOF, codeArticle, page, pageSize, cancellationToken);
        return new PagedRebutsUsinage2Response(items.Select(ToResponse).ToArray(), total);
    }

    public async Task<RebutUsinage2Response> CreateAsync(CreateRebutUsinage2Request request, CancellationToken cancellationToken = default)
    {
        var entity = RebutUsinage2.Creer(
            request.DateDeclaration,
            request.NumeroOF,
            request.CodeArticle,
            request.QuantiteRebutPieces,
            request.Motif,
            request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            DateTime.UtcNow);

        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<RebutUsinage2Response> UpdateAsync(Guid id, UpdateRebutUsinage2Request request, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        entity.Modifier(
            request.DateDeclaration,
            request.NumeroOF,
            request.CodeArticle,
            request.QuantiteRebutPieces,
            request.Motif,
            request.Observation,
            DateTime.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<RebutUsinage2Response> ValiderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        entity.Valider(currentUser.UserId, currentUser.UserName, DateTime.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<RebutUsinage2Response> AnnulerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        entity.Annuler(DateTime.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    private async Task<RebutUsinage2> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException($"Rebut Usinage 2 {id} introuvable.");

    private static RebutUsinage2Response ToResponse(RebutUsinage2 entity)
        => new(
            entity.Id,
            entity.DateDeclaration,
            $"{entity.Annee:D4}-{entity.Mois:D2}",
            entity.NumeroOF,
            entity.CodeArticle,
            entity.QuantiteRebutPieces,
            entity.Motif,
            entity.Observation,
            entity.Statut.ToString(),
            entity.CreeParNom,
            entity.CreeLeUtc,
            entity.ValideParNom,
            entity.ValideLeUtc);
}
