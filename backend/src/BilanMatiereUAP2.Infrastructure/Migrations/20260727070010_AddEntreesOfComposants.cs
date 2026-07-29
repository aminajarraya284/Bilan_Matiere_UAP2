using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntreesOfComposants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntreesOfComposants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateImputation = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroOF = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodeArticle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DesignationArticle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Unite = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CleMetier = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    QuantiteNetteSignee = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DateCreationUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiseAJourUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntreesOfComposants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntreesOfComposants_CleMetier",
                table: "EntreesOfComposants",
                column: "CleMetier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntreesOfComposants_NumeroOF_DateImputation",
                table: "EntreesOfComposants",
                columns: new[] { "NumeroOF", "DateImputation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntreesOfComposants");
        }
    }
}
