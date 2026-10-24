using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AquaPrior.Data;

#nullable disable

namespace AquaPrior.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AquaPriorContext))]
    [Migration("20261024150000_AgregarMotivoReporte")]
    public partial class AgregarMotivoReporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                table: "Reportes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Motivo",
                table: "Reportes");
        }
    }
}
