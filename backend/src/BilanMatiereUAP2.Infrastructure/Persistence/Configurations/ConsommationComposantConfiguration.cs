using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class ConsommationComposantConfiguration : IEntityTypeConfiguration<ConsommationComposant>
{
    public void Configure(EntityTypeBuilder<ConsommationComposant> builder)
    {
        builder.ToTable("ConsommationsComposants");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.NumeroOF).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.NumeroOF);

        builder.Property(e => e.ArticlePrincipal).HasMaxLength(80).IsRequired();
        builder.Property(e => e.DesignationPrincipale).HasMaxLength(250);

        builder.Property(e => e.CodeTypeMatiere).HasMaxLength(80).IsRequired();
        builder.HasIndex(e => e.CodeTypeMatiere);

        builder.Property(e => e.DesignationComposant).HasMaxLength(250);
        builder.Property(e => e.Unite).HasMaxLength(20).IsRequired();

        builder.Property(e => e.CleMetier).HasMaxLength(250).IsRequired();
        builder.HasIndex(e => e.CleMetier).IsUnique();

        builder.Property(e => e.SommeQuantiteX3Signee).HasColumnType("decimal(18,4)");
        builder.Property(e => e.Consommation).HasColumnType("decimal(18,4)");

        builder.Property(e => e.Statut)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<StatutRapprochementX3>(v))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.DateCreationUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.DateMiseAJourUtc).HasColumnType("datetime2");

        builder.HasOne(e => e.ImportBatch)
            .WithMany()
            .HasForeignKey(e => e.ImportBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ImportBatchId);
    }
}
