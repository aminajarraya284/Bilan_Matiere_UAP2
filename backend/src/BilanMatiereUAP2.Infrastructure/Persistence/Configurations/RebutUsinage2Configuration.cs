using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class RebutUsinage2Configuration : IEntityTypeConfiguration<RebutUsinage2>
{
    public void Configure(EntityTypeBuilder<RebutUsinage2> builder)
    {
        builder.ToTable("RebutsUsinage2");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.NumeroOF).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.CodeArticle).HasMaxLength(80).IsRequired();
        // Quantite en PIECES : la precision decimale autorise les demi-pieces eventuelles
        // sans jamais convertir en kg.
        builder.Property(entity => entity.QuantiteRebutPieces).HasPrecision(18, 4);
        builder.Property(entity => entity.Motif).HasMaxLength(200);
        builder.Property(entity => entity.Observation).HasMaxLength(500);
        builder.Property(entity => entity.CreeParNom).HasMaxLength(120);
        builder.Property(entity => entity.ValideParNom).HasMaxLength(120);
        builder.Property(entity => entity.Statut)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(StatutRebutUsinage2.Brouillon);

        // Index principal du Bilan Usinage 2 : periode + cle metier.
        builder.HasIndex(entity => new { entity.Annee, entity.Mois, entity.NumeroOF, entity.CodeArticle });
        builder.HasIndex(entity => entity.NumeroOF);
        builder.HasQueryFilter(entity => !entity.EstSupprime);
    }
}
