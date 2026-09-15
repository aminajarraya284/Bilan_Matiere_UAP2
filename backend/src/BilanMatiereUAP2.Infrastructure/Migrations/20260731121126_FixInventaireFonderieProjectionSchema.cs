using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixInventaireFonderieProjectionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM InventairesFonderie
                    GROUP BY Periode, TypeMatiereId
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51000, 'Impossible de creer l index unique InventairesFonderie(Periode, TypeMatiereId) : des doublons existent.', 1;
                END
                """);

            migrationBuilder.Sql(
                "ALTER TABLE [InventairesFonderie] ALTER COLUMN [StockDebutKg] decimal(18,3) NULL;");

            migrationBuilder.CreateIndex(
                name: "UX_InventairesFonderie_Periode_TypeMatiereId",
                table: "InventairesFonderie",
                columns: new[] { "Periode", "TypeMatiereId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_InventairesFonderie_Periode_TypeMatiereId",
                table: "InventairesFonderie");

            migrationBuilder.Sql(
                "ALTER TABLE [InventairesFonderie] ALTER COLUMN [StockDebutKg] decimal(18,3) NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFonderie_Periode_TypeMatiereId",
                table: "InventairesFonderie",
                columns: new[] { "Periode", "TypeMatiereId" });
        }
    }
}
