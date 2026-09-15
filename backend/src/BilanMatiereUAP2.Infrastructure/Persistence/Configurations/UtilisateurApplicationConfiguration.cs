using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class UtilisateurApplicationConfiguration : IEntityTypeConfiguration<UtilisateurApplication>
{
    public void Configure(EntityTypeBuilder<UtilisateurApplication> builder)
    {
        builder.ToTable("UtilisateursApplication");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.EntraObjectId)
            .HasMaxLength(UtilisateurApplication.EntraObjectIdMaxLength)
            .IsRequired();
        builder.Property(u => u.Email).HasMaxLength(UtilisateurApplication.EmailMaxLength);
        builder.Property(u => u.NomAffichage).HasMaxLength(UtilisateurApplication.NomAffichageMaxLength);
        // Photographie du role du jeton : affichage et diagnostic uniquement.
        builder.Property(u => u.RoleObserve).HasMaxLength(50);
        builder.Property(u => u.EstActif).IsRequired();

        // L'oid est la seule cle stable : l'email peut changer chez Entra.
        builder.HasIndex(u => u.EntraObjectId).IsUnique();
        builder.HasIndex(u => u.EstActif);
    }
}
