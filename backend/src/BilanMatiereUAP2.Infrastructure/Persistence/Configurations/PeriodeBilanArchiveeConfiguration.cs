using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class PeriodeBilanArchiveeConfiguration : IEntityTypeConfiguration<PeriodeBilanArchivee>
{
    public void Configure(EntityTypeBuilder<PeriodeBilanArchivee> builder)
    {
        builder.ToTable("PeriodesBilansArchivees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Periode).HasMaxLength(7).IsRequired();
        builder.HasIndex(x => x.Periode).IsUnique();
        builder.Property(x => x.EnregistrePar).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StatutGlobal).HasMaxLength(50).IsRequired();
        builder.Property(x => x.VersionCalcul).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RapportTemplateVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SnapshotJson).IsRequired();
    }
}
