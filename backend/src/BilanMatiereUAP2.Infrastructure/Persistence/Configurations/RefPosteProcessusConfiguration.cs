using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

internal sealed class RefPosteProcessusConfiguration : IEntityTypeConfiguration<RefPosteProcessus>
{
    public void Configure(EntityTypeBuilder<RefPosteProcessus> builder)
    {
        builder.ToTable("RefPostesProcessus");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.CodePoste).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.LibellePoste).HasMaxLength(200);
        builder.Property(entity => entity.OrigineProcessus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.CreePar).HasMaxLength(150);
        builder.Property(entity => entity.ModifiePar).HasMaxLength(150);
        builder.HasIndex(entity => entity.CodePoste).IsUnique();
        builder.HasIndex(entity => new { entity.IsActive, entity.OrigineProcessus });
    }
}

internal sealed class RefPosteProcessusHistoriqueConfiguration :
    IEntityTypeConfiguration<RefPosteProcessusHistorique>
{
    public void Configure(EntityTypeBuilder<RefPosteProcessusHistorique> builder)
    {
        builder.ToTable("RefPostesProcessusHistorique");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.CodePoste).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.OrigineProcessus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.Action).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Utilisateur).HasMaxLength(150);
        builder.HasIndex(entity => new { entity.PosteProcessusId, entity.DateUtc });
        builder.HasOne<RefPosteProcessus>()
            .WithMany()
            .HasForeignKey(entity => entity.PosteProcessusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
