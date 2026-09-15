using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class X3OperationOFConfiguration : IEntityTypeConfiguration<X3OperationOF>
{
    public void Configure(EntityTypeBuilder<X3OperationOF> builder)
    {
        builder.ToTable("OperationsOF");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.NumeroOperation).HasMaxLength(30).IsRequired();
        builder.Property(e => e.CodePoste).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TypePoste).HasMaxLength(30);
        builder.Property(e => e.NumeroPoste).HasMaxLength(30);
        builder.Property(e => e.EffectifPoste).HasMaxLength(30);
        builder.Property(e => e.Cad).HasMaxLength(30);
        builder.Property(e => e.EffectifOperation).HasMaxLength(30);
        builder.Property(e => e.QuantitePrevue).HasColumnType("decimal(18,4)");
        builder.Property(e => e.QuantiteRestante).HasColumnType("decimal(18,4)");

        builder.Property(e => e.DateCreationUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.DateModificationUtc).HasColumnType("datetime2");

        builder.HasOne(e => e.ImportBatch)
            .WithMany()
            .HasForeignKey(e => e.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.OrdreFabricationId, e.NumeroOperation, e.CodePoste }).IsUnique();
    }
}
