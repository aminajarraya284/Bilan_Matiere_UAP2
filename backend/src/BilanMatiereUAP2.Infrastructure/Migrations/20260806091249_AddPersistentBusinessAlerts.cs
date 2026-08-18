using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentBusinessAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertesMetier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Severite = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Titre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Periode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodeArticle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TypeMatiere = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CleDeduplication = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    DetecteeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DerniereDetectionLeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcquitteeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcquitteePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ResolueLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResoluePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IgnoreeLeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IgnoreePar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CommentaireTraitement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DonneesAssocieesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResolutionAutomatique = table.Column<bool>(type: "bit", nullable: false),
                    EstActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertesMetier", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertesMetier_CleDeduplication",
                table: "AlertesMetier",
                column: "CleDeduplication",
                unique: true,
                filter: "[EstActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AlertesMetier_CodeArticle",
                table: "AlertesMetier",
                column: "CodeArticle");

            migrationBuilder.CreateIndex(
                name: "IX_AlertesMetier_DetecteeLeUtc",
                table: "AlertesMetier",
                column: "DetecteeLeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AlertesMetier_NumeroOF",
                table: "AlertesMetier",
                column: "NumeroOF");

            migrationBuilder.CreateIndex(
                name: "IX_AlertesMetier_Periode_Statut",
                table: "AlertesMetier",
                columns: new[] { "Periode", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertesMetier_Source_Severite",
                table: "AlertesMetier",
                columns: new[] { "Source", "Severite" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertesMetier");

        }
    }
}
