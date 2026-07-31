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
    Task<InventaireComptagePeriodeResponse> GetPeriodeAsync(string periode, CancellationToken cancellationToken = default);

    Task<InventaireConteneurResponse> CreerConteneurAsync(CreateInventaireConteneurRequest request, CancellationToken cancellationToken = default);
    Task<InventaireCategorieResponse> CreerCategorieAsync(CreateInventaireCategorieRequest request, CancellationToken cancellationToken = default);
    Task<InventaireFourResponse> CreerFourAsync(CreateInventaireFourRequest request, CancellationToken cancellationToken = default);

    Task<InventaireConteneurResponse> ValiderConteneurAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventaireCategorieResponse> ValiderCategorieAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventaireFourResponse> ValiderFourAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InventaireConteneurResponse> AnnulerConteneurAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventaireCategorieResponse> AnnulerCategorieAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventaireFourResponse> AnnulerFourAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Comptage physique d'inventaire Fonderie.
///
/// Le total des parties 1 et 2 (lignes VALIDES) constitue le STOCK FIN de la periode.
/// Les fours sont comptes et restitues, mais EXCLUS de ce stock. L'agregation elle-meme
/// vit dans <see cref="InventaireComptageRules"/> : ce service ne fait qu'orchestrer.
/// </summary>
public sealed class InventaireComptageService(
    IInventaireComptageRepository repository,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTimeProvider) : IInventaireComptageService
{
    private const string UniteKg = "KG";

    public async Task<InventaireComptagePeriodeResponse> GetPeriodeAsync(string periode, CancellationToken cancellationToken = default)
    {
        var mois = ParsePeriode(periode);
        var conteneurs = await repository.GetConteneursAsync(mois, cancellationToken);
        var categories = await repository.GetCategoriesAsync(mois, cancellationToken);
        var fours = await repository.GetFoursAsync(mois, cancellationToken);

        var totaux = InventaireComptageRules.CalculerTotaux(conteneurs, categories, fours);
        var libelles = conteneurs.Select(l => (l.TypeMatiereId, Code: l.TypeMatiere?.Code))
            .Concat(categories.Select(l => (l.TypeMatiereId, Code: l.TypeMatiere?.Code)))
            .Concat(fours.Select(l => (l.TypeMatiereId, Code: l.TypeMatiere?.Code)))
            .Where(item => item.Code is not null)
            .GroupBy(item => item.TypeMatiereId)
            .ToDictionary(groupe => groupe.Key, groupe => groupe.First().Code!);

        return new InventaireComptagePeriodeResponse(
            $"{mois.Year:D4}-{mois.Month:D2}",
            conteneurs.Select(ToResponse).ToArray(),
            categories.Select(ToResponse).ToArray(),
            fours.Select(ToResponse).ToArray(),
            totaux.Select(total => new InventaireTotalParMatiereResponse(
                total.TypeMatiereId,
                libelles.TryGetValue(total.TypeMatiereId, out var code) ? code : string.Empty,
                total.TotalConteneursKg,
                total.TotalCategoriesKg,
                total.TotalFoursKg,
                total.TotalRapportKg,
                total.StockFinKg)).ToArray());
    }

    // ------------------------------------------------------------- creation

    public async Task<InventaireConteneurResponse> CreerConteneurAsync(CreateInventaireConteneurRequest request, CancellationToken cancellationToken = default)
    {
        var ligne = InventaireConteneur.Creer(
            request.DateInventaire,
            request.NumeroConteneur,
            request.OfDestination,
            request.CodeArticle,
            request.DesignationArticle,
            request.TypeMatiereId,
            request.QuantiteReelle,
            request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);
        await repository.AddAsync(ligne, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(await RechargerConteneurAsync(ligne.Id, cancellationToken));
    }

    public async Task<InventaireCategorieResponse> CreerCategorieAsync(CreateInventaireCategorieRequest request, CancellationToken cancellationToken = default)
    {
        var ligne = InventaireCategorieComptage.Creer(
            new DateOnly(request.Annee, request.Mois, 1),
            request.Categorie,
            request.TypeMatiereId,
            request.QuantiteReelle,
            request.Commentaire,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);
        await repository.AddAsync(ligne, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(await RechargerCategorieAsync(ligne.Id, cancellationToken));
    }

    public async Task<InventaireFourResponse> CreerFourAsync(CreateInventaireFourRequest request, CancellationToken cancellationToken = default)
    {
        var ligne = InventaireFourComptage.Creer(
            request.DateInventaire,
            request.Four,
            request.TypeMatiereId,
            request.Quantite,
            request.Observation,
            currentUser.UserId,
            currentUser.UserName,
            dateTimeProvider.UtcNow);
        await repository.AddAsync(ligne, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(await RechargerFourAsync(ligne.Id, cancellationToken));
    }

    // ------------------------------------------------------- cycle de vie

    public async Task<InventaireConteneurResponse> ValiderConteneurAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ligne = await RechargerConteneurAsync(id, cancellationToken);
        ligne.Valider(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(ligne);
    }

    public async Task<InventaireCategorieResponse> ValiderCategorieAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ligne = await RechargerCategorieAsync(id, cancellationToken);
        ligne.Valider(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(ligne);
    }

    public async Task<InventaireFourResponse> ValiderFourAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ligne = await RechargerFourAsync(id, cancellationToken);
        ligne.Valider(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(ligne);
    }

    public async Task<InventaireConteneurResponse> AnnulerConteneurAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ligne = await RechargerConteneurAsync(id, cancellationToken);
        ligne.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(ligne);
    }

    public async Task<InventaireCategorieResponse> AnnulerCategorieAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ligne = await RechargerCategorieAsync(id, cancellationToken);
        ligne.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(ligne);
    }

    public async Task<InventaireFourResponse> AnnulerFourAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ligne = await RechargerFourAsync(id, cancellationToken);
        ligne.Annuler(currentUser.UserId, currentUser.UserName, dateTimeProvider.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(ligne);
    }

    // ------------------------------------------------------------- outils

    private async Task<InventaireConteneur> RechargerConteneurAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetConteneurAsync(id, cancellationToken)
           ?? throw new NotFoundException($"Le conteneur d'inventaire {id} est introuvable.");

    private async Task<InventaireCategorieComptage> RechargerCategorieAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetCategorieAsync(id, cancellationToken)
           ?? throw new NotFoundException($"La categorie d'inventaire {id} est introuvable.");

    private async Task<InventaireFourComptage> RechargerFourAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetFourAsync(id, cancellationToken)
           ?? throw new NotFoundException($"Le four d'inventaire {id} est introuvable.");

    private static DateOnly ParsePeriode(string periode)
    {
        var parties = (periode ?? string.Empty).Split('-');
        if (parties.Length != 2 || !int.TryParse(parties[0], out var annee) || !int.TryParse(parties[1], out var mois)
            || mois is < 1 or > 12)
        {
            throw new ValidationException([new ValidationFailure("periode", "La période doit être au format AAAA-MM.")]);
        }

        return new DateOnly(annee, mois, 1);
    }

    private static InventaireConteneurResponse ToResponse(InventaireConteneur ligne)
        => new(
            ligne.Id,
            ligne.DateInventaire,
            ligne.Periode.Month,
            ligne.Periode.Year,
            ligne.NumeroConteneur,
            ligne.NumeroOF,
            ligne.CodeArticle,
            ligne.DesignationArticle,
            ligne.TypeMatiereId,
            ligne.TypeMatiere?.Code ?? string.Empty,
            ligne.QuantiteKg,
            UniteKg,
            ligne.Observation,
            ligne.Statut.ToString());

    private static InventaireCategorieResponse ToResponse(InventaireCategorieComptage ligne)
        => new(
            ligne.Id,
            ligne.Periode.Month,
            ligne.Periode.Year,
            ligne.Categorie,
            ligne.TypeMatiereId,
            ligne.TypeMatiere?.Code ?? string.Empty,
            ligne.QuantiteKg,
            UniteKg,
            ligne.Observation,
            ligne.Statut.ToString(),
            InclusDansBilanFonderie: true,
            VisibleDansRapportInventaire: true);

    private static InventaireFourResponse ToResponse(InventaireFourComptage ligne)
        => new(
            ligne.Id,
            ligne.DateInventaire,
            ligne.Periode.Month,
            ligne.Periode.Year,
            ligne.CodeFour,
            ligne.TypeMatiereId,
            ligne.TypeMatiere?.Code ?? string.Empty,
            ligne.QuantiteKg,
            UniteKg,
            ligne.Observation,
            ligne.Statut.ToString(),
            // Invariant : un four n'entre jamais dans le bilan.
            InclusDansBilanFonderie: InventaireFourComptage.InclusDansBilanFonderie,
            VisibleDansRapportInventaire: true);
}
