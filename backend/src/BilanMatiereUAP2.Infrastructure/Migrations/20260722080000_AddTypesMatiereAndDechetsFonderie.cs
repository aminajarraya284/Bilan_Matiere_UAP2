using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations;

[DbContext(typeof(BilanMatiereUAP2DbContext))]
[Migration("20260722080000_AddTypesMatiereAndDechetsFonderie")]
public partial class AddTypesMatiereAndDechetsFonderie : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TypesMatiere",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EstActif = table.Column<bool>(type: "bit", nullable: false),
                DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                DateModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TypesMatiere", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DechetsFonderie",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DateDeclaration = table.Column<DateOnly>(type: "date", nullable: false),
                SemaineIso = table.Column<int>(type: "int", nullable: false),
                TypeMatiereId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TypeDechet = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                PoidsKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Observation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CreeParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreeParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                DateModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                EstSupprime = table.Column<bool>(type: "bit", nullable: false),
                DateSuppressionUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DechetsFonderie", x => x.Id);
                table.ForeignKey(
                    name: "FK_DechetsFonderie_TypesMatiere_TypeMatiereId",
                    column: x => x.TypeMatiereId,
                    principalTable: "TypesMatiere",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "TypesMatiere",
            columns: ["Id", "Code", "DateCreationUtc", "DateModificationUtc", "EstActif", "Libelle"],
            columnTypes: ["uniqueidentifier", "nvarchar(30)", "datetime2", "datetime2", "bit", "nvarchar(100)"],
            values: new object[,]
            {
                { new Guid("30000000-0000-0000-0000-000000000001"), "M3000L0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, true, "Matière M3000L0" },
                { new Guid("30100000-0000-0000-0000-000000000001"), "M3010L0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, true, "Matière M3010L0" },
                { new Guid("30600000-0000-0000-0000-000000000001"), "M3060L0", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, true, "Matière M3060L0" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_DechetsFonderie_DateDeclaration",
            table: "DechetsFonderie",
            column: "DateDeclaration");
        migrationBuilder.CreateIndex(
            name: "IX_DechetsFonderie_EstSupprime",
            table: "DechetsFonderie",
            column: "EstSupprime");
        migrationBuilder.CreateIndex(
            name: "IX_DechetsFonderie_TypeDechet",
            table: "DechetsFonderie",
            column: "TypeDechet");
        migrationBuilder.CreateIndex(
            name: "IX_DechetsFonderie_TypeMatiereId",
            table: "DechetsFonderie",
            column: "TypeMatiereId");
        migrationBuilder.CreateIndex(
            name: "IX_TypesMatiere_Code",
            table: "TypesMatiere",
            column: "Code",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DechetsFonderie");
        migrationBuilder.DropTable(name: "TypesMatiere");
    }
}
