using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

internal sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatches");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.HashFichierSha256).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ProfilImport).HasMaxLength(100);
        builder.Property(e => e.DateCreationUtc).IsRequired();
        builder.Property(e => e.DateFinUtc);
        builder.Property(e => e.Statut).IsRequired();
        builder.Property(e => e.NombreLignesLues).IsRequired();
        builder.Property(e => e.NombreLignesValides).IsRequired();
        builder.Property(e => e.NombreLignesImportees).IsRequired();
        builder.Property(e => e.NombreAnomalies).IsRequired();
        builder.Property(e => e.ReadCount).IsRequired();
        builder.Property(e => e.ValidCount).IsRequired();
        builder.Property(e => e.InsertedCount).IsRequired();
        builder.Property(e => e.UpdatedCount).IsRequired();
        builder.Property(e => e.UnchangedCount).IsRequired();
        builder.Property(e => e.RejectedCount).IsRequired();
        builder.Property(e => e.ArchiveErrorMessage).HasMaxLength(2000);

        builder.HasIndex(e => e.HashFichierSha256);
        builder.HasIndex(e => e.TypeImport);
        builder.HasIndex(e => e.ProfilImport);
    }
}
