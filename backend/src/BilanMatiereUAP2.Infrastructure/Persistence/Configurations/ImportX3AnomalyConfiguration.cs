using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class ImportX3AnomalyConfiguration : IEntityTypeConfiguration<ImportX3Anomaly>
{
    public void Configure(EntityTypeBuilder<ImportX3Anomaly> builder)
    {
        builder.ToTable("ImportX3Anomalies");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TypeFichier)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TypeFichierImportX3>(v))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Statut)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<StatutRapprochementX3>(v))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Message).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.NumeroOF).HasMaxLength(50);
        builder.Property(e => e.CodeTypeMatiere).HasMaxLength(80);
        builder.Property(e => e.NomFeuilleSource).HasMaxLength(128);
        builder.Property(e => e.DateCreationUtc).HasColumnType("datetime2").IsRequired();

        builder.HasIndex(e => e.ImportBatchId);
    }
}
