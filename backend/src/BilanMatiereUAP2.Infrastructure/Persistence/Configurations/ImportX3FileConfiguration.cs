using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class ImportX3FileConfiguration : IEntityTypeConfiguration<ImportX3File>
{
    public void Configure(EntityTypeBuilder<ImportX3File> builder)
    {
        builder.ToTable("ImportX3Files");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TypeFichier)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TypeFichierImportX3>(v))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.NomFichier).HasMaxLength(260).IsRequired();
        builder.Property(e => e.Extension).HasMaxLength(10).IsRequired();
        builder.Property(e => e.HashSha256).HasMaxLength(64).IsRequired();
        builder.Property(e => e.DateCreationUtc).HasColumnType("datetime2").IsRequired();
    }
}
