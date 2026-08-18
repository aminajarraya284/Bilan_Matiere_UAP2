using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class InventaireFonderieRepository(BilanMatiereUAP2DbContext dbContext)
    : IInventaireFonderieRepository
{
    // ==========================================
    // PARTIE 1 : CONTENEURS / OF
    // ==========================================
    public async Task<PagedResult<InventaireConteneurOf>> SearchConteneursAsync(
        InventaireConteneurCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.InventairesConteneursOf.AsNoTracking().AsQueryable();

        if (criteria.Mois.HasValue && criteria.Annee.HasValue)
        {
            var dateDebut = new DateOnly(criteria.Annee.Value, criteria.Mois.Value, 1);
            var dateFin = dateDebut.AddMonths(1);
            query = query.Where(x => x.DateInventaire >= dateDebut && x.DateInventaire < dateFin);
        }
        else if (criteria.Annee.HasValue)
        {
            query = query.Where(x => x.DateInventaire.Year == criteria.Annee.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.CodeTypeMatiere))
        {
            var code = criteria.CodeTypeMatiere.Trim().ToUpperInvariant();
            query = query.Where(x => x.CodeTypeMatiere == code);
        }

        if (criteria.Statut.HasValue)
        {
            query = query.Where(x => x.Statut == criteria.Statut.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var s = criteria.Search.Trim().ToUpperInvariant();
            query = query.Where(x => x.NumeroConteneur.Contains(s) || x.NumeroOF.Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.DateInventaire)
            .ThenBy(x => x.NumeroConteneur)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<InventaireConteneurOf>(items, total);
    }

    public Task<InventaireConteneurOf?> GetConteneurByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.InventairesConteneursOf.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddConteneurAsync(InventaireConteneurOf entity, CancellationToken cancellationToken)
        => await dbContext.InventairesConteneursOf.AddAsync(entity, cancellationToken);

    public Task RemoveConteneurAsync(InventaireConteneurOf entity, CancellationToken cancellationToken)
    {
        dbContext.InventairesConteneursOf.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<bool> ConteneurExisteAsync(
        DateOnly dateInventaire,
        string numeroConteneur,
        string numeroOF,
        string codeTypeMatiere,
        CancellationToken cancellationToken)
    {
        var normConteneur = numeroConteneur.Trim().ToUpperInvariant();
        var normOf = numeroOF.Trim().ToUpperInvariant();
        var normCode = codeTypeMatiere.Trim().ToUpperInvariant();

        return dbContext.InventairesConteneursOf.AsNoTracking().AnyAsync(x =>
            x.DateInventaire == dateInventaire &&
            x.NumeroConteneur == normConteneur &&
            x.NumeroOF == normOf &&
            x.CodeTypeMatiere == normCode &&
            x.Statut != StatutInventaire.Annule,
            cancellationToken);
    }

    // ==========================================
    // PARTIE 2 : CATÉGORIES
    // ==========================================
    public async Task<PagedResult<InventaireCategorie>> SearchCategoriesAsync(
        InventaireCategorieCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.InventairesCategories.AsNoTracking().AsQueryable();

        if (criteria.Mois.HasValue && criteria.Annee.HasValue)
        {
            var dateDebut = new DateOnly(criteria.Annee.Value, criteria.Mois.Value, 1);
            var dateFin = dateDebut.AddMonths(1);
            query = query.Where(x => x.DateInventaire >= dateDebut && x.DateInventaire < dateFin);
        }
        else if (criteria.Annee.HasValue)
        {
            query = query.Where(x => x.DateInventaire.Year == criteria.Annee.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.CodeTypeMatiere))
        {
            var code = criteria.CodeTypeMatiere.Trim().ToUpperInvariant();
            query = query.Where(x => x.CodeTypeMatiere == code);
        }

        if (criteria.Statut.HasValue)
        {
            query = query.Where(x => x.Statut == criteria.Statut.Value);
        }

        if (criteria.Categorie.HasValue)
        {
            query = query.Where(x => x.Categorie == criteria.Categorie.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.DateInventaire)
            .ThenBy(x => x.CodeTypeMatiere)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<InventaireCategorie>(items, total);
    }

    public Task<InventaireCategorie?> GetCategorieByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.InventairesCategories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddCategorieAsync(InventaireCategorie entity, CancellationToken cancellationToken)
        => await dbContext.InventairesCategories.AddAsync(entity, cancellationToken);

    public Task RemoveCategorieAsync(InventaireCategorie entity, CancellationToken cancellationToken)
    {
        dbContext.InventairesCategories.Remove(entity);
        return Task.CompletedTask;
    }

    // ==========================================
    // PARTIE 3 : FOURS
    // ==========================================
    public async Task<PagedResult<InventaireFour>> SearchFoursAsync(
        InventaireFourCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.InventairesFours.AsNoTracking().AsQueryable();

        if (criteria.Mois.HasValue && criteria.Annee.HasValue)
        {
            query = query.Where(x => x.Mois == criteria.Mois.Value && x.Annee == criteria.Annee.Value);
        }
        else if (criteria.Annee.HasValue)
        {
            query = query.Where(x => x.Annee == criteria.Annee.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.CodeTypeMatiere))
        {
            var code = criteria.CodeTypeMatiere.Trim().ToUpperInvariant();
            query = query.Where(x => x.CodeTypeMatiere == code);
        }

        if (criteria.Statut.HasValue)
        {
            query = query.Where(x => x.Statut == criteria.Statut.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.CodeFour))
        {
            var four = criteria.CodeFour.Trim().ToUpperInvariant();
            query = query.Where(x => x.CodeFour == four);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.DateInventaire)
            .ThenBy(x => x.CodeFour)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<InventaireFour>(items, total);
    }

    public Task<InventaireFour?> GetFourByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.InventairesFours.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddFourAsync(InventaireFour entity, CancellationToken cancellationToken)
        => await dbContext.InventairesFours.AddAsync(entity, cancellationToken);

    public Task RemoveFourAsync(InventaireFour entity, CancellationToken cancellationToken)
    {
        dbContext.InventairesFours.Remove(entity);
        return Task.CompletedTask;
    }

    // ==========================================
    // SYNTHÈSE MENSUELLE SQL AGGREGATIONS
    // ==========================================
    public async Task<IReadOnlyCollection<InventaireSyntheseRow>> GetSyntheseAsync(
        int mois,
        int annee,
        string? codeTypeMatiere,
        CancellationToken cancellationToken)
    {
        var dateDebut = new DateOnly(annee, mois, 1);
        var dateFin = dateDebut.AddMonths(1);
        var filterCode = string.IsNullOrWhiteSpace(codeTypeMatiere) ? null : codeTypeMatiere.Trim().ToUpperInvariant();

        // Partie 1 : Conteneurs Valides
        var p1Query = dbContext.InventairesConteneursOf
            .AsNoTracking()
            .Where(x => x.DateInventaire >= dateDebut && x.DateInventaire < dateFin && x.Statut == StatutInventaire.Valide);

        if (filterCode is not null)
        {
            p1Query = p1Query.Where(x => x.CodeTypeMatiere == filterCode);
        }

        var p1Rows = await p1Query
            .Select(x => new { x.CodeTypeMatiere, x.PoidsReelKg })
            .ToListAsync(cancellationToken);
        var p1List = p1Rows
            .GroupBy(x => x.CodeTypeMatiere)
            .Select(g => new { CodeTypeMatiere = g.Key, TotalKg = g.Sum(x => x.PoidsReelKg) })
            .ToList();

        // Partie 2 : Catégories Valides
        var p2Query = dbContext.InventairesCategories
            .AsNoTracking()
            .Where(x => x.DateInventaire >= dateDebut && x.DateInventaire < dateFin && x.Statut == StatutInventaire.Valide);

        if (filterCode is not null)
        {
            p2Query = p2Query.Where(x => x.CodeTypeMatiere == filterCode);
        }

        var p2Rows = await p2Query
            .Select(x => new { x.CodeTypeMatiere, x.PoidsReelKg })
            .ToListAsync(cancellationToken);
        var p2List = p2Rows
            .GroupBy(x => x.CodeTypeMatiere)
            .Select(g => new { CodeTypeMatiere = g.Key, TotalKg = g.Sum(x => x.PoidsReelKg) })
            .ToList();

        // Partie 3 : Fours Valides
        var p3Query = dbContext.InventairesFours
            .AsNoTracking()
            .Where(x => x.Mois == mois && x.Annee == annee && x.Statut == StatutInventaire.Valide);

        if (filterCode is not null)
        {
            p3Query = p3Query.Where(x => x.CodeTypeMatiere == filterCode);
        }

        var p3Rows = await p3Query
            .Select(x => new { x.CodeTypeMatiere, x.QuantiteKg })
            .ToListAsync(cancellationToken);
        var p3List = p3Rows
            .GroupBy(x => x.CodeTypeMatiere)
            .Select(g => new { CodeTypeMatiere = g.Key, TotalKg = g.Sum(x => x.QuantiteKg) })
            .ToList();

        var allCodes = p1List.Select(x => x.CodeTypeMatiere)
            .Union(p2List.Select(x => x.CodeTypeMatiere))
            .Union(p3List.Select(x => x.CodeTypeMatiere))
            .OrderBy(c => c)
            .ToList();

        var p1Dict = p1List.ToDictionary(x => x.CodeTypeMatiere, x => x.TotalKg);
        var p2Dict = p2List.ToDictionary(x => x.CodeTypeMatiere, x => x.TotalKg);
        var p3Dict = p3List.ToDictionary(x => x.CodeTypeMatiere, x => x.TotalKg);

        var rows = new List<InventaireSyntheseRow>();
        foreach (var code in allCodes)
        {
            var p1Kg = p1Dict.TryGetValue(code, out var v1) ? v1 : 0m;
            var p2Kg = p2Dict.TryGetValue(code, out var v2) ? v2 : 0m;
            var p3Kg = p3Dict.TryGetValue(code, out var v3) ? v3 : 0m;
            rows.Add(new InventaireSyntheseRow(code, p1Kg, p2Kg, p3Kg));
        }

        return rows;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    // ==========================================
    // ANCIEN MODÈLE / RÉTROCOMPATIBILITÉ
    // ==========================================
    public Task<InventaireFonderie?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.InventairesFonderie
            .Include(entity => entity.TypeMatiere)
            .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<PagedResult<InventaireFonderie>> GetPagedAsync(
        InventairesFonderieFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.InventairesFonderie
            .AsNoTracking()
            .Include(entity => entity.TypeMatiere)
            .AsQueryable();

        if (filter.Periode is not null)
        {
            query = query.Where(entity => entity.Periode == filter.Periode);
        }

        if (filter.TypeMatiereId is not null)
        {
            query = query.Where(entity => entity.TypeMatiereId == filter.TypeMatiereId);
        }

        if (filter.Statut is not null)
        {
            query = query.Where(entity => entity.Statut == filter.Statut);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entity => entity.Periode)
            .ThenBy(entity => entity.TypeMatiere.Code)
            .ThenByDescending(entity => entity.CreeLeUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<InventaireFonderie>(items, total);
    }

    public Task<bool> ExistsActiveAsync(
        DateOnly periode,
        Guid typeMatiereId,
        Guid? excludeId,
        CancellationToken cancellationToken)
        => dbContext.InventairesFonderie.AsNoTracking().AnyAsync(entity =>
            entity.Periode == periode &&
            entity.TypeMatiereId == typeMatiereId &&
            entity.Statut != StatutInventaireFonderie.Annule &&
            (!excludeId.HasValue || entity.Id != excludeId.Value),
            cancellationToken);

    public async Task AddAsync(InventaireFonderie inventaire, CancellationToken cancellationToken)
        => await dbContext.InventairesFonderie.AddAsync(inventaire, cancellationToken);
}
