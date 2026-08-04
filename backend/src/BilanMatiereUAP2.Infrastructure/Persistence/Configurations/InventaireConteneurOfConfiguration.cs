using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class InventaireConteneurOfConfiguration : IEntityTypeConfiguration<InventaireConteneurOf>
{
    public void Configure(EntityTypeBuilder<InventaireConteneurOf> builder)
    {
        builder.ToTable("InventairesConteneursOf");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NumeroConteneur)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NumeroOF)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CodeTypeMatiere)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PoidsReelKg)
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
            x.DateInventaire,
            x.NumeroConteneur
        });

        builder.HasIndex(x => new
        {
            x.NumeroOF,
            x.CodeTypeMatiere
        });

        builder.HasIndex(x => new
        {
            x.DateInventaire,
            x.CodeTypeMatiere,
            x.Statut
        });
    }
}
