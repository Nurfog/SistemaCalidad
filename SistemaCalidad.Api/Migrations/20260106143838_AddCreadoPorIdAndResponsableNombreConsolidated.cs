using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaCalidad.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreadoPorIdAndResponsableNombreConsolidated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreadoPorId",
                table: "NoConformidades",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResponsableNombre",
                table: "AccionesCalidad",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "NoConformidades");

            migrationBuilder.DropColumn(
                name: "ResponsableNombre",
                table: "AccionesCalidad");
        }
    }
}
