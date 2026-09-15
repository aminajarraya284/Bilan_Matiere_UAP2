using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditTraces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Domaine = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Entite = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntiteId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ReferenceMetier = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    UtilisateurId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UtilisateurNom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UtilisateurEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Resume = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnciennesValeursJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    NouvellesValeursJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    InformationsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTraces", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTraces_Action",
                table: "AuditTraces",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTraces_CorrelationId",
                table: "AuditTraces",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTraces_DateUtc",
                table: "AuditTraces",
                column: "DateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTraces_Domaine",
                table: "AuditTraces",
                column: "Domaine");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTraces_Entite_EntiteId_DateUtc",
                table: "AuditTraces",
                columns: new[] { "Entite", "EntiteId", "DateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditTraces_ReferenceMetier",
                table: "AuditTraces",
                column: "ReferenceMetier");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTraces_UtilisateurId",
                table: "AuditTraces",
                column: "UtilisateurId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditTraces");
        }
    }
}
