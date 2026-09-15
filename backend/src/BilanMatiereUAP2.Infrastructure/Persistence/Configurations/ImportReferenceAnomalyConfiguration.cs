using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

internal sealed class ImportReferenceAnomalyConfiguration : IEntityTypeConfiguration<ImportReferenceAnomaly>
{
    public void Configure(EntityTypeBuilder<ImportReferenceAnomaly> builder)
    {
        builder.ToTable("ImportReferenceAnomalies");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TypeImport).IsRequired();
        builder.Property(e => e.NumeroLigneSource).IsRequired();
        builder.Property(e => e.NomFeuilleSource).HasMaxLength(128);
        builder.Property(e => e.Message).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.DateCreationUtc).HasColumnType("datetime2").IsRequired();
        builder.HasOne<ImportBatch>().WithMany().HasForeignKey(e => e.ImportBatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ImportBatchId);
    }
}
