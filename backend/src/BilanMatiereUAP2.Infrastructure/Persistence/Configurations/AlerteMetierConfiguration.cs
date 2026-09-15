using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class AlerteMetierConfiguration : IEntityTypeConfiguration<AlerteMetier>
{
    public void Configure(EntityTypeBuilder<AlerteMetier> builder)
    {
        var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        var nullableUtcDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            value => value,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null);

        builder.ToTable("AlertesMetier");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Severite).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Statut).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Titre).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.Source).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Periode).HasMaxLength(7);
        builder.Property(entity => entity.NumeroOF).HasMaxLength(50);
        builder.Property(entity => entity.CodeArticle).HasMaxLength(100);
        builder.Property(entity => entity.TypeMatiere).HasMaxLength(50);
        builder.Property(entity => entity.CleDeduplication).HasMaxLength(600).IsRequired();
        builder.Property(entity => entity.AcquitteePar).HasMaxLength(150);
        builder.Property(entity => entity.ResoluePar).HasMaxLength(150);
        builder.Property(entity => entity.IgnoreePar).HasMaxLength(150);
        builder.Property(entity => entity.CommentaireTraitement).HasMaxLength(AlerteMetier.CommentaireMaxLength);
        builder.Property(entity => entity.DonneesAssocieesJson).HasMaxLength(AlerteMetier.DonneesAssocieesMaxLength);

        builder.Property(entity => entity.DetecteeLeUtc)
            .HasConversion(utcDateTimeConverter)
            .HasColumnType("datetime2")
            .IsRequired();
        builder.Property(entity => entity.DerniereDetectionLeUtc)
            .HasConversion(utcDateTimeConverter)
            .HasColumnType("datetime2")
            .IsRequired();
        builder.Property(entity => entity.AcquitteeLeUtc)
            .HasConversion(nullableUtcDateTimeConverter)
            .HasColumnType("datetime2");
        builder.Property(entity => entity.ResolueLeUtc)
            .HasConversion(nullableUtcDateTimeConverter)
            .HasColumnType("datetime2");
        builder.Property(entity => entity.IgnoreeLeUtc)
            .HasConversion(nullableUtcDateTimeConverter)
            .HasColumnType("datetime2");

        builder.HasIndex(entity => entity.CleDeduplication)
            .IsUnique()
            .HasFilter("[EstActive] = 1");
        builder.HasIndex(entity => new { entity.Periode, entity.Statut });
        builder.HasIndex(entity => new { entity.Source, entity.Severite });
        builder.HasIndex(entity => entity.NumeroOF);
        builder.HasIndex(entity => entity.CodeArticle);
        builder.HasIndex(entity => entity.DetecteeLeUtc);
    }
}
