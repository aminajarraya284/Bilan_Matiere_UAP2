using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.PostesProcessus;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.PostesProcessus;

public interface IPosteProcessusService
{
    Task<PagedPostesProcessusResponse> GetPagedAsync(
        string? poste,
        string? origine,
        bool? actif,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PosteProcessusResponse> CreateAsync(
        CreatePosteProcessusRequest request,
        CancellationToken cancellationToken = default);
    Task<PosteProcessusResponse> UpdateAsync(
        Guid id,
        UpdatePosteProcessusRequest request,
        CancellationToken cancellationToken = default);
    Task<PosteProcessusResponse> ChangeActivationAsync(
        Guid id,
        ChangePosteProcessusActivationRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosteProcessusHistoriqueResponse>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosteNonMappeResponse>> GetNonMappedAsync(
        CancellationToken cancellationToken = default);
    Task<RecalculClassificationPostesResponse> RecalculateAsync(
        CancellationToken cancellationToken = default);
}

public sealed class PosteProcessusService(
    IPosteProcessusRepository repository,
    IReportMachineRepository reportMachineRepository,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider) : IPosteProcessusService
{
    public async Task<PagedPostesProcessusResponse> GetPagedAsync(
        string? poste,
        string? origine,
        bool? actif,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(page, pageSize);
        // Filtre facultatif : absence d'origine = aucun filtre, et non une origine par defaut.
        OrigineProcessus? parsedOrigin = string.IsNullOrWhiteSpace(origine) ? null : ParseOrigin(origine);
        var result = await repository.GetPagedAsync(
            new PostesProcessusFilter(
                NormalizeOptional(poste),
                parsedOrigin,
                actif,
                page,
                pageSize),
            cancellationToken);
        return new PagedPostesProcessusResponse(
            result.Items.Select(ToResponse).ToArray(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<PosteProcessusResponse> CreateAsync(
        CreatePosteProcessusRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = RefPosteProcessus.NormaliserCodePoste(request.Poste);
        if (await repository.GetByCodeAsync(code, cancellationToken) is not null)
        {
            throw new ConflictException($"Le poste {code} existe deja.");
        }

        var now = dateTimeProvider.UtcNow;
        var entity = RefPosteProcessus.Creer(
            code,
            request.Libelle,
            ParseOrigin(request.OrigineProcessus),
            currentUser.UserName,
            now);
        await repository.AddAsync(entity, cancellationToken);
        await repository.AddHistoryAsync(
            RefPosteProcessusHistorique.Capturer(entity, "Creation", currentUser.UserName, now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<PosteProcessusResponse> UpdateAsync(
        Guid id,
        UpdatePosteProcessusRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        var now = dateTimeProvider.UtcNow;
        entity.Modifier(
            request.Libelle,
            ParseOrigin(request.OrigineProcessus),
            currentUser.UserName,
            now);
        await repository.AddHistoryAsync(
            RefPosteProcessusHistorique.Capturer(entity, "Modification", currentUser.UserName, now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<PosteProcessusResponse> ChangeActivationAsync(
        Guid id,
        ChangePosteProcessusActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetRequiredAsync(id, cancellationToken);
        var now = dateTimeProvider.UtcNow;
        entity.ChangerActivation(request.Actif, currentUser.UserName, now);
        await repository.AddHistoryAsync(
            RefPosteProcessusHistorique.Capturer(
                entity,
                request.Actif ? "Activation" : "Desactivation",
                currentUser.UserName,
                now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<IReadOnlyList<PosteProcessusHistoriqueResponse>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _ = await GetRequiredAsync(id, cancellationToken);
        return (await repository.GetHistoryAsync(id, cancellationToken))
            .Select(history => new PosteProcessusHistoriqueResponse(
                history.Id,
                history.PosteProcessusId,
                history.CodePoste,
                ToCode(history.OrigineProcessus),
                history.Actif,
                history.Action,
                history.Utilisateur,
                history.DateUtc))
            .ToArray();
    }

    public async Task<IReadOnlyList<PosteNonMappeResponse>> GetNonMappedAsync(
        CancellationToken cancellationToken = default)
    {
        var mapped = (await repository.GetAllAsync(true, cancellationToken))
            .Select(item => item.CodePoste)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (await reportMachineRepository.GetPostesNonMappesAsync(cancellationToken))
            .Where(item => !mapped.Contains(item.CodePoste))
            .Select(item => new PosteNonMappeResponse(
                item.CodePoste,
                item.NombreLignes,
                item.QuantiteTotale,
                item.QuantiteRebutTotale,
                item.PremiereDate,
                item.DerniereDate,
                item.ExemplesArticles,
                item.ExemplesOF))
            .ToArray();
    }

    public async Task<RecalculClassificationPostesResponse> RecalculateAsync(
        CancellationToken cancellationToken = default)
    {
        var mappings = (await repository.GetAllAsync(true, cancellationToken))
            .ToDictionary(item => item.CodePoste, item => item.OrigineProcessus, StringComparer.OrdinalIgnoreCase);
        var transaction = await reportMachineRepository.BeginTransactionAsync(cancellationToken);
        try
        {
            var rows = await reportMachineRepository.GetAllActiveTrackedAsync(cancellationToken);
            var modified = 0;
            var counts = Enum.GetValues<OrigineProcessus>()
                .ToDictionary(ToCode, _ => 0, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var newOrigin = mappings.TryGetValue(row.CodePoste, out var mappedOrigin)
                    ? mappedOrigin
                    : OrigineProcessusRules.DeterminerOrigineProcessus(row.CodePoste);
                if (row.OrigineProcessus != newOrigin)
                {
                    row.RecalculerOrigineProcessus(newOrigin);
                    modified++;
                }
                counts[ToCode(newOrigin)]++;
            }

            await reportMachineRepository.SaveChangesAsync(cancellationToken);
            await reportMachineRepository.CommitTransactionAsync(transaction, cancellationToken);
            return new RecalculClassificationPostesResponse(
                rows.Count,
                modified,
                rows.Count - modified,
                counts);
        }
        catch
        {
            await reportMachineRepository.RollbackTransactionAsync(transaction, cancellationToken);
            throw;
        }
    }

    private async Task<RefPosteProcessus> GetRequiredAsync(
        Guid id,
        CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException("Poste processus introuvable.");

    private static PosteProcessusResponse ToResponse(RefPosteProcessus entity)
        => new(
            entity.Id,
            entity.CodePoste,
            entity.LibellePoste,
            ToCode(entity.OrigineProcessus),
            entity.IsActive,
            entity.DateCreationUtc,
            entity.DateModificationUtc,
            entity.CreePar,
            entity.ModifiePar);

    private static OrigineProcessus ParseOrigin(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        return normalized switch
        {
            "CTRL" => OrigineProcessus.Ctrl,
            "SCI" => OrigineProcessus.Sci,
            "EMR" => OrigineProcessus.Emr,
            "TRN" => OrigineProcessus.Trn,
            "U2" => OrigineProcessus.U2,
            "FON" => OrigineProcessus.Fon,
            _ => throw new ValidationException(
                [new ValidationFailure("origineProcessus", "L'origine doit etre CTRL, SCI, EMR, TRN, U2 ou FON.")])
        };
    }

    private static string ToCode(OrigineProcessus origin)
        => origin switch
        {
            OrigineProcessus.Ctrl => "CTRL",
            OrigineProcessus.Sci => "SCI",
            OrigineProcessus.Emr => "EMR",
            OrigineProcessus.Trn => "TRN",
            OrigineProcessus.U2 => "U2",
            OrigineProcessus.Fon => "FON",
            _ => "INCONNUE"
        };

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 200)
        {
            throw new ValidationException(
                [new ValidationFailure("page", "La page doit etre positive et pageSize compris entre 1 et 200.")]);
        }
    }
}
