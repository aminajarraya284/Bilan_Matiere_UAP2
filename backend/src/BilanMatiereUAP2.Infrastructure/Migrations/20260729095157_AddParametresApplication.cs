using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BilanMatiereUAP2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParametresApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParametresApplication",
                columns: table => new
                {
                    Cle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valeur = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametresApplication", x => x.Cle);
                });

            migrationBuilder.InsertData(
                table: "ParametresApplication",
                columns: new[] { "Cle", "Description", "Valeur" },
                values: new object[,]
                {
                    { "CODE_ATELIER_UAP2", "Code atelier X3 identifiant le perimetre UAP2 pour les calculs.", "16" },
                    { "SEUIL_ECART_FONDERIE", "Seuil d'ecart Fonderie en pourcentage.", "5.0" },
                    { "SEUIL_ECART_UAP2", "Seuil d'ecart UAP2 en pourcentage.", "1.5" },
                    { "TRANSACTION_ENTREE_OF", "Libelle de transaction X3 retenu pour les mouvements d'entree OF.", "Entree OF" },
                    { "TYPES_MATIERE", "Codes matiere autorises pour le rapprochement Sortie MP et Nomenclature.", "M3000L0,M3010L0,M3060L0" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParametresApplication");
        }
    }
}
