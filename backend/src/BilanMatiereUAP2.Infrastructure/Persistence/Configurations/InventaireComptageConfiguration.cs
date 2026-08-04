using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

internal static class ComptageCommun
{
    public static void ConfigurerColonnesCommunes<T>(EntityTypeBuilder<T> builder)
        where T : LigneComptageInventaire
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Periode).IsRequired();
        builder.Property(entity => entity.TypeMatiereId).IsRequired();
        builder.Property(entity => entity.QuantiteKg).HasPrecision(18, 3).IsRequired();
        builder.Property(entity => entity.Observation).HasMaxLength(500);
        builder.Property(entity => entity.Statut).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.CreeParNom).HasMaxLength(150);
        builder.Property(entity => entity.CreeLeUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(entity => entity.ModifieParNom).HasMaxLength(150);
        builder.Property(entity => entity.ModifieLeUtc).HasColumnType("datetime2");
        builder.Property(entity => entity.ValideParNom).HasMaxLength(150);
        builder.Property(entity => entity.ValideLeUtc).HasColumnType("datetime2");

        builder.HasOne(entity => entity.TypeMatiere)
            .WithMany()
            .HasForeignKey(entity => entity.TypeMatiereId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.Periode, entity.TypeMatiereId });
        builder.HasIndex(entity => entity.Statut);
    }
}

public sealed class InventaireConteneurConfiguration : IEntityTypeConfiguration<InventaireConteneur>
{
    public void Configure(EntityTypeBuilder<InventaireConteneur> builder)
    {
        builder.ToTable("InventairesConteneursComptage");
        ComptageCommun.ConfigurerColonnesCommunes(builder);
        builder.Property(entity => entity.DateInventaire).IsRequired();
        builder.Property(entity => entity.NumeroConteneur).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.NumeroOF).HasMaxLength(50);
        builder.Property(entity => entity.CodeArticle).HasMaxLength(50);
        builder.Property(entity => entity.DesignationArticle).HasMaxLength(250);
        builder.HasIndex(entity => entity.NumeroConteneur);
    }
}

public sealed class InventaireCategorieComptageConfiguration : IEntityTypeConfiguration<InventaireCategorieComptage>
{
    public void Configure(EntityTypeBuilder<InventaireCategorieComptage> builder)
    {
        builder.ToTable("InventairesCategoriesComptage");
        ComptageCommun.ConfigurerColonnesCommunes(builder);
        builder.Property(entity => entity.Categorie).HasMaxLength(100).IsRequired();
    }
}

public sealed class InventaireFourComptageConfiguration : IEntityTypeConfiguration<InventaireFourComptage>
{
    public void Configure(EntityTypeBuilder<InventaireFourComptage> builder)
    {
        builder.ToTable("InventairesFoursComptage");
        ComptageCommun.ConfigurerColonnesCommunes(builder);
        builder.Property(entity => entity.DateInventaire).IsRequired();
        builder.Property(entity => entity.CodeFour).HasMaxLength(50).IsRequired();
    }
}
