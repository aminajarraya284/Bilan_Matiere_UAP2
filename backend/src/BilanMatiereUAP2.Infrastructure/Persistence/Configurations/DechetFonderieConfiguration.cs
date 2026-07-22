using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class DechetFonderieConfiguration : IEntityTypeConfiguration<DechetFonderie>
{
    public void Configure(EntityTypeBuilder<DechetFonderie> builder)
    {
        builder.ToTable("DechetsFonderie");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.DateDeclaration).IsRequired();
        builder.Property(entity => entity.SemaineIso).IsRequired();
        builder.Property(entity => entity.TypeMatiereId).IsRequired();
        builder.Property(entity => entity.TypeDechet)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(entity => entity.PoidsKg)
            .HasPrecision(18, 3)
            .IsRequired();
        builder.Property(entity => entity.Observation)
            .HasMaxLength(500);
        builder.Property(entity => entity.Statut)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(entity => entity.CreeParNom)
            .HasMaxLength(150);
        builder.Property(entity => entity.DateCreationUtc)
            .HasColumnType("datetime2")
            .IsRequired();
        builder.Property(entity => entity.DateModificationUtc)
            .HasColumnType("datetime2");
        builder.Property(entity => entity.EstSupprime)
            .IsRequired();
        builder.Property(entity => entity.DateSuppressionUtc)
            .HasColumnType("datetime2");

        builder.HasOne(entity => entity.TypeMatiere)
            .WithMany()
            .HasForeignKey(entity => entity.TypeMatiereId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.DateDeclaration);
        builder.HasIndex(entity => entity.TypeMatiereId);
        builder.HasIndex(entity => entity.TypeDechet);
        builder.HasIndex(entity => entity.EstSupprime);

        builder.HasQueryFilter(entity => !entity.EstSupprime);
    }
}
