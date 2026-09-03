using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Contracts.RebutsUsinage2;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

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
    ICurrentUserService currentUser,
    IOfContextResolver ofContextResolver,
    IAuditService? auditService = null) : IRebutUsinage2Service
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
        await AuditerAsync(ActionAudit.Creation, entity, null, Snapshot(entity), "Creation d'un rebut Usinage 2.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<RebutUsinage2Response> UpdateAsync(Guid id, UpdateRebutUsinage2Request request, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        var avant = Snapshot(entity);
        entity.Modifier(
            request.DateDeclaration,
            request.NumeroOF,
            request.CodeArticle,
            request.QuantiteRebutPieces,
            request.Motif,
            request.Observation,
            DateTime.UtcNow);
        await AuditerAsync(ActionAudit.Modification, entity, avant, Snapshot(entity), "Modification d'un rebut Usinage 2.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<RebutUsinage2Response> ValiderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        var avant = Snapshot(entity);
        var contexte = await ofContextResolver.ResolveAsync(entity.NumeroOF, entity.DateDeclaration, cancellationToken);
        if (contexte.Anomalies.Contains("OF_X3_INTROUVABLE", StringComparer.OrdinalIgnoreCase))
        {
            throw new ConflictException("OF_X3_INTROUVABLE");
        }
        if (!contexte.EligibleCalculUap2)
        {
            throw new ConflictException("HORS_ATELIER_UAP2");
        }

        entity.Valider(currentUser.UserId, currentUser.UserName, DateTime.UtcNow);
        await AuditerAsync(ActionAudit.Validation, entity, avant, Snapshot(entity), "Validation d'un rebut Usinage 2.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<RebutUsinage2Response> AnnulerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        var avant = Snapshot(entity);
        entity.Annuler(DateTime.UtcNow);
        await AuditerAsync(ActionAudit.Annulation, entity, avant, Snapshot(entity), "Annulation d'un rebut Usinage 2.", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    private async Task<RebutUsinage2> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException($"Rebut Usinage 2 {id} introuvable.");

    private Task AuditerAsync(
        ActionAudit action,
        RebutUsinage2 entity,
        IReadOnlyDictionary<string, object?>? avant,
        IReadOnlyDictionary<string, object?> apres,
        string resume,
        CancellationToken cancellationToken)
        => auditService is null
            ? Task.CompletedTask
            : auditService.EnregistrerAsync(
                new AuditEntry(
                    action,
                    DomaineAudit.RebutUsinage2,
                    nameof(RebutUsinage2),
                    entity.Id.ToString(),
                    $"{entity.NumeroOF}|{entity.CodeArticle}|{entity.Id}",
                    nameof(RebutUsinage2Service),
                    resume,
                    avant,
                    apres),
                cancellationToken);

    private static IReadOnlyDictionary<string, object?> Snapshot(RebutUsinage2 entity)
        => new Dictionary<string, object?>
        {
            ["date"] = entity.DateDeclaration,
            ["numeroOF"] = entity.NumeroOF,
            ["codeArticle"] = entity.CodeArticle,
            ["quantitePieces"] = entity.QuantiteRebutPieces,
            ["motif"] = entity.Motif,
            ["statut"] = entity.Statut.ToString()
        };

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
