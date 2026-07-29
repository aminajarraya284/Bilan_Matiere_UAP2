using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRapportMachineAndReferenceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategorieComposant",
                table: "RefNomenclatures",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignationComposant",
                table: "RefNomenclatures",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PourcentageRebut",
                table: "RefNomenclatures",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantiteLien",
                table: "RefNomenclatures",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantiteLienCumulee",
                table: "RefNomenclatures",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeLien",
                table: "RefNomenclatures",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniteQuantite",
                table: "RefNomenclatures",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cadence",
                table: "RefGammesFabrication",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeAtelier",
                table: "RefGammesFabrication",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CoefficientCcpt",
                table: "RefGammesFabrication",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CoefficientOperationStock",
                table: "RefGammesFabrication",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateFinValidite",
                table: "RefGammesFabrication",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateModificationX3",
                table: "RefGammesFabrication",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignationArticle",
                table: "RefGammesFabrication",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignationOperation",
                table: "RefGammesFabrication",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EtapeSuivi",
                table: "RefGammesFabrication",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupePosteCharge",
                table: "RefGammesFabrication",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Jalon",
                table: "RefGammesFabrication",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NombrePostes",
                table: "RefGammesFabrication",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantite",
                table: "RefGammesFabrication",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TempsAttente",
                table: "RefGammesFabrication",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TempsCycle",
                table: "RefGammesFabrication",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TempsReglage",
                table: "RefGammesFabrication",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Trs",
                table: "RefGammesFabrication",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniteOperation",
                table: "RefGammesFabrication",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NombreAnomalies",
                table: "ImportBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NombreLignesImportees",
                table: "ImportBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NombreLignesLues",
                table: "ImportBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NombreLignesValides",
                table: "ImportBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE RefGammesFabrication
                SET CleMetier = UPPER(LTRIM(RTRIM(CodeArticle))) + '|' +
                    UPPER(LTRIM(RTRIM(NumeroOperation))) + '|' +
                    UPPER(LTRIM(RTRIM(ISNULL(CodeAtelier, '')))) + '|' +
                    UPPER(LTRIM(RTRIM(ISNULL(CodePoste, '')))) + '|' +
                    UPPER(LTRIM(RTRIM(ISNULL(AlternativeGamme, ''))));
                """);

            migrationBuilder.CreateTable(
                name: "ImportReferenceAnomalies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeImport = table.Column<int>(type: "int", nullable: false),
                    NumeroLigneSource = table.Column<int>(type: "int", nullable: false),
                    NomFeuilleSource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportReferenceAnomalies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportReferenceAnomalies_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "X3RapportsMachine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateProduction = table.Column<DateOnly>(type: "date", nullable: false),
                    CodePoste = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroOperation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodeArticle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DesignationArticle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Equipe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuantiteEMR = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuantiteRebutEMR = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AlternativeGamme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CleMetier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_X3RapportsMachine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_X3RapportsMachine_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefGammesFabrication_CodeAtelier",
                table: "RefGammesFabrication",
                column: "CodeAtelier");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReferenceAnomalies_ImportBatchId",
                table: "ImportReferenceAnomalies",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_X3RapportsMachine_CleMetier",
                table: "X3RapportsMachine",
                column: "CleMetier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_X3RapportsMachine_ImportBatchId",
                table: "X3RapportsMachine",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_X3RapportsMachine_IsActive",
                table: "X3RapportsMachine",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_X3RapportsMachine_NumeroOF_CodeArticle",
                table: "X3RapportsMachine",
                columns: new[] { "NumeroOF", "CodeArticle" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportReferenceAnomalies");

            migrationBuilder.DropTable(
                name: "X3RapportsMachine");

            migrationBuilder.DropIndex(
                name: "IX_RefGammesFabrication_CodeAtelier",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "CategorieComposant",
                table: "RefNomenclatures");

            migrationBuilder.DropColumn(
                name: "DesignationComposant",
                table: "RefNomenclatures");

            migrationBuilder.DropColumn(
                name: "PourcentageRebut",
                table: "RefNomenclatures");

            migrationBuilder.DropColumn(
                name: "QuantiteLien",
                table: "RefNomenclatures");

            migrationBuilder.DropColumn(
                name: "QuantiteLienCumulee",
                table: "RefNomenclatures");

            migrationBuilder.DropColumn(
                name: "TypeLien",
                table: "RefNomenclatures");

            migrationBuilder.DropColumn(
                name: "UniteQuantite",
                table: "RefNomenclatures");

            migrationBuilder.DropColumn(
                name: "Cadence",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "CodeAtelier",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "CoefficientCcpt",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "CoefficientOperationStock",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "DateFinValidite",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "DateModificationX3",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "DesignationArticle",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "DesignationOperation",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "EtapeSuivi",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "GroupePosteCharge",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "Jalon",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "NombrePostes",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "Quantite",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "TempsAttente",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "TempsCycle",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "TempsReglage",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "Trs",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "UniteOperation",
                table: "RefGammesFabrication");

            migrationBuilder.DropColumn(
                name: "NombreAnomalies",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "NombreLignesImportees",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "NombreLignesLues",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "NombreLignesValides",
                table: "ImportBatches");
        }
    }
}
