using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence;

public sealed class BilanMatiereUAP2DbContext(
    DbContextOptions<BilanMatiereUAP2DbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<TypeMatiere> TypesMatiere => Set<TypeMatiere>();
    public DbSet<DechetFonderie> DechetsFonderie => Set<DechetFonderie>();
    public DbSet<ImportX3Batch> ImportX3Batches => Set<ImportX3Batch>();
    public DbSet<ImportX3File> ImportX3Files => Set<ImportX3File>();
    public DbSet<ImportX3Anomaly> ImportX3Anomalies => Set<ImportX3Anomaly>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<RefGammeFabrication> RefGammesFabrication => Set<RefGammeFabrication>();
    public DbSet<RefNomenclature> RefNomenclatures => Set<RefNomenclature>();
    public DbSet<X3OrdreFabrication> OrdresFabrication => Set<X3OrdreFabrication>();
    public DbSet<X3OperationOF> OperationsOF => Set<X3OperationOF>();
    public DbSet<ConsommationComposant> ConsommationsComposants => Set<ConsommationComposant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BilanMatiereUAP2DbContext).Assembly);
        modelBuilder.Ignore<X3MouvementStock>();
        modelBuilder.Ignore<Composant>();
        base.OnModelCreating(modelBuilder);
    }
}
