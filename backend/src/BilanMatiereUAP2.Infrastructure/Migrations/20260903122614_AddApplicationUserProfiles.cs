using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UtilisateursApplication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntraObjectId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    NomAffichage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoleObserve = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstActif = table.Column<bool>(type: "bit", nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModificationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DerniereConnexionUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilisateursApplication", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateursApplication_EntraObjectId",
                table: "UtilisateursApplication",
                column: "EntraObjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateursApplication_EstActif",
                table: "UtilisateursApplication",
                column: "EstActif");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UtilisateursApplication");
        }
    }
}
