using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventaireFonderiePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventairesCategories_TypesMatiere_TypeMatiereId",
                table: "InventairesCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_InventairesConteneurs_TypesMatiere_TypeMatiereId",
                table: "InventairesConteneurs");

            migrationBuilder.DropForeignKey(
                name: "FK_InventairesFours_TypesMatiere_TypeMatiereId",
                table: "InventairesFours");

            migrationBuilder.DropIndex(
                name: "IX_InventairesFours_Periode_TypeMatiereId",
                table: "InventairesFours");

            migrationBuilder.DropIndex(
                name: "IX_InventairesFours_Statut",
                table: "InventairesFours");

            migrationBuilder.DropIndex(
                name: "IX_InventairesFours_TypeMatiereId",
                table: "InventairesFours");

            migrationBuilder.DropIndex(
                name: "IX_InventairesFonderie_Periode_TypeMatiereId",
                table: "InventairesFonderie");

            migrationBuilder.DropIndex(
                name: "IX_InventairesCategories_Periode_TypeMatiereId",
                table: "InventairesCategories");

            migrationBuilder.DropIndex(
                name: "IX_InventairesCategories_Statut",
                table: "InventairesCategories");

            migrationBuilder.DropIndex(
                name: "IX_InventairesCategories_TypeMatiereId",
                table: "InventairesCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventairesConteneurs",
                table: "InventairesConteneurs");

            migrationBuilder.DropColumn(
                name: "CreeParNom",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "CreeParUtilisateurId",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "ModifieParNom",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "ModifieParUtilisateurId",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "Periode",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "TypeMatiereId",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "ValideParNom",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "ValideParUtilisateurId",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "CreeParNom",
                table: "InventairesCategories");

            migrationBuilder.DropColumn(
                name: "CreeParUtilisateurId",
                table: "InventairesCategories");

            migrationBuilder.DropColumn(
                name: "ModifieParNom",
                table: "InventairesCategories");

            migrationBuilder.DropColumn(
                name: "ModifieParUtilisateurId",
                table: "InventairesCategories");

            migrationBuilder.DropColumn(
                name: "TypeMatiereId",
                table: "InventairesCategories");

            migrationBuilder.DropColumn(
                name: "ValideParNom",
                table: "InventairesCategories");

            migrationBuilder.DropColumn(
                name: "ValideParUtilisateurId",
                table: "InventairesCategories");

            migrationBuilder.RenameTable(
                name: "InventairesConteneurs",
                newName: "InventairesConteneursComptage");

            migrationBuilder.RenameColumn(
                name: "QuantiteKg",
                table: "InventairesCategories",
                newName: "PoidsReelKg");

            migrationBuilder.RenameColumn(
                name: "Periode",
                table: "InventairesCategories",
                newName: "DateInventaire");

            migrationBuilder.RenameColumn(
                name: "Observation",
                table: "InventairesCategories",
                newName: "Commentaire");

            migrationBuilder.RenameIndex(
                name: "IX_InventairesConteneurs_TypeMatiereId",
                table: "InventairesConteneursComptage",
                newName: "IX_InventairesConteneursComptage_TypeMatiereId");

            migrationBuilder.RenameIndex(
                name: "IX_InventairesConteneurs_Statut",
                table: "InventairesConteneursComptage",
                newName: "IX_InventairesConteneursComptage_Statut");

            migrationBuilder.RenameIndex(
                name: "IX_InventairesConteneurs_Periode_TypeMatiereId",
                table: "InventairesConteneursComptage",
                newName: "IX_InventairesConteneursComptage_Periode_TypeMatiereId");

            migrationBuilder.RenameIndex(
                name: "IX_InventairesConteneurs_NumeroConteneur",
                table: "InventairesConteneursComptage",
                newName: "IX_InventairesConteneursComptage_NumeroConteneur");

            migrationBuilder.AlterColumn<int>(
                name: "Statut",
                table: "InventairesFours",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<int>(
                name: "Annee",
                table: "InventairesFours",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnnuleLeUtc",
                table: "InventairesFours",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnulePar",
                table: "InventairesFours",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeTypeMatiere",
                table: "InventairesFours",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Mois",
                table: "InventairesFours",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ValidePar",
                table: "InventairesFours",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Statut",
                table: "InventairesCategories",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Categorie",
                table: "InventairesCategories",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnnuleLeUtc",
                table: "InventairesCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnulePar",
                table: "InventairesCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeTypeMatiere",
                table: "InventairesCategories",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ValidePar",
                table: "InventairesCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventairesConteneursComptage",
                table: "InventairesConteneursComptage",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "InventairesCategoriesComptage",
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
                    table.PrimaryKey("PK_InventairesCategoriesComptage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventairesCategoriesComptage_TypesMatiere_TypeMatiereId",
                        column: x => x.TypeMatiereId,
                        principalTable: "TypesMatiere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventairesConteneursOf",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateInventaire = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroConteneur = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodeTypeMatiere = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PoidsReelKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifieLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValideLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidePar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AnnuleLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnnulePar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventairesConteneursOf", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventairesFonderiePeriodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mois = table.Column<int>(type: "int", nullable: false),
                    Annee = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    CreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValideLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidePar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClotureLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CloturePar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventairesFonderiePeriodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventairesFoursComptage",
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
                    table.PrimaryKey("PK_InventairesFoursComptage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventairesFoursComptage_TypesMatiere_TypeMatiereId",
                        column: x => x.TypeMatiereId,
                        principalTable: "TypesMatiere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFours_Annee_Mois_CodeFour",
                table: "InventairesFours",
                columns: new[] { "Annee", "Mois", "CodeFour" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFours_DateInventaire_CodeTypeMatiere_Statut",
                table: "InventairesFours",
                columns: new[] { "DateInventaire", "CodeTypeMatiere", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFonderie_Periode_TypeMatiereId",
                table: "InventairesFonderie",
                columns: new[] { "Periode", "TypeMatiereId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesCategories_DateInventaire_Categorie",
                table: "InventairesCategories",
                columns: new[] { "DateInventaire", "Categorie" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesCategories_DateInventaire_CodeTypeMatiere_Statut",
                table: "InventairesCategories",
                columns: new[] { "DateInventaire", "CodeTypeMatiere", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesCategoriesComptage_Periode_TypeMatiereId",
                table: "InventairesCategoriesComptage",
                columns: new[] { "Periode", "TypeMatiereId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesCategoriesComptage_Statut",
                table: "InventairesCategoriesComptage",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesCategoriesComptage_TypeMatiereId",
                table: "InventairesCategoriesComptage",
                column: "TypeMatiereId");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesConteneursOf_DateInventaire_CodeTypeMatiere_Statut",
                table: "InventairesConteneursOf",
                columns: new[] { "DateInventaire", "CodeTypeMatiere", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesConteneursOf_DateInventaire_NumeroConteneur",
                table: "InventairesConteneursOf",
                columns: new[] { "DateInventaire", "NumeroConteneur" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesConteneursOf_NumeroOF_CodeTypeMatiere",
                table: "InventairesConteneursOf",
                columns: new[] { "NumeroOF", "CodeTypeMatiere" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFonderiePeriodes_Annee_Mois",
                table: "InventairesFonderiePeriodes",
                columns: new[] { "Annee", "Mois" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFoursComptage_Periode_TypeMatiereId",
                table: "InventairesFoursComptage",
                columns: new[] { "Periode", "TypeMatiereId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFoursComptage_Statut",
                table: "InventairesFoursComptage",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFoursComptage_TypeMatiereId",
                table: "InventairesFoursComptage",
                column: "TypeMatiereId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventairesConteneursComptage_TypesMatiere_TypeMatiereId",
                table: "InventairesConteneursComptage",
                column: "TypeMatiereId",
                principalTable: "TypesMatiere",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventairesConteneursComptage_TypesMatiere_TypeMatiereId",
                table: "InventairesConteneursComptage");

            migrationBuilder.DropTable(
                name: "InventairesCategoriesComptage");

            migrationBuilder.DropTable(
                name: "InventairesConteneursOf");

            migrationBuilder.DropTable(
                name: "InventairesFonderiePeriodes");

            migrationBuilder.DropTable(
                name: "InventairesFoursComptage");

            migrationBuilder.DropIndex(
                name: "IX_InventairesFours_Annee_Mois_CodeFour",
                table: "InventairesFours");

            migrationBuilder.DropIndex(
                name: "IX_InventairesFours_DateInventaire_CodeTypeMatiere_Statut",
                table: "InventairesFours");

            migrationBuilder.DropIndex(
                name: "IX_InventairesFonderie_Periode_TypeMatiereId",
                table: "InventairesFonderie");

            migrationBuilder.DropIndex(
                name: "IX_InventairesCategories_DateInventaire_Categorie",
                table: "InventairesCategories");

            migrationBuilder.DropIndex(
                name: "IX_InventairesCategories_DateInventaire_CodeTypeMatiere_Statut",
                table: "InventairesCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventairesConteneursComptage",
                table: "InventairesConteneursComptage");

            migrationBuilder.DropColumn(
                name: "Annee",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "AnnuleLeUtc",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "AnnulePar",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "CodeTypeMatiere",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "Mois",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "ValidePar",
                table: "InventairesFours");

            migrationBuilder.DropColumn(
                name: "AnnuleLeUtc",
                table: "InventairesCategories");

            migrationBuilder.DropColumn(
                name: "AnnulePar",
                table: "InventairesCategories");

            migrationBuilder.DropColumn(
                name: "CodeTypeMatiere",
                table: "InventairesCategories");

            migrationBuilder.DropColumn(
                name: "ValidePar",
                table: "InventairesCategories");

            migrationBuilder.RenameTable(
                name: "InventairesConteneursComptage",
                newName: "InventairesConteneurs");

            migrationBuilder.RenameColumn(
                name: "PoidsReelKg",
                table: "InventairesCategories",
                newName: "QuantiteKg");

            migrationBuilder.RenameColumn(
                name: "DateInventaire",
                table: "InventairesCategories",
                newName: "Periode");

            migrationBuilder.RenameColumn(
                name: "Commentaire",
                table: "InventairesCategories",
                newName: "Observation");

            migrationBuilder.RenameIndex(
                name: "IX_InventairesConteneursComptage_TypeMatiereId",
                table: "InventairesConteneurs",
                newName: "IX_InventairesConteneurs_TypeMatiereId");

            migrationBuilder.RenameIndex(
                name: "IX_InventairesConteneursComptage_Statut",
                table: "InventairesConteneurs",
                newName: "IX_InventairesConteneurs_Statut");

            migrationBuilder.RenameIndex(
                name: "IX_InventairesConteneursComptage_Periode_TypeMatiereId",
                table: "InventairesConteneurs",
                newName: "IX_InventairesConteneurs_Periode_TypeMatiereId");

            migrationBuilder.RenameIndex(
                name: "IX_InventairesConteneursComptage_NumeroConteneur",
                table: "InventairesConteneurs",
                newName: "IX_InventairesConteneurs_NumeroConteneur");

            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "InventairesFours",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CreeParNom",
                table: "InventairesFours",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreeParUtilisateurId",
                table: "InventairesFours",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifieParNom",
                table: "InventairesFours",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifieParUtilisateurId",
                table: "InventairesFours",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Periode",
                table: "InventairesFours",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<Guid>(
                name: "TypeMatiereId",
                table: "InventairesFours",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ValideParNom",
                table: "InventairesFours",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ValideParUtilisateurId",
                table: "InventairesFours",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "InventairesCategories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Categorie",
                table: "InventairesCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CreeParNom",
                table: "InventairesCategories",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreeParUtilisateurId",
                table: "InventairesCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifieParNom",
                table: "InventairesCategories",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifieParUtilisateurId",
                table: "InventairesCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TypeMatiereId",
                table: "InventairesCategories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ValideParNom",
                table: "InventairesCategories",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ValideParUtilisateurId",
                table: "InventairesCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventairesConteneurs",
                table: "InventairesConteneurs",
                column: "Id");

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

            migrationBuilder.CreateIndex(
                name: "IX_InventairesFonderie_Periode_TypeMatiereId",
                table: "InventairesFonderie",
                columns: new[] { "Periode", "TypeMatiereId" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_InventairesCategories_TypesMatiere_TypeMatiereId",
                table: "InventairesCategories",
                column: "TypeMatiereId",
                principalTable: "TypesMatiere",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventairesConteneurs_TypesMatiere_TypeMatiereId",
                table: "InventairesConteneurs",
                column: "TypeMatiereId",
                principalTable: "TypesMatiere",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventairesFours_TypesMatiere_TypeMatiereId",
                table: "InventairesFours",
                column: "TypeMatiereId",
                principalTable: "TypesMatiere",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
