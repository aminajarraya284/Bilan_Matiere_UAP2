using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilanMatiereUAP2.Infrastructure.Migrations;

[DbContext(typeof(BilanMatiereUAP2DbContext))]
[Migration("20260727130000_AddImportBatchReimportCounters")]
public partial class AddImportBatchReimportCounters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ArchiveErrorMessage",
            table: "ImportBatches",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);

        foreach (var columnName in new[] { "ReadCount", "ValidCount", "InsertedCount", "UpdatedCount", "UnchangedCount", "RejectedCount" })
        {
            migrationBuilder.AddColumn<int>(
                name: columnName,
                table: "ImportBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        migrationBuilder.Sql(
            """
            UPDATE ImportBatches
            SET ReadCount = NombreLignesLues,
                ValidCount = NombreLignesValides,
                InsertedCount = CASE
                    WHEN NombreLignesImportees > NombreLignesValides THEN NombreLignesValides
                    ELSE NombreLignesImportees
                END,
                UpdatedCount = 0,
                UnchangedCount = CASE
                    WHEN NombreLignesValides > NombreLignesImportees THEN NombreLignesValides - NombreLignesImportees
                    ELSE 0
                END,
                RejectedCount = CASE
                    WHEN NombreLignesLues > NombreLignesValides THEN NombreLignesLues - NombreLignesValides
                    ELSE 0
                END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ArchiveErrorMessage", table: "ImportBatches");
        foreach (var columnName in new[] { "ReadCount", "ValidCount", "InsertedCount", "UpdatedCount", "UnchangedCount", "RejectedCount" })
        {
            migrationBuilder.DropColumn(name: columnName, table: "ImportBatches");
        }
    }
}
