using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class ImportSourceFileConfiguration : IEntityTypeConfiguration<ImportSourceFile>
{
    public void Configure(EntityTypeBuilder<ImportSourceFile> builder)
    {
        builder.ToTable("ImportSourceFiles");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.ProfilImport).HasMaxLength(100);
        builder.Property(entity => entity.NomFichierOriginal).HasMaxLength(260).IsRequired();
        builder.Property(entity => entity.Extension).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.MimeType).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.HashSha256).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.CheminStockage).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.UploadeParNom).HasMaxLength(150);

        builder.HasIndex(entity => entity.ImportBatchId);
        builder.HasIndex(entity => entity.HashSha256);
    }
}
