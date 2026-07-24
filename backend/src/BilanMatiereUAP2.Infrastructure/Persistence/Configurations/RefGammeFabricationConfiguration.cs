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
        builder.Property(e => e.AlternativeGamme).HasMaxLength(50);
        builder.Property(e => e.NumeroOperation).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CodePoste).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CleMetier).HasMaxLength(500).IsRequired();
        builder.Property(e => e.SourceSystem).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ImportBatchId).IsRequired();
        builder.Property(e => e.ImportedAt).IsRequired();
        builder.Property(e => e.SourceUpdatedAt);
        builder.Property(e => e.IsActive).IsRequired();

        builder.HasIndex(e => e.CleMetier).IsUnique();
        builder.HasIndex(e => new { e.CodeArticle });
        builder.HasIndex(e => new { e.NumeroOperation });
        builder.HasIndex(e => new { e.IsActive });

        builder.HasOne<ImportBatch>().WithMany().HasForeignKey(e => e.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
