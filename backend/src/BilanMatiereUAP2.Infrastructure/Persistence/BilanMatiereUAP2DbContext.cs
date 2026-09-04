using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Infrastructure.Persistence;

public sealed class BilanMatiereUAP2DbContext(
    DbContextOptions<BilanMatiereUAP2DbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<TypeMatiere> TypesMatiere => Set<TypeMatiere>();
    public DbSet<DechetFonderie> DechetsFonderie => Set<DechetFonderie>();
    public DbSet<ControlePoidsFonderie> ControlesPoidsFonderie => Set<ControlePoidsFonderie>();
    public DbSet<InventaireFonderie> InventairesFonderie => Set<InventaireFonderie>();
    public DbSet<InventaireConteneur> InventairesConteneursComptage => Set<InventaireConteneur>();
    public DbSet<InventaireCategorieComptage> InventairesCategoriesComptage => Set<InventaireCategorieComptage>();
    public DbSet<InventaireFourComptage> InventairesFoursComptage => Set<InventaireFourComptage>();

    public DbSet<InventaireConteneurOf> InventairesConteneursOf => Set<InventaireConteneurOf>();
    public DbSet<InventaireCategorie> InventairesCategories => Set<InventaireCategorie>();
    public DbSet<InventaireFour> InventairesFours => Set<InventaireFour>();
    public DbSet<InventaireFonderiePeriode> InventairesFonderiePeriodes => Set<InventaireFonderiePeriode>();
    public DbSet<ImportX3Batch> ImportX3Batches => Set<ImportX3Batch>();
    public DbSet<ImportX3File> ImportX3Files => Set<ImportX3File>();
    public DbSet<ImportX3Anomaly> ImportX3Anomalies => Set<ImportX3Anomaly>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportReferenceAnomaly> ImportReferenceAnomalies => Set<ImportReferenceAnomaly>();
    public DbSet<RefGammeFabrication> RefGammesFabrication => Set<RefGammeFabrication>();
    public DbSet<RefNomenclature> RefNomenclatures => Set<RefNomenclature>();
    public DbSet<X3RapportMachine> X3RapportsMachine => Set<X3RapportMachine>();
    public DbSet<X3OrdreFabrication> OrdresFabrication => Set<X3OrdreFabrication>();
    public DbSet<X3OperationOF> OperationsOF => Set<X3OperationOF>();
    public DbSet<ConsommationComposant> ConsommationsComposants => Set<ConsommationComposant>();
    public DbSet<ImportSourceFile> ImportSourceFiles => Set<ImportSourceFile>();
    public DbSet<ReceptionUsinage2> ReceptionsUsinage2 => Set<ReceptionUsinage2>();
    public DbSet<RebutUsinage2> RebutsUsinage2 => Set<RebutUsinage2>();
    public DbSet<EntreeOfComposant> EntreesOfComposants => Set<EntreeOfComposant>();
    public DbSet<X3MouvementStock> X3MouvementsStock => Set<X3MouvementStock>();
    public DbSet<ParametreApplication> ParametresApplication => Set<ParametreApplication>();
    public DbSet<AlerteMetier> AlertesMetier => Set<AlerteMetier>();
    public DbSet<TraceAudit> AuditTraces => Set<TraceAudit>();
    public DbSet<UtilisateurApplication> UtilisateursApplication => Set<UtilisateurApplication>();
    public DbSet<PeriodeBilanArchivee> PeriodesBilansArchivees => Set<PeriodeBilanArchivee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BilanMatiereUAP2DbContext).Assembly);
        modelBuilder.Ignore<Composant>();
        base.OnModelCreating(modelBuilder);
    }
}
