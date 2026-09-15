using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRebutsUsinage2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE : le scaffolder proposait ici de deplacer ArchiveErrorMessage de
            // ConsommationsComposants vers ImportBatches. Ces deux operations ont ete
            // retirees volontairement : la colonne existe DEJA sur ImportBatches (ajoutee
            // par 20260727130000_AddImportBatchReimportCounters) et n'a jamais existe sur
            // ConsommationsComposants. Les appliquer ferait echouer la migration. Seul le
            // snapshot etait desynchronise ; sa regeneration corrige la derive.

            migrationBuilder.CreateTable(
                name: "RebutsUsinage2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateDeclaration = table.Column<DateOnly>(type: "date", nullable: false),
                    Annee = table.Column<int>(type: "int", nullable: false),
                    Mois = table.Column<int>(type: "int", nullable: false),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodeArticle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    QuantiteRebutPieces = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_RebutsUsinage2", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RebutsUsinage2_Annee_Mois_NumeroOF_CodeArticle",
                table: "RebutsUsinage2",
                columns: new[] { "Annee", "Mois", "NumeroOF", "CodeArticle" });

            migrationBuilder.CreateIndex(
                name: "IX_RebutsUsinage2_NumeroOF",
                table: "RebutsUsinage2",
                column: "NumeroOF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RebutsUsinage2");
        }
    }
}
