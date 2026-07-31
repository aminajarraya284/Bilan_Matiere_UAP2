using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class InventaireComptageRepository(BilanMatiereUAP2DbContext context) : IInventaireComptageRepository
{
    // Lectures de liste : AsNoTracking + projection du type matiere pour eviter un N+1
    // sur le code matiere affiche dans chaque ligne.
    public async Task<IReadOnlyList<InventaireConteneur>> GetConteneursAsync(DateOnly periode, CancellationToken cancellationToken = default)
        => await context.InventairesConteneurs
            .AsNoTracking()
            .Include(ligne => ligne.TypeMatiere)
            .Where(ligne => ligne.Periode == periode)
            .OrderBy(ligne => ligne.DateInventaire).ThenBy(ligne => ligne.NumeroConteneur)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventaireCategorieComptage>> GetCategoriesAsync(DateOnly periode, CancellationToken cancellationToken = default)
        => await context.InventairesCategories
            .AsNoTracking()
            .Include(ligne => ligne.TypeMatiere)
            .Where(ligne => ligne.Periode == periode)
            .OrderBy(ligne => ligne.Categorie)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventaireFourComptage>> GetFoursAsync(DateOnly periode, CancellationToken cancellationToken = default)
        => await context.InventairesFours
            .AsNoTracking()
            .Include(ligne => ligne.TypeMatiere)
            .Where(ligne => ligne.Periode == periode)
            .OrderBy(ligne => ligne.DateInventaire).ThenBy(ligne => ligne.CodeFour)
            .ToListAsync(cancellationToken);

    // Lectures unitaires : entites SUIVIES, le cycle de vie les modifie.
    public Task<InventaireConteneur?> GetConteneurAsync(Guid id, CancellationToken cancellationToken = default)
        => context.InventairesConteneurs.Include(l => l.TypeMatiere).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<InventaireCategorieComptage?> GetCategorieAsync(Guid id, CancellationToken cancellationToken = default)
        => context.InventairesCategories.Include(l => l.TypeMatiere).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<InventaireFourComptage?> GetFourAsync(Guid id, CancellationToken cancellationToken = default)
        => context.InventairesFours.Include(l => l.TypeMatiere).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task AddAsync(InventaireConteneur ligne, CancellationToken cancellationToken = default)
        => await context.InventairesConteneurs.AddAsync(ligne, cancellationToken);

    public async Task AddAsync(InventaireCategorieComptage ligne, CancellationToken cancellationToken = default)
        => await context.InventairesCategories.AddAsync(ligne, cancellationToken);

    public async Task AddAsync(InventaireFourComptage ligne, CancellationToken cancellationToken = default)
        => await context.InventairesFours.AddAsync(ligne, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
