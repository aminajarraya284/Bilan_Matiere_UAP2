using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class InventaireFonderieConfiguration : IEntityTypeConfiguration<InventaireFonderie>
{
    public void Configure(EntityTypeBuilder<InventaireFonderie> builder)
    {
        builder.ToTable("InventairesFonderie");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Periode).IsRequired();
        builder.Property(entity => entity.TypeMatiereId).IsRequired();
        builder.Property(entity => entity.StockDebutKg).HasPrecision(18, 3).IsRequired(false);
        builder.Property(entity => entity.StockFinKg).HasPrecision(18, 3).IsRequired();
        builder.Property(entity => entity.Observation).HasMaxLength(500);
        builder.Property(entity => entity.Statut).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.CreeParNom).HasMaxLength(150);
        builder.Property(entity => entity.CreeLeUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(entity => entity.ModifieParNom).HasMaxLength(150);
        builder.Property(entity => entity.ModifieLeUtc).HasColumnType("datetime2");
        builder.Property(entity => entity.ValideParNom).HasMaxLength(150);
        builder.Property(entity => entity.ValideLeUtc).HasColumnType("datetime2");
        builder.Property(entity => entity.AnnuleParNom).HasMaxLength(150);
        builder.Property(entity => entity.AnnuleLeUtc).HasColumnType("datetime2");

        builder.HasOne(entity => entity.TypeMatiere)
            .WithMany()
            .HasForeignKey(entity => entity.TypeMatiereId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.Periode, entity.TypeMatiereId })
            .HasDatabaseName("UX_InventairesFonderie_Periode_TypeMatiereId")
            .IsUnique();
        builder.HasIndex(entity => entity.Statut);
    }
}
