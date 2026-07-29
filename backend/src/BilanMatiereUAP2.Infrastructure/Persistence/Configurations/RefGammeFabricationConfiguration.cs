using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

internal sealed class RefGammeFabricationConfiguration : IEntityTypeConfiguration<RefGammeFabrication>
{
    public void Configure(EntityTypeBuilder<RefGammeFabrication> builder)
    {
        builder.ToTable("RefGammesFabrication");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CodeArticle).HasMaxLength(100).IsRequired();
        builder.Property(e => e.DesignationArticle).HasMaxLength(250);
        builder.Property(e => e.DateFinValidite).HasColumnType("date");
        builder.Property(e => e.EtapeSuivi).HasMaxLength(50);
        builder.Property(e => e.AlternativeGamme).HasMaxLength(50);
        builder.Property(e => e.NumeroOperation).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CodeAtelier).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CodePoste).HasMaxLength(50).IsRequired();
        builder.Property(e => e.NombrePostes).HasColumnType("decimal(18,4)");
        builder.Property(e => e.GroupePosteCharge).HasMaxLength(100);
        builder.Property(e => e.DesignationOperation).HasMaxLength(250);
        builder.Property(e => e.UniteOperation).HasMaxLength(20);
        builder.Property(e => e.CoefficientOperationStock).HasColumnType("decimal(18,6)");
        builder.Property(e => e.TempsCycle).HasColumnType("decimal(18,6)");
        builder.Property(e => e.Trs).HasColumnType("decimal(18,6)");
        builder.Property(e => e.TempsAttente).HasColumnType("decimal(18,6)");
        builder.Property(e => e.Jalon).HasMaxLength(50);
        builder.Property(e => e.Quantite).HasColumnType("decimal(18,6)");
        builder.Property(e => e.Cadence).HasColumnType("decimal(18,6)");
        builder.Property(e => e.DateModificationX3).HasColumnType("datetime2");
        builder.Property(e => e.TempsReglage).HasColumnType("decimal(18,6)");
        builder.Property(e => e.CoefficientCcpt).HasColumnType("decimal(18,6)");
        builder.Property(e => e.CleMetier).HasMaxLength(500).IsRequired();
        builder.Property(e => e.SourceSystem).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ImportBatchId).IsRequired();
        builder.Property(e => e.ImportedAt).IsRequired();
        builder.Property(e => e.SourceUpdatedAt);
        builder.Property(e => e.IsActive).IsRequired();

        builder.HasIndex(e => e.CleMetier).IsUnique();
        builder.HasIndex(e => new { e.CodeArticle });
        builder.HasIndex(e => new { e.CodeAtelier });
        builder.HasIndex(e => new { e.NumeroOperation });
        builder.HasIndex(e => new { e.IsActive });

        builder.HasOne<ImportBatch>().WithMany().HasForeignKey(e => e.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
