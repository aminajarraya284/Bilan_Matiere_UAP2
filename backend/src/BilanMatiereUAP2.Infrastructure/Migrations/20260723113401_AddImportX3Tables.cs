using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportX3Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportX3Batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashCoupleSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LignesEncoursLues = table.Column<int>(type: "int", nullable: false),
                    LignesEncoursValides = table.Column<int>(type: "int", nullable: false),
                    LignesMouvementsLues = table.Column<int>(type: "int", nullable: false),
                    LignesMouvementsValides = table.Column<int>(type: "int", nullable: false),
                    NombreSortiesGenerees = table.Column<int>(type: "int", nullable: false),
                    NombreAnomalies = table.Column<int>(type: "int", nullable: false),
                    CreeParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreeParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFinUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportX3Batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportX3Anomalies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeFichier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NumeroLigneSource = table.Column<int>(type: "int", nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeTypeMatiere = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    NomFeuilleSource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportX3Anomalies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportX3Anomalies_ImportX3Batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportX3Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportX3Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeFichier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NomFichier = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TailleOctets = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportX3Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportX3Files_ImportX3Batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportX3Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdresFabrication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ArticlePrincipal = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DesignationPrincipale = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SituationOF = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Atelier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DateObjectifOF = table.Column<DateOnly>(type: "date", nullable: true),
                    StatutX3 = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DernierImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdresFabrication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdresFabrication_ImportX3Batches_DernierImportBatchId",
                        column: x => x.DernierImportBatchId,
                        principalTable: "ImportX3Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsommationsComposants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateImputation = table.Column<DateOnly>(type: "date", nullable: false),
                    OrdreFabricationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ArticlePrincipal = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DesignationPrincipale = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CodeTypeMatiere = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DesignationComposant = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Unite = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CleMetier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SommeQuantiteX3Signee = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Consommation = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiseAJourUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsommationsComposants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsommationsComposants_ImportX3Batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportX3Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsommationsComposants_OrdresFabrication_OrdreFabricationId",
                        column: x => x.OrdreFabricationId,
                        principalTable: "OrdresFabrication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationsOF",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdreFabricationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLigneSource = table.Column<int>(type: "int", nullable: false),
                    NumeroOperation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodePoste = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TypePoste = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NumeroPoste = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EffectifPoste = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Cad = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EffectifOperation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    QuantitePrevue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    QuantiteRestante = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DateOperation = table.Column<DateOnly>(type: "date", nullable: true),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationsOF", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationsOF_ImportX3Batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportX3Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationsOF_OrdresFabrication_OrdreFabricationId",
                        column: x => x.OrdreFabricationId,
                        principalTable: "OrdresFabrication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsommationsComposants_CleMetier",
                table: "ConsommationsComposants",
                column: "CleMetier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsommationsComposants_CodeTypeMatiere",
                table: "ConsommationsComposants",
                column: "CodeTypeMatiere");

            migrationBuilder.CreateIndex(
                name: "IX_ConsommationsComposants_ImportBatchId",
                table: "ConsommationsComposants",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsommationsComposants_NumeroOF",
                table: "ConsommationsComposants",
                column: "NumeroOF");

            migrationBuilder.CreateIndex(
                name: "IX_ConsommationsComposants_OrdreFabricationId",
                table: "ConsommationsComposants",
                column: "OrdreFabricationId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportX3Anomalies_ImportBatchId",
                table: "ImportX3Anomalies",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportX3Batches_HashCoupleSha256",
                table: "ImportX3Batches",
                column: "HashCoupleSha256");

            migrationBuilder.CreateIndex(
                name: "IX_ImportX3Files_ImportBatchId",
                table: "ImportX3Files",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationsOF_ImportBatchId",
                table: "OperationsOF",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationsOF_OrdreFabricationId_NumeroOperation_CodePoste",
                table: "OperationsOF",
                columns: new[] { "OrdreFabricationId", "NumeroOperation", "CodePoste" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdresFabrication_ArticlePrincipal",
                table: "OrdresFabrication",
                column: "ArticlePrincipal");

            migrationBuilder.CreateIndex(
                name: "IX_OrdresFabrication_DernierImportBatchId",
                table: "OrdresFabrication",
                column: "DernierImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdresFabrication_NumeroOF",
                table: "OrdresFabrication",
                column: "NumeroOF",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsommationsComposants");

            migrationBuilder.DropTable(
                name: "ImportX3Anomalies");

            migrationBuilder.DropTable(
                name: "ImportX3Files");

            migrationBuilder.DropTable(
                name: "OperationsOF");

            migrationBuilder.DropTable(
                name: "OrdresFabrication");

            migrationBuilder.DropTable(
                name: "ImportX3Batches");
        }
    }
}
