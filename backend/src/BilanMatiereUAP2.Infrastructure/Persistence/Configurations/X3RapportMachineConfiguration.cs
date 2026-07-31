using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

internal sealed class X3RapportMachineConfiguration : IEntityTypeConfiguration<X3RapportMachine>
{
    public void Configure(EntityTypeBuilder<X3RapportMachine> builder)
    {
        builder.ToTable("X3RapportsMachine");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DateProduction).HasColumnType("date").IsRequired();
        builder.Property(e => e.CodePoste).HasMaxLength(50).IsRequired();
        builder.Property(e => e.NumeroOperation).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CodeArticle).HasMaxLength(100).IsRequired();
        builder.Property(e => e.DesignationArticle).HasMaxLength(250);
        builder.Property(e => e.NumeroOF).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Equipe).HasMaxLength(50);
        builder.Property(e => e.OrigineProcessus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.QuantiteTotale).HasColumnName("QuantiteTotale").HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(e => e.QuantiteRebut).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(e => e.AlternativeGamme).HasMaxLength(50);
        builder.Property(e => e.CleMetier).HasMaxLength(500).IsRequired();
        builder.Property(e => e.SourceSystem).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ImportedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.HasIndex(e => e.CleMetier).IsUnique();
        builder.HasIndex(e => new { e.NumeroOF, e.CodeArticle });
        builder.HasIndex(e => e.OrigineProcessus);
        builder.HasIndex(e => e.IsActive);
        builder.HasOne<ImportBatch>().WithMany().HasForeignKey(e => e.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
