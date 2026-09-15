using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncPostesProcessusEtInventairesFonderie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EligibleAtelier16",
                table: "ReceptionsUsinage2",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceTypeMatiere",
                table: "ReceptionsUsinage2",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AnnuleLeUtc",
                table: "DechetsFonderie",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnuleParNom",
                table: "DechetsFonderie",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AnnuleParUtilisateurId",
                table: "DechetsFonderie",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origine",
                table: "DechetsFonderie",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Fonderie");

            migrationBuilder.AddColumn<DateTime>(
                name: "ValideLeUtc",
                table: "DechetsFonderie",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValideParNom",
                table: "DechetsFonderie",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ValideParUtilisateurId",
                table: "DechetsFonderie",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventairesFonderie",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Periode = table.Column<DateOnly>(type: "date", nullable: false),
                    TypeMatiereId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockDebutKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    StockFinKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
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
                    ValideLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnnuleParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnnuleParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AnnuleLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventairesFonderie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventairesFonderie_TypesMatiere_TypeMatiereId",
                        column: x => x.TypeMatiereId,
                        principalTable: "TypesMatiere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefPostesProcessus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodePoste = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LibellePoste = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OrigineProcessus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ModifiePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefPostesProcessus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "X3MouvementsStock",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLigneSource = table.Column<int>(type: "int", nullable: false),
                    NomFeuilleSource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DateImputation = table.Column<DateOnly>(type: "date", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Designation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DateCreationX3 = table.Column<DateOnly>(type: "date", nullable: true),
                    HeureCreationX3 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Utilisateur = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QuantiteX3Signee = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unite = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Transaction = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TypePiece = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Lot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PieceOrigine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NumeroOFNormalise = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DesignationMouvement = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Emplacement = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    StatutX3 = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Nature = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StatutRapprochement = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_X3MouvementsStock", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefPostesProcessusHistorique",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosteProcessusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodePoste = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrigineProcessus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Utilisateur = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DateUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefPostesProcessusHistorique", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefPostesProcessusHistorique_RefPostesProcessus_PosteProcessusId",
                        column: x => x.PosteProcessusId,
                        principalTable: "RefPostesProcessus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DechetsFonderie_Origine",
                table: "DechetsFonderie",
                column: "Origine");

            migrationBuilder.CreateIndex(
                name: "IX_DechetsFonderie_Statut",
                table: "DechetsFonderie",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFonderie_Periode_TypeMatiereId",
                table: "InventairesFonderie",
                columns: new[] { "Periode", "TypeMatiereId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFonderie_Statut",
                table: "InventairesFonderie",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFonderie_TypeMatiereId",
                table: "InventairesFonderie",
                column: "TypeMatiereId");

            migrationBuilder.CreateIndex(
                name: "IX_RefPostesProcessus_CodePoste",
                table: "RefPostesProcessus",
                column: "CodePoste",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefPostesProcessus_IsActive_OrigineProcessus",
                table: "RefPostesProcessus",
                columns: new[] { "IsActive", "OrigineProcessus" });

            migrationBuilder.CreateIndex(
                name: "IX_RefPostesProcessusHistorique_PosteProcessusId_DateUtc",
                table: "RefPostesProcessusHistorique",
                columns: new[] { "PosteProcessusId", "DateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_X3MouvementsStock_Code",
                table: "X3MouvementsStock",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_X3MouvementsStock_DateImputation",
                table: "X3MouvementsStock",
                column: "DateImputation");

            migrationBuilder.CreateIndex(
                name: "IX_X3MouvementsStock_ImportBatchId",
                table: "X3MouvementsStock",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_X3MouvementsStock_Nature",
                table: "X3MouvementsStock",
                column: "Nature");

            migrationBuilder.CreateIndex(
                name: "IX_X3MouvementsStock_NumeroOFNormalise",
                table: "X3MouvementsStock",
                column: "NumeroOFNormalise");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventairesFonderie");

            migrationBuilder.DropTable(
                name: "RefPostesProcessusHistorique");

            migrationBuilder.DropTable(
                name: "X3MouvementsStock");

            migrationBuilder.DropTable(
                name: "RefPostesProcessus");

            migrationBuilder.DropIndex(
                name: "IX_DechetsFonderie_Origine",
                table: "DechetsFonderie");

            migrationBuilder.DropIndex(
                name: "IX_DechetsFonderie_Statut",
                table: "DechetsFonderie");

            migrationBuilder.DropColumn(
                name: "EligibleAtelier16",
                table: "ReceptionsUsinage2");

            migrationBuilder.DropColumn(
                name: "SourceTypeMatiere",
                table: "ReceptionsUsinage2");

            migrationBuilder.DropColumn(
                name: "AnnuleLeUtc",
                table: "DechetsFonderie");

            migrationBuilder.DropColumn(
                name: "AnnuleParNom",
                table: "DechetsFonderie");

            migrationBuilder.DropColumn(
                name: "AnnuleParUtilisateurId",
                table: "DechetsFonderie");

            migrationBuilder.DropColumn(
                name: "Origine",
                table: "DechetsFonderie");

            migrationBuilder.DropColumn(
                name: "ValideLeUtc",
                table: "DechetsFonderie");

            migrationBuilder.DropColumn(
                name: "ValideParNom",
                table: "DechetsFonderie");

            migrationBuilder.DropColumn(
                name: "ValideParUtilisateurId",
                table: "DechetsFonderie");
        }
    }
}
