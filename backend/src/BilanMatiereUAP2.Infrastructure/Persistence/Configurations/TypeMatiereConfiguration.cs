using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class TypeMatiereConfiguration : IEntityTypeConfiguration<TypeMatiere>
{
    public static readonly Guid M3000L0Id = Guid.Parse("30000000-0000-0000-0000-000000000001");
    public static readonly Guid M3010L0Id = Guid.Parse("30100000-0000-0000-0000-000000000001");
    public static readonly Guid M3060L0Id = Guid.Parse("30600000-0000-0000-0000-000000000001");
    private static readonly DateTime SeedDateUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<TypeMatiere> builder)
    {
        builder.ToTable("TypesMatiere");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code)
            .HasMaxLength(30)
            .IsRequired();
        builder.HasIndex(entity => entity.Code)
            .IsUnique();

        builder.Property(entity => entity.Libelle)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(entity => entity.EstActif)
            .IsRequired();
        builder.Property(entity => entity.DateCreationUtc)
            .HasColumnType("datetime2")
            .IsRequired();
        builder.Property(entity => entity.DateModificationUtc)
            .HasColumnType("datetime2");

        builder.HasData(
            new
            {
                Id = M3000L0Id,
                Code = "M3000L0",
                Libelle = "Matière M3000L0",
                EstActif = true,
                DateCreationUtc = SeedDateUtc,
                DateModificationUtc = (DateTime?)null
            },
            new
            {
                Id = M3010L0Id,
                Code = "M3010L0",
                Libelle = "Matière M3010L0",
                EstActif = true,
                DateCreationUtc = SeedDateUtc,
                DateModificationUtc = (DateTime?)null
            },
            new
            {
                Id = M3060L0Id,
                Code = "M3060L0",
                Libelle = "Matière M3060L0",
                EstActif = true,
                DateCreationUtc = SeedDateUtc,
                DateModificationUtc = (DateTime?)null
            });
    }
}
