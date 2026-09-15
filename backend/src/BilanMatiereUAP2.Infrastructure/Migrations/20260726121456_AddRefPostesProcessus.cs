using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefPostesProcessus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_RefPostesProcessus_CodePoste",
                table: "RefPostesProcessus",
                column: "CodePoste",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefPostesProcessus");
        }
    }
}
