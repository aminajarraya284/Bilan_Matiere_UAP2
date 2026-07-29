using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class ControlePoidsFonderieConfiguration : IEntityTypeConfiguration<ControlePoidsFonderie>
{
    public void Configure(EntityTypeBuilder<ControlePoidsFonderie> builder)
    {
        builder.ToTable("ControlesPoidsFonderie");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.NumeroOF).HasMaxLength(50);
        builder.Property(entity => entity.CodeArticle).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.NumeroLotOuSerie).HasMaxLength(80);
        builder.Property(entity => entity.PoidsPhysiqueUnitaireKg).HasPrecision(18, 6);
        builder.Property(entity => entity.Observation).HasMaxLength(500);
        builder.Property(entity => entity.CreeParNom).HasMaxLength(120);
        builder.Property(entity => entity.ValideParNom).HasMaxLength(120);
        builder.Property(entity => entity.Statut)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(StatutControlePoidsFonderie.Brouillon);

        builder.HasOne(entity => entity.TypeMatiere)
            .WithMany()
            .HasForeignKey(entity => entity.TypeMatiereId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CodeArticle, entity.TypeMatiereId, entity.DateControle });
        builder.HasIndex(entity => entity.NumeroOF);
        builder.HasQueryFilter(entity => !entity.EstSupprime);
    }
}
