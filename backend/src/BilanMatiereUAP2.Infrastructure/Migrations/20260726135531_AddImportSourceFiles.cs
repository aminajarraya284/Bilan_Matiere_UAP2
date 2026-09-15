using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportSourceFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportSourceFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeImport = table.Column<int>(type: "int", nullable: false),
                    ProfilImport = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NomFichierOriginal = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TailleOctets = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CheminStockage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DateUploadUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadeParUtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadeParNom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportSourceFiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportSourceFiles_HashSha256",
                table: "ImportSourceFiles",
                column: "HashSha256");

            migrationBuilder.CreateIndex(
                name: "IX_ImportSourceFiles_ImportBatchId",
                table: "ImportSourceFiles",
                column: "ImportBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportSourceFiles");
        }
    }
}
