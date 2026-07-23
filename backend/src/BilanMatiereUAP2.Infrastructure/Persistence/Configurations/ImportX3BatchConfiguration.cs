using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class ImportX3BatchConfiguration : IEntityTypeConfiguration<ImportX3Batch>
{
    public void Configure(EntityTypeBuilder<ImportX3Batch> builder)
    {
        builder.ToTable("ImportX3Batches");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.HashCoupleSha256)
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(e => e.HashCoupleSha256);

        builder.Property(e => e.Statut)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<StatutRapprochementX3>(v))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.CreeParNom).HasMaxLength(150);
        builder.Property(e => e.DateCreationUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.DateFinUtc).HasColumnType("datetime2");

        builder.HasMany(e => e.Fichiers)
            .WithOne(e => e.ImportBatch)
            .HasForeignKey(e => e.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Anomalies)
            .WithOne(e => e.ImportBatch)
            .HasForeignKey(e => e.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.Mouvements);
        builder.Ignore(e => e.Consommations);
    }
}
