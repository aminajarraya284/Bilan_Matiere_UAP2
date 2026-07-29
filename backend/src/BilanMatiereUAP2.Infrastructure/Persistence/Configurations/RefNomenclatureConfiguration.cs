using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

internal sealed class RefNomenclatureConfiguration : IEntityTypeConfiguration<RefNomenclature>
{
    public void Configure(EntityTypeBuilder<RefNomenclature> builder)
    {
        builder.ToTable("RefNomenclatures");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CodeArticleParent).HasMaxLength(100).IsRequired();
        builder.Property(e => e.AlternativeNomenclature).HasMaxLength(50);
        builder.Property(e => e.CodeParentNiveau).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CodeComposant).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Niveau).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Sequence).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DesignationComposant).HasMaxLength(250);
        builder.Property(e => e.QuantiteLien).HasColumnType("decimal(18,6)");
        builder.Property(e => e.QuantiteLienCumulee).HasColumnType("decimal(18,6)");
        builder.Property(e => e.UniteQuantite).HasMaxLength(100);
        builder.Property(e => e.CategorieComposant).HasMaxLength(100);
        builder.Property(e => e.TypeLien).HasMaxLength(100);
        builder.Property(e => e.PourcentageRebut).HasColumnType("decimal(18,6)");
        builder.Property(e => e.CleMetier).HasMaxLength(500).IsRequired();
        builder.Property(e => e.SourceSystem).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ImportBatchId).IsRequired();
        builder.Property(e => e.ImportedAt).IsRequired();
        builder.Property(e => e.SourceUpdatedAt);
        builder.Property(e => e.IsActive).IsRequired();

        builder.HasIndex(e => e.CleMetier).IsUnique();
        builder.HasIndex(e => new { e.CodeArticleParent });
        builder.HasIndex(e => new { e.CodeComposant });
        builder.HasIndex(e => new { e.IsActive });

        builder.HasOne<ImportBatch>().WithMany().HasForeignKey(e => e.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
