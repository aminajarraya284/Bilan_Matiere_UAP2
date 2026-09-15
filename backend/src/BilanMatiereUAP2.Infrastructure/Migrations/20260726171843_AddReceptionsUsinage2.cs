using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionsUsinage2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceptionsUsinage2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateReception = table.Column<DateOnly>(type: "date", nullable: false),
                    NombrePiecesReceptionnees = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PoidsTotalReceptionneKg = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Origine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CodeArticle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DesignationArticle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SituationX3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AtelierOrigine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeTypeMatiere = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuantiteLienUtilisee = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    UniteQuantiteLien = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PoidsTheoriqueUnitaireKg = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    PoidsPhysiqueUnitaireKg = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ControlePoidsFonderieId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalCtrlSci = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TotalCtrlEmr = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TotalCtrlTrn = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TotalRebutSci = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TotalRebutEmr = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TotalRebutTrn = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    StatutArticle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatutAtelier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatutTypeMatiere = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatutNomenclature = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatutPoidsPhysique = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatutRapportMachine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AvertissementsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatutReception = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DerogationJustification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreeParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreeParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifieParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifieParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DateModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceptionsUsinage2", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionsUsinage2_DateReception",
                table: "ReceptionsUsinage2",
                column: "DateReception");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionsUsinage2_NumeroOF",
                table: "ReceptionsUsinage2",
                column: "NumeroOF");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionsUsinage2_StatutReception",
                table: "ReceptionsUsinage2",
                column: "StatutReception");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceptionsUsinage2");
        }
    }
}
