using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class X3OrdreFabricationConfiguration : IEntityTypeConfiguration<X3OrdreFabrication>
{
    public void Configure(EntityTypeBuilder<X3OrdreFabrication> builder)
    {
        builder.ToTable("OrdresFabrication");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.NumeroOF).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.NumeroOF).IsUnique();

        builder.Property(e => e.ArticlePrincipal).HasMaxLength(80);
        builder.HasIndex(e => e.ArticlePrincipal);

        builder.Property(e => e.DesignationPrincipale).HasMaxLength(250);
        builder.Property(e => e.SituationOF).HasMaxLength(80);
        builder.Property(e => e.Atelier).HasMaxLength(30);
        builder.Property(e => e.StatutX3).HasMaxLength(80);

        builder.Property(e => e.DateCreationUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.DateModificationUtc).HasColumnType("datetime2");

        builder.HasOne(e => e.DernierImportBatch)
            .WithMany()
            .HasForeignKey(e => e.DernierImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Operations)
            .WithOne(e => e.OrdreFabrication)
            .HasForeignKey(e => e.OrdreFabricationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Consommations)
            .WithOne(e => e.OrdreFabrication)
            .HasForeignKey(e => e.OrdreFabricationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.Mouvements);
    }
}
