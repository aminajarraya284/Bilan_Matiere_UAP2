using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventaireComptagePhysique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventairesCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Categorie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Periode = table.Column<DateOnly>(type: "date", nullable: false),
                    TypeMatiereId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantiteKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreeParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreeParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifieParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifieParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ModifieLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValideParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValideParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ValideLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventairesCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventairesCategories_TypesMatiere_TypeMatiereId",
                        column: x => x.TypeMatiereId,
                        principalTable: "TypesMatiere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventairesConteneurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateInventaire = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroConteneur = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeArticle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DesignationArticle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Periode = table.Column<DateOnly>(type: "date", nullable: false),
                    TypeMatiereId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantiteKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreeParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreeParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifieParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifieParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ModifieLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValideParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValideParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ValideLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventairesConteneurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventairesConteneurs_TypesMatiere_TypeMatiereId",
                        column: x => x.TypeMatiereId,
                        principalTable: "TypesMatiere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventairesFours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateInventaire = table.Column<DateOnly>(type: "date", nullable: false),
                    CodeFour = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Periode = table.Column<DateOnly>(type: "date", nullable: false),
                    TypeMatiereId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantiteKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreeParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreeParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifieParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifieParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ModifieLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValideParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValideParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ValideLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventairesFours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventairesFours_TypesMatiere_TypeMatiereId",
                        column: x => x.TypeMatiereId,
                        principalTable: "TypesMatiere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesCategories_Periode_TypeMatiereId",
                table: "InventairesCategories",
                columns: new[] { "Periode", "TypeMatiereId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesCategories_Statut",
                table: "InventairesCategories",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesCategories_TypeMatiereId",
                table: "InventairesCategories",
                column: "TypeMatiereId");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesConteneurs_NumeroConteneur",
                table: "InventairesConteneurs",
                column: "NumeroConteneur");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesConteneurs_Periode_TypeMatiereId",
                table: "InventairesConteneurs",
                columns: new[] { "Periode", "TypeMatiereId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesConteneurs_Statut",
                table: "InventairesConteneurs",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesConteneurs_TypeMatiereId",
                table: "InventairesConteneurs",
                column: "TypeMatiereId");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFours_Periode_TypeMatiereId",
                table: "InventairesFours",
                columns: new[] { "Periode", "TypeMatiereId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFours_Statut",
                table: "InventairesFours",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFours_TypeMatiereId",
                table: "InventairesFours",
                column: "TypeMatiereId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventairesCategories");

            migrationBuilder.DropTable(
                name: "InventairesConteneurs");

            migrationBuilder.DropTable(
                name: "InventairesFours");
        }
    }
}
