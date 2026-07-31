using BilanMatiereUAP2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BilanMatiereUAP2.Infrastructure.Persistence.Configurations;

public sealed class ReceptionUsinage2Configuration : IEntityTypeConfiguration<ReceptionUsinage2>
{
    public void Configure(EntityTypeBuilder<ReceptionUsinage2> builder)
    {
        builder.ToTable("ReceptionsUsinage2");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.NumeroOF).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.NombrePiecesReceptionnees).HasPrecision(18, 3);
        builder.Property(entity => entity.PoidsTotalReceptionneKg).HasPrecision(18, 6);
        builder.Property(entity => entity.Origine).HasMaxLength(100);
        builder.Property(entity => entity.Observation).HasMaxLength(500);

        builder.Property(entity => entity.CodeArticle).HasMaxLength(100);
        builder.Property(entity => entity.DesignationArticle).HasMaxLength(250);
        builder.Property(entity => entity.SituationX3).HasMaxLength(100);
        builder.Property(entity => entity.AtelierOrigine).HasMaxLength(50);
        builder.Property(entity => entity.CodeTypeMatiere).HasMaxLength(50);
        builder.Property(entity => entity.QuantiteLienUtilisee).HasPrecision(18, 6);
        builder.Property(entity => entity.UniteQuantiteLien).HasMaxLength(20);
        builder.Property(entity => entity.PoidsTheoriqueUnitaireKg).HasPrecision(18, 6);
        builder.Property(entity => entity.PoidsPhysiqueUnitaireKg).HasPrecision(18, 6);
        builder.Property(entity => entity.TotalCtrlSci).HasPrecision(18, 3);
        builder.Property(entity => entity.TotalCtrlEmr).HasPrecision(18, 3);
        builder.Property(entity => entity.TotalCtrlTrn).HasPrecision(18, 3);
        builder.Property(entity => entity.TotalRebutSci).HasPrecision(18, 3);
        builder.Property(entity => entity.TotalRebutEmr).HasPrecision(18, 3);
        builder.Property(entity => entity.TotalRebutTrn).HasPrecision(18, 3);
        builder.Property(entity => entity.StatutArticle).HasMaxLength(50);
        builder.Property(entity => entity.StatutAtelier).HasMaxLength(50);
        builder.Property(entity => entity.StatutTypeMatiere).HasMaxLength(50);
        builder.Property(entity => entity.StatutNomenclature).HasMaxLength(50);
        builder.Property(entity => entity.StatutPoidsPhysique).HasMaxLength(50);
        builder.Property(entity => entity.StatutRapportMachine).HasMaxLength(50);
        builder.Property(entity => entity.SourceTypeMatiere).HasMaxLength(30).IsRequired();
        // Pas de HasColumnType("nvarchar(max)") : specifique SQL Server, casse la traduction Sqlite
        // utilisee par les tests d'integration. EF Core mappe par defaut vers un texte non borne.
        builder.Property(entity => entity.AvertissementsJson);

        builder.Property(entity => entity.StatutReception).HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.DerogationJustification).HasMaxLength(500);
        builder.Property(entity => entity.CreeParNom).HasMaxLength(150);
        builder.Property(entity => entity.ModifieParNom).HasMaxLength(150);

        builder.HasIndex(entity => entity.NumeroOF);
        builder.HasIndex(entity => entity.DateReception);
        builder.HasIndex(entity => entity.StatutReception);
    }
}
