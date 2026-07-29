using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameRapportMachineQuantitesAddOrigineProcessus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuantiteRebutEMR",
                table: "X3RapportsMachine",
                newName: "QuantiteRebut");

            migrationBuilder.RenameColumn(
                name: "QuantiteEMR",
                table: "X3RapportsMachine",
                newName: "QuantiteCTRL");

            migrationBuilder.AddColumn<string>(
                name: "OrigineProcessus",
                table: "X3RapportsMachine",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ControlesPoidsFonderie",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateControle = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeArticle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TypeMatiereId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLotOuSerie = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PoidsPhysiqueUnitaireKg = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NombreEchantillons = table.Column<int>(type: "int", nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Brouillon"),
                    CreeParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreeParNom = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValideParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValideParNom = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ValideLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifieLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstSupprime = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlesPoidsFonderie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlesPoidsFonderie_TypesMatiere_TypeMatiereId",
                        column: x => x.TypeMatiereId,
                        principalTable: "TypesMatiere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_X3RapportsMachine_OrigineProcessus",
                table: "X3RapportsMachine",
                column: "OrigineProcessus");

            migrationBuilder.CreateIndex(
                name: "IX_ControlesPoidsFonderie_CodeArticle_TypeMatiereId_DateControle",
                table: "ControlesPoidsFonderie",
                columns: new[] { "CodeArticle", "TypeMatiereId", "DateControle" });

            migrationBuilder.CreateIndex(
                name: "IX_ControlesPoidsFonderie_NumeroOF",
                table: "ControlesPoidsFonderie",
                column: "NumeroOF");

            migrationBuilder.CreateIndex(
                name: "IX_ControlesPoidsFonderie_TypeMatiereId",
                table: "ControlesPoidsFonderie",
                column: "TypeMatiereId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ControlesPoidsFonderie");

            migrationBuilder.DropIndex(
                name: "IX_X3RapportsMachine_OrigineProcessus",
                table: "X3RapportsMachine");

            migrationBuilder.DropColumn(
                name: "OrigineProcessus",
                table: "X3RapportsMachine");

            migrationBuilder.RenameColumn(
                name: "QuantiteRebut",
                table: "X3RapportsMachine",
                newName: "QuantiteRebutEMR");

            migrationBuilder.RenameColumn(
                name: "QuantiteCTRL",
                table: "X3RapportsMachine",
                newName: "QuantiteEMR");
        }
    }
}
