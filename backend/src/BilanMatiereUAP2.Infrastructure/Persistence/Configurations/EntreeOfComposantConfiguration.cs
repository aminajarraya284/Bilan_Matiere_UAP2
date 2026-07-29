using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class EntreeOfComposantConfiguration : IEntityTypeConfiguration<EntreeOfComposant>
{
    public void Configure(EntityTypeBuilder<EntreeOfComposant> builder)
    {
        builder.ToTable("EntreesOfComposants");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.NumeroOF).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.CodeArticle).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.DesignationArticle).HasMaxLength(250);
        builder.Property(entity => entity.Unite).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.CleMetier).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.QuantiteNetteSignee).HasPrecision(18, 6);

        builder.HasIndex(entity => entity.CleMetier).IsUnique();
        builder.HasIndex(entity => new { entity.NumeroOF, entity.DateImputation });
    }
}
