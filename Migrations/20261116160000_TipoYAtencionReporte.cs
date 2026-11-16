using AquaPrior.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaPrior.Migrations;

/// <summary>Clasifica el tipo de problema y permite registrar atención operativa.</summary>
[DbContext(typeof(AquaPriorContext))]
[Migration("20261116160000_TipoYAtencionReporte")]
public partial class TipoYAtencionReporte : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Tipo", table: "Reportes", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(
            name: "Atencion", table: "Reportes", type: "int", nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Tipo", table: "Reportes");
        migrationBuilder.DropColumn(name: "Atencion", table: "Reportes");
    }
}
