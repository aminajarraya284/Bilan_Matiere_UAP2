using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameRapportMachineQuantiteTotale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuantiteCTRL",
                table: "X3RapportsMachine",
                newName: "QuantiteTotale");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuantiteTotale",
                table: "X3RapportsMachine",
                newName: "QuantiteCTRL");
        }
    }
}
