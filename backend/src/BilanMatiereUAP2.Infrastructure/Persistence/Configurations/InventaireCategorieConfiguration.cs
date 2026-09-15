using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class InventaireCategorieConfiguration : IEntityTypeConfiguration<InventaireCategorie>
{
    public void Configure(EntityTypeBuilder<InventaireCategorie> builder)
    {
        builder.ToTable("InventairesCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Categorie)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CodeTypeMatiere)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PoidsReelKg)
            .HasPrecision(18, 3);

        builder.Property(x => x.Commentaire)
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
            x.Categorie
        });

        builder.HasIndex(x => new
        {
            x.DateInventaire,
            x.CodeTypeMatiere,
            x.Statut
        });
    }
}
