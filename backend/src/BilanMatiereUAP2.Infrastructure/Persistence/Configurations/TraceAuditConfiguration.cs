using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class TraceAuditConfiguration : IEntityTypeConfiguration<TraceAudit>
{
    public void Configure(EntityTypeBuilder<TraceAudit> builder)
    {
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        builder.ToTable("AuditTraces");
        builder.HasKey(trace => trace.Id);
        builder.Property(trace => trace.DateUtc).HasConversion(utcConverter).HasColumnType("datetime2").IsRequired();
        builder.Property(trace => trace.Action).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(trace => trace.Domaine).HasConversion<string>().HasMaxLength(60).IsRequired();
        builder.Property(trace => trace.Entite).HasMaxLength(100).IsRequired();
        builder.Property(trace => trace.EntiteId).HasMaxLength(150).IsRequired();
        builder.Property(trace => trace.ReferenceMetier).HasMaxLength(300);
        builder.Property(trace => trace.UtilisateurId).HasMaxLength(150).IsRequired();
        builder.Property(trace => trace.UtilisateurNom).HasMaxLength(200).IsRequired();
        builder.Property(trace => trace.UtilisateurEmail).HasMaxLength(320);
        builder.Property(trace => trace.Role).HasMaxLength(200).IsRequired();
        builder.Property(trace => trace.Source).HasMaxLength(150).IsRequired();
        builder.Property(trace => trace.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(trace => trace.Resume).HasMaxLength(1000);
        builder.Property(trace => trace.AnciennesValeursJson).HasMaxLength(TraceAudit.JsonMaxLength);
        builder.Property(trace => trace.NouvellesValeursJson).HasMaxLength(TraceAudit.JsonMaxLength);
        builder.Property(trace => trace.InformationsJson).HasMaxLength(TraceAudit.JsonMaxLength);

        builder.HasIndex(trace => trace.DateUtc);
        builder.HasIndex(trace => trace.UtilisateurId);
        builder.HasIndex(trace => trace.Action);
        builder.HasIndex(trace => trace.Domaine);
        builder.HasIndex(trace => trace.CorrelationId);
        builder.HasIndex(trace => trace.ReferenceMetier);
        builder.HasIndex(trace => new { trace.Entite, trace.EntiteId, trace.DateUtc });
    }
}
