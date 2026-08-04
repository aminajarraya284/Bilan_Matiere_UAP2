using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Repositories;

public sealed class InventaireComptageRepository(BilanMatiereUAP2DbContext context) : IInventaireComptageRepository
{
    public async Task<IReadOnlyList<InventaireConteneur>> GetConteneursAsync(DateOnly periode, CancellationToken cancellationToken = default)
        => await context.InventairesConteneursComptage
            .AsNoTracking()
            .Include(ligne => ligne.TypeMatiere)
            .Where(ligne => ligne.Periode == periode)
            .OrderBy(ligne => ligne.DateInventaire).ThenBy(ligne => ligne.NumeroConteneur)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventaireCategorieComptage>> GetCategoriesAsync(DateOnly periode, CancellationToken cancellationToken = default)
        => await context.InventairesCategoriesComptage
            .AsNoTracking()
            .Include(ligne => ligne.TypeMatiere)
            .Where(ligne => ligne.Periode == periode)
            .OrderBy(ligne => ligne.Categorie)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventaireFourComptage>> GetFoursAsync(DateOnly periode, CancellationToken cancellationToken = default)
        => await context.InventairesFoursComptage
            .AsNoTracking()
            .Include(ligne => ligne.TypeMatiere)
            .Where(ligne => ligne.Periode == periode)
            .OrderBy(ligne => ligne.DateInventaire).ThenBy(ligne => ligne.CodeFour)
            .ToListAsync(cancellationToken);

    public Task<InventaireConteneur?> GetConteneurAsync(Guid id, CancellationToken cancellationToken = default)
        => context.InventairesConteneursComptage.Include(l => l.TypeMatiere).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<InventaireCategorieComptage?> GetCategorieAsync(Guid id, CancellationToken cancellationToken = default)
        => context.InventairesCategoriesComptage.Include(l => l.TypeMatiere).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<InventaireFourComptage?> GetFourAsync(Guid id, CancellationToken cancellationToken = default)
        => context.InventairesFoursComptage.Include(l => l.TypeMatiere).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task AddAsync(InventaireConteneur ligne, CancellationToken cancellationToken = default)
        => await context.InventairesConteneursComptage.AddAsync(ligne, cancellationToken);

    public async Task AddAsync(InventaireCategorieComptage ligne, CancellationToken cancellationToken = default)
        => await context.InventairesCategoriesComptage.AddAsync(ligne, cancellationToken);

    public async Task AddAsync(InventaireFourComptage ligne, CancellationToken cancellationToken = default)
        => await context.InventairesFoursComptage.AddAsync(ligne, cancellationToken);

    public Task<InventaireFonderie?> GetProjectionAsync(
        DateOnly periode,
        Guid typeMatiereId,
        CancellationToken cancellationToken = default)
        => context.InventairesFonderie
            .SingleOrDefaultAsync(
                projection => projection.Periode == periode && projection.TypeMatiereId == typeMatiereId,
                cancellationToken);

    public async Task AddProjectionAsync(
        InventaireFonderie projection,
        CancellationToken cancellationToken = default)
        => await context.InventairesFonderie.AddAsync(projection, cancellationToken);

    public async Task<IInventaireComptageTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
        => new InventaireComptageTransaction(
            await context.Database.BeginTransactionAsync(cancellationToken));

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    private sealed class InventaireComptageTransaction(IDbContextTransaction transaction)
        : IInventaireComptageTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
            => transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
