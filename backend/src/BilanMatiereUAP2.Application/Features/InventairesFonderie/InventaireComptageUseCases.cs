using System.Globalization;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Contracts.InventairesFonderie;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace BilanMatiereUAP2.Application.Features.InventairesFonderie;

public interface IInventaireComptageService
{
    Task<InventaireComptagePeriodeResponse> GetPeriodeAsync(
        string periode,
        CancellationToken cancellationToken = default);

    Task<InventaireComptageConteneurResponse> CreerConteneurAsync(
        CreateInventaireComptageConteneurRequest request,
        CancellationToken cancellationToken = default);

    Task<InventaireComptageCategorieResponse> CreerCategorieAsync(
        CreateInventaireComptageCategorieRequest request,
        CancellationToken cancellationToken = default);

    Task<InventaireComptageFourResponse> CreerFourAsync(
        CreateInventaireComptageFourRequest request,
        CancellationToken cancellationToken = default);

    Task<InventaireComptageConteneurResponse> ValiderConteneurAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InventaireComptageCategorieResponse> ValiderCategorieAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InventaireComptageFourResponse> ValiderFourAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InventaireComptageConteneurResponse> AnnulerConteneurAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InventaireComptageCategorieResponse> AnnulerCategorieAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InventaireComptageFourResponse> AnnulerFourAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Orchestration du comptage physique et de la projection InventairesFonderie.
/// Les créations restent des brouillons et ne touchent jamais la projection.
/// Toute validation ou annulation modifie la ligne détaillée et recalcule sa
/// projection dans la même transaction.
/// </summary>
public sealed class InventaireComptageService(
    IInventaireComptageRepository repository,
    ITypeMatiereRepository typeMatiereRepository,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider) : IInventaireComptageService
{
    public async Task<InventaireComptagePeriodeResponse> GetPeriodeAsync(
        string periode,
        CancellationToken cancellationToken = default)
    {
        var mois = ParsePeriode(periode);
        var conteneurs = await repository.GetConteneursAsync(mois, cancellationToken);
        var categories = await repository.GetCategoriesAsync(mois, cancellationToken);
        var fours = await repository.GetFoursAsync(mois, cancellationToken);
        var types = await typeMatiereRepository.GetAllAsync(true, cancellationToken);

        var totaux = InventaireComptageRules.CalculerTotaux(conteneurs, categories, fours);
        var totauxParType = new List<InventaireComptageTotalResponse>(types.Count);

        foreach (var type in types)
        {
            var total = totaux.FirstOrDefault(item => item.TypeMatiereId == type.Id);
            var projection = await repository.GetProjectionAsync(mois, type.Id, cancellationToken);
            var projectionPrecedente = await repository.GetProjectionAsync(
                InventaireComptageRules.PeriodePrecedente(mois),
                type.Id,
                cancellationToken);

            var stockFin = projection?.StockFinKg ?? total?.StockFinKg ?? 0m;
            var stockDebut = projection?.StockDebutKg ?? projectionPrecedente?.StockFinKg;
            var totalFours = total?.TotalFoursKg ?? 0m;

            totauxParType.Add(new InventaireComptageTotalResponse(
                type.Id,
                type.Code,
                stockDebut,
                stockFin,
                totalFours,
                stockFin,
                stockFin + totalFours,
                stockDebut is null
                    ? ["STOCK_DEBUT_INTROUVABLE"]
                    : Array.Empty<string>()));
        }

        return new InventaireComptagePeriodeResponse(
            FormatPeriode(mois),
            conteneurs.Select(ToResponse).ToArray(),
            categories.Select(ToResponse).ToArray(),
            fours.Select(ToResponse).ToArray(),
            totaux.Select(total => new InventaireTotalParMatiereResponse(
                total.TypeMatiereId,
                types.FirstOrDefault(type => type.Id == total.TypeMatiereId)?.Code ?? string.Empty,
                total.TotalConteneursKg,
                total.TotalCategoriesKg,
                total.TotalFoursKg,
                total.TotalRapportKg,
                total.StockFinKg)).ToArray(),
            totauxParType);
    }

    public async Task<InventaireComptageConteneurResponse> CreerConteneurAsync(
        CreateInventaireComptageConteneurRequest request,
        CancellationToken cancellationToken = default)
    {
        await GarantirTypeActifAsync(request.TypeMatiereId, cancellationToken);
        var ligne = InventaireConteneur.Creer(
            request.DateInventaire,
            request.NumeroConteneur,
            request.NumeroOF,
            request.CodeArticle,
            request.DesignationArticle,
            request.TypeMatiereId,
            request.QuantiteKg,
            request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);

        await repository.AddAsync(ligne, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(await RechargerConteneurAsync(ligne.Id, cancellationToken));
    }

    public async Task<InventaireComptageCategorieResponse> CreerCategorieAsync(
        CreateInventaireComptageCategorieRequest request,
        CancellationToken cancellationToken = default)
    {
        await GarantirTypeActifAsync(request.TypeMatiereId, cancellationToken);
        var ligne = InventaireCategorieComptage.Creer(
            request.Periode,
            request.Categorie,
            request.TypeMatiereId,
            request.QuantiteKg,
            request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);

        await repository.AddAsync(ligne, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(await RechargerCategorieAsync(ligne.Id, cancellationToken));
    }

    public async Task<InventaireComptageFourResponse> CreerFourAsync(
        CreateInventaireComptageFourRequest request,
        CancellationToken cancellationToken = default)
    {
        await GarantirTypeActifAsync(request.TypeMatiereId, cancellationToken);
        var ligne = InventaireFourComptage.Creer(
            request.DateInventaire,
            request.CodeFour,
            request.TypeMatiereId,
            request.QuantiteKg,
            request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);

        await repository.AddAsync(ligne, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(await RechargerFourAsync(ligne.Id, cancellationToken));
    }

    public Task<InventaireComptageConteneurResponse> ValiderConteneurAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => ChangerStatutConteneurAsync(id, valider: true, cancellationToken);

    public Task<InventaireComptageCategorieResponse> ValiderCategorieAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => ChangerStatutCategorieAsync(id, valider: true, cancellationToken);

    public Task<InventaireComptageFourResponse> ValiderFourAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => ChangerStatutFourAsync(id, valider: true, cancellationToken);

    public Task<InventaireComptageConteneurResponse> AnnulerConteneurAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => ChangerStatutConteneurAsync(id, valider: false, cancellationToken);

    public Task<InventaireComptageCategorieResponse> AnnulerCategorieAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => ChangerStatutCategorieAsync(id, valider: false, cancellationToken);

    public Task<InventaireComptageFourResponse> AnnulerFourAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => ChangerStatutFourAsync(id, valider: false, cancellationToken);

    private async Task<InventaireComptageConteneurResponse> ChangerStatutConteneurAsync(
        Guid id,
        bool valider,
        CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
        try
        {
            var ligne = await RechargerConteneurAsync(id, cancellationToken);
            if (valider)
            {
                ligne.Valider(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
            }
            else
            {
                ligne.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
            }

            // Les agrégations sont relues depuis la base. Le premier flush rend le
            // nouveau statut visible dans cette transaction; le second persiste la
            // projection. Les deux écritures restent atomiques.
            await repository.SaveChangesAsync(cancellationToken);
            await RecalculerProjectionAsync(ligne.Periode, ligne.TypeMatiereId, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(ligne);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<InventaireComptageCategorieResponse> ChangerStatutCategorieAsync(
        Guid id,
        bool valider,
        CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
        try
        {
            var ligne = await RechargerCategorieAsync(id, cancellationToken);
            if (valider)
            {
                ligne.Valider(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
            }
            else
            {
                ligne.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
            }

            await repository.SaveChangesAsync(cancellationToken);
            await RecalculerProjectionAsync(ligne.Periode, ligne.TypeMatiereId, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(ligne);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<InventaireComptageFourResponse> ChangerStatutFourAsync(
        Guid id,
        bool valider,
        CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
        try
        {
            var ligne = await RechargerFourAsync(id, cancellationToken);
            if (valider)
            {
                ligne.Valider(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
            }
            else
            {
                ligne.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
            }

            await repository.SaveChangesAsync(cancellationToken);
            await RecalculerProjectionAsync(ligne.Periode, ligne.TypeMatiereId, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(ligne);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task RecalculerProjectionAsync(
        DateOnly periode,
        Guid typeMatiereId,
        CancellationToken cancellationToken)
    {
        var conteneurs = await repository.GetConteneursAsync(periode, cancellationToken);
        var categories = await repository.GetCategoriesAsync(periode, cancellationToken);
        var fours = await repository.GetFoursAsync(periode, cancellationToken);
        var total = InventaireComptageRules.CalculerTotaux(conteneurs, categories, fours)
            .FirstOrDefault(item => item.TypeMatiereId == typeMatiereId);
        var stockFin = total?.StockFinKg ?? 0m;
        var projectionPrecedente = await repository.GetProjectionAsync(
            InventaireComptageRules.PeriodePrecedente(periode),
            typeMatiereId,
            cancellationToken);
        var stockDebut = projectionPrecedente?.StockFinKg;
        var projection = await repository.GetProjectionAsync(periode, typeMatiereId, cancellationToken);

        if (projection is null)
        {
            await repository.AddProjectionAsync(
                InventaireFonderie.Creer(
                    periode,
                    typeMatiereId,
                    stockDebut,
                    stockFin,
                    observation: null,
                    currentUser.UserId,
                    currentUser.UserName,
                    dateTimeProvider.UtcNow),
                cancellationToken);
        }
        else
        {
            projection.RecalculerProjection(
                periode,
                typeMatiereId,
                stockDebut,
                stockFin,
                observation: null,
                currentUser.UserId,
                currentUser.UserName,
                dateTimeProvider.UtcNow);
        }
    }

    private async Task GarantirTypeActifAsync(Guid typeMatiereId, CancellationToken cancellationToken)
    {
        var type = await typeMatiereRepository.GetByIdAsync(typeMatiereId, cancellationToken)
            ?? throw new NotFoundException($"Le type matiere '{typeMatiereId}' est introuvable.");
        if (!type.EstActif)
        {
            throw new ConflictException("Un type matiere desactive ne peut pas etre utilise.");
        }
    }

    private async Task<InventaireConteneur> RechargerConteneurAsync(
        Guid id,
        CancellationToken cancellationToken)
        => await repository.GetConteneurAsync(id, cancellationToken)
           ?? throw new NotFoundException($"Le conteneur de comptage {id} est introuvable.");

    private async Task<InventaireCategorieComptage> RechargerCategorieAsync(
        Guid id,
        CancellationToken cancellationToken)
        => await repository.GetCategorieAsync(id, cancellationToken)
           ?? throw new NotFoundException($"La categorie de comptage {id} est introuvable.");

    private async Task<InventaireFourComptage> RechargerFourAsync(
        Guid id,
        CancellationToken cancellationToken)
        => await repository.GetFourAsync(id, cancellationToken)
           ?? throw new NotFoundException($"Le four de comptage {id} est introuvable.");

    private static DateOnly ParsePeriode(string value)
    {
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var periode))
        {
            throw new ValidationException([
                new ValidationFailure("periode", "La période doit être au format AAAA-MM.")]);
        }

        return new DateOnly(periode.Year, periode.Month, 1);
    }

    private static string FormatPeriode(DateOnly periode) => $"{periode.Year:D4}-{periode.Month:D2}";

    private static InventaireComptageConteneurResponse ToResponse(InventaireConteneur ligne)
        => new(
            ligne.Id,
            ligne.Periode,
            ligne.DateInventaire,
            ligne.NumeroConteneur,
            ligne.NumeroOF,
            ligne.CodeArticle,
            ligne.DesignationArticle,
            ligne.TypeMatiereId,
            ligne.TypeMatiere?.Code ?? string.Empty,
            ligne.QuantiteKg,
            ligne.Observation,
            ligne.Statut.ToString(),
            ligne.CreeLeUtc,
            ligne.ModifieLeUtc,
            ligne.ValideLeUtc,
            ligne.ValideParNom);

    private static InventaireComptageCategorieResponse ToResponse(InventaireCategorieComptage ligne)
        => new(
            ligne.Id,
            ligne.Periode,
            ligne.Categorie,
            ligne.TypeMatiereId,
            ligne.TypeMatiere?.Code ?? string.Empty,
            ligne.QuantiteKg,
            ligne.Observation,
            ligne.Statut.ToString(),
            InclusBilanFonderie: true,
            VisibleRapportInventaire: true,
            ligne.CreeLeUtc,
            ligne.ModifieLeUtc,
            ligne.ValideLeUtc,
            ligne.ValideParNom);

    private static InventaireComptageFourResponse ToResponse(InventaireFourComptage ligne)
        => new(
            ligne.Id,
            ligne.Periode,
            ligne.DateInventaire,
            ligne.CodeFour,
            ligne.TypeMatiereId,
            ligne.TypeMatiere?.Code ?? string.Empty,
            ligne.QuantiteKg,
            ligne.Observation,
            ligne.Statut.ToString(),
            InclusBilanFonderie: InventaireFourComptage.InclusDansBilanFonderie,
            VisibleRapportInventaire: true,
            ligne.CreeLeUtc,
            ligne.ModifieLeUtc,
            ligne.ValideLeUtc,
            ligne.ValideParNom);
}
