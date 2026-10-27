using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaPrior.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCoordenadasReporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitud",
                table: "Reportes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitud",
                table: "Reportes",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "Reportes");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "Reportes");
        }
    }
}
