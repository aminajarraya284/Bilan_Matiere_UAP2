using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class InventaireFourConfiguration : IEntityTypeConfiguration<InventaireFour>
{
    public void Configure(EntityTypeBuilder<InventaireFour> builder)
    {
        builder.ToTable("InventairesFours");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodeFour)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CodeTypeMatiere)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.QuantiteKg)
            .HasPrecision(18, 3);

        builder.Property(x => x.Observation)
            .HasMaxLength(500);

        builder.Property(x => x.Statut)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ValidePar)
            .HasMaxLength(100);

        builder.Property(x => x.AnnulePar)
            .HasMaxLength(100);

        builder.HasIndex(x => new
        {
            x.Annee,
            x.Mois,
            x.CodeFour
        });

        builder.HasIndex(x => new
        {
            x.DateInventaire,
            x.CodeTypeMatiere,
            x.Statut
        });
    }
}
