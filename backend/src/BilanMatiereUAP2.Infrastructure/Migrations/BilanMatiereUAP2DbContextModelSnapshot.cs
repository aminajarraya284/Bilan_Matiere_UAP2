using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations;

[DbContext(typeof(BilanMatiereUAP2DbContext))]
public partial class BilanMatiereUAP2DbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("BilanMatiereUAP2.Domain.Entities.TypeMatiere", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<string>("Code").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            b.Property<DateTime>("DateCreationUtc").HasColumnType("datetime2");
            b.Property<DateTime?>("DateModificationUtc").HasColumnType("datetime2");
            b.Property<bool>("EstActif").HasColumnType("bit");
            b.Property<string>("Libelle").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.HasKey("Id");
            b.HasIndex("Code").IsUnique();
            b.ToTable("TypesMatiere");
            b.HasData(
                new { Id = new Guid("30000000-0000-0000-0000-000000000001"), Code = "M3000L0", DateCreationUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateModificationUtc = (DateTime?)null, EstActif = true, Libelle = "Matière M3000L0" },
                new { Id = new Guid("30100000-0000-0000-0000-000000000001"), Code = "M3010L0", DateCreationUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateModificationUtc = (DateTime?)null, EstActif = true, Libelle = "Matière M3010L0" },
                new { Id = new Guid("30600000-0000-0000-0000-000000000001"), Code = "M3060L0", DateCreationUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateModificationUtc = (DateTime?)null, EstActif = true, Libelle = "Matière M3060L0" });
        });

        modelBuilder.Entity("BilanMatiereUAP2.Domain.Entities.DechetFonderie", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            b.Property<Guid?>("CreeParUtilisateurId").HasColumnType("uniqueidentifier");
            b.Property<string>("CreeParNom").HasMaxLength(150).HasColumnType("nvarchar(150)");
            b.Property<DateTime>("DateCreationUtc").HasColumnType("datetime2");
            b.Property<DateOnly>("DateDeclaration").HasColumnType("date");
            b.Property<DateTime?>("DateModificationUtc").HasColumnType("datetime2");
            b.Property<DateTime?>("DateSuppressionUtc").HasColumnType("datetime2");
            b.Property<bool>("EstSupprime").HasColumnType("bit");
            b.Property<string>("Observation").HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<decimal>("PoidsKg").HasPrecision(18, 3).HasColumnType("decimal(18,3)");
            b.Property<int>("SemaineIso").HasColumnType("int");
            b.Property<BilanMatiereUAP2.Domain.Enums.StatutDechet>("Statut")
                .HasConversion<string>().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<BilanMatiereUAP2.Domain.Enums.TypeDechet>("TypeDechet")
                .HasConversion<string>().HasMaxLength(20).HasColumnType("nvarchar(20)");
            b.Property<Guid>("TypeMatiereId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("DateDeclaration");
            b.HasIndex("EstSupprime");
            b.HasIndex("TypeDechet");
            b.HasIndex("TypeMatiereId");
            b.ToTable("DechetsFonderie");
            b.HasQueryFilter((DechetFonderie entity) => !entity.EstSupprime);
        });

        modelBuilder.Entity("BilanMatiereUAP2.Domain.Entities.DechetFonderie", b =>
        {
            b.HasOne("BilanMatiereUAP2.Domain.Entities.TypeMatiere", "TypeMatiere")
                .WithMany()
                .HasForeignKey("TypeMatiereId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            b.Navigation("TypeMatiere");
        });
    }
}
