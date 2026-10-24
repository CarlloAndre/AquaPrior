using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaPrior.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReporteZona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reportes_Zonas_ZonaId",
                table: "Reportes");

            migrationBuilder.DropIndex(
                name: "IX_Reportes_ZonaId",
                table: "Reportes");

            migrationBuilder.DropColumn(
                name: "PersonasAfectadas",
                table: "Reportes");

            migrationBuilder.DropColumn(
                name: "ZonaId",
                table: "Reportes");

            migrationBuilder.CreateTable(
                name: "ReporteZonas",
                columns: table => new
                {
                    ReporteId = table.Column<int>(type: "int", nullable: false),
                    ZonaId = table.Column<int>(type: "int", nullable: false),
                    PersonasAfectadas = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReporteZonas", x => new { x.ReporteId, x.ZonaId });
                    table.ForeignKey(
                        name: "FK_ReporteZonas_Reportes_ReporteId",
                        column: x => x.ReporteId,
                        principalTable: "Reportes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReporteZonas_Zonas_ZonaId",
                        column: x => x.ZonaId,
                        principalTable: "Zonas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReporteZonas_ZonaId",
                table: "ReporteZonas",
                column: "ZonaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReporteZonas");

            migrationBuilder.AddColumn<int>(
                name: "PersonasAfectadas",
                table: "Reportes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ZonaId",
                table: "Reportes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reportes_ZonaId",
                table: "Reportes",
                column: "ZonaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reportes_Zonas_ZonaId",
                table: "Reportes",
                column: "ZonaId",
                principalTable: "Zonas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
