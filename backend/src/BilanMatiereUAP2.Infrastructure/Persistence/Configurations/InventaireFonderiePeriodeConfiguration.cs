using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class InventaireFonderiePeriodeConfiguration : IEntityTypeConfiguration<InventaireFonderiePeriode>
{
    public void Configure(EntityTypeBuilder<InventaireFonderiePeriode> builder)
    {
        builder.ToTable("InventairesFonderiePeriodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Statut)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ValidePar)
            .HasMaxLength(100);

        builder.Property(x => x.CloturePar)
            .HasMaxLength(100);

        builder.HasIndex(x => new
        {
            x.Annee,
            x.Mois
        }).IsUnique();
    }
}
