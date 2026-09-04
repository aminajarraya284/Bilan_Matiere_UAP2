using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodBalanceArchives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PeriodesBilansArchivees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Periode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    DateEnregistrementUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnregistrePar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StatutGlobal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VersionCalcul = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    NombreAlertes = table.Column<int>(type: "int", nullable: false),
                    NombreAnomalies = table.Column<int>(type: "int", nullable: false),
                    NombreDonneesManquantes = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RapportTemplateVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodesBilansArchivees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodesBilansArchivees_Periode",
                table: "PeriodesBilansArchivees",
                column: "Periode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeriodesBilansArchivees");
        }
    }
}
