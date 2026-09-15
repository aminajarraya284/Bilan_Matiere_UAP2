using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class X3MouvementStockConfiguration : IEntityTypeConfiguration<X3MouvementStock>
{
    public void Configure(EntityTypeBuilder<X3MouvementStock> builder)
    {
        builder.ToTable("X3MouvementsStock");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.NomFeuilleSource).HasMaxLength(128);
        builder.Property(entity => entity.Code).HasMaxLength(80);
        builder.Property(entity => entity.Designation).HasMaxLength(250);
        builder.Property(entity => entity.HeureCreationX3).HasMaxLength(30);
        builder.Property(entity => entity.Utilisateur).HasMaxLength(100);
        builder.Property(entity => entity.QuantiteX3Signee).HasColumnType("decimal(18,4)");
        builder.Property(entity => entity.Unite).HasMaxLength(20);
        builder.Property(entity => entity.Transaction).HasMaxLength(80);
        builder.Property(entity => entity.TypePiece).HasMaxLength(80);
        builder.Property(entity => entity.Lot).HasMaxLength(80);
        builder.Property(entity => entity.PieceOrigine).HasMaxLength(100);
        builder.Property(entity => entity.NumeroOFNormalise).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.DesignationMouvement).HasMaxLength(250);
        builder.Property(entity => entity.Emplacement).HasMaxLength(80);
        builder.Property(entity => entity.StatutX3).HasMaxLength(80);
        builder.Property(entity => entity.Nature).HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.StatutRapprochement).HasConversion<string>().HasMaxLength(40);

        // ImportBatchId correspond au lot ImportBatch du flux MVT unitaire. Comme les archives
        // d'import, le lien reste scalaire pour ne pas coupler les deux familles historiques de lots.
        builder.HasIndex(entity => entity.ImportBatchId);
        builder.HasIndex(entity => entity.DateImputation);
        builder.HasIndex(entity => entity.NumeroOFNormalise);
        builder.HasIndex(entity => entity.Code);
        builder.HasIndex(entity => entity.Nature);
    }
}
