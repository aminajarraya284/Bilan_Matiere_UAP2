using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    public partial class AddManufacturingReferencesPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashFichierSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TypeImport = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProfilImport = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreeParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreeParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFinUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefGammesFabrication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeArticle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AlternativeGamme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NumeroOperation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodePoste = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CleMetier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefGammesFabrication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefGammesFabrication_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefNomenclatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeArticleParent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AlternativeNomenclature = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeParentNiveau = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodeComposant = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Niveau = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sequence = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CleMetier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefNomenclatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefNomenclatures_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_HashFichierSha256",
                table: "ImportBatches",
                column: "HashFichierSha256");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_TypeImport",
                table: "ImportBatches",
                column: "TypeImport");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_ProfilImport",
                table: "ImportBatches",
                column: "ProfilImport");

            migrationBuilder.CreateIndex(
                name: "IX_RefGammesFabrication_CleMetier",
                table: "RefGammesFabrication",
                column: "CleMetier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefGammesFabrication_CodeArticle",
                table: "RefGammesFabrication",
                column: "CodeArticle");

            migrationBuilder.CreateIndex(
                name: "IX_RefGammesFabrication_NumeroOperation",
                table: "RefGammesFabrication",
                column: "NumeroOperation");

            migrationBuilder.CreateIndex(
                name: "IX_RefGammesFabrication_IsActive",
                table: "RefGammesFabrication",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RefGammesFabrication_ImportBatchId",
                table: "RefGammesFabrication",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RefNomenclatures_CleMetier",
                table: "RefNomenclatures",
                column: "CleMetier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefNomenclatures_CodeArticleParent",
                table: "RefNomenclatures",
                column: "CodeArticleParent");

            migrationBuilder.CreateIndex(
                name: "IX_RefNomenclatures_CodeComposant",
                table: "RefNomenclatures",
                column: "CodeComposant");

            migrationBuilder.CreateIndex(
                name: "IX_RefNomenclatures_IsActive",
                table: "RefNomenclatures",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RefNomenclatures_ImportBatchId",
                table: "RefNomenclatures",
                column: "ImportBatchId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RefGammesFabrication");
            migrationBuilder.DropTable(name: "RefNomenclatures");
            migrationBuilder.DropTable(name: "ImportBatches");
        }
    }
}
