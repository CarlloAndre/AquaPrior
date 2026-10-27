using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AquaPrior.Data;

#nullable disable

namespace AquaPrior.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AquaPriorContext))]
    [Migration("20261027030000_AgregarFinCorteYMotivoEstado")]
    public partial class AgregarFinCorteYMotivoEstado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinCorte",
                table: "Reportes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoEstado",
                table: "Reportes",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FinCorte", table: "Reportes");
            migrationBuilder.DropColumn(name: "MotivoEstado", table: "Reportes");
        }
    }
}
