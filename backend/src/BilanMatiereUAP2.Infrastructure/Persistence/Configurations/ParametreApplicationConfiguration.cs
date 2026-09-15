using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class ParametreApplicationConfiguration : IEntityTypeConfiguration<ParametreApplication>
{
    public void Configure(EntityTypeBuilder<ParametreApplication> builder)
    {
        builder.ToTable("ParametresApplication");
        builder.HasKey(e => e.Cle);
        builder.Property(e => e.Cle).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Valeur).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();

        builder.HasData(
            new
            {
                Cle = ParametresApplicationCles.SeuilEcartFonderie,
                Valeur = "5.0",
                Description = "Seuil d'ecart Fonderie en pourcentage."
            },
            new
            {
                Cle = ParametresApplicationCles.SeuilEcartUap2,
                Valeur = "1.5",
                Description = "Seuil d'ecart UAP2 en pourcentage."
            },
            new
            {
                Cle = ParametresApplicationCles.CodeAtelierUap2,
                Valeur = "16",
                Description = "Code atelier X3 identifiant le perimetre UAP2 pour les calculs."
            },
            new
            {
                Cle = ParametresApplicationCles.TransactionEntreeOf,
                Valeur = "Entree OF",
                Description = "Libelle de transaction X3 retenu pour les mouvements d'entree OF."
            },
            new
            {
                Cle = ParametresApplicationCles.TypesMatiere,
                Valeur = "M3000L0,M3010L0,M3060L0",
                Description = "Codes matiere autorises pour le rapprochement Sortie MP et Nomenclature."
            });
    }
}
