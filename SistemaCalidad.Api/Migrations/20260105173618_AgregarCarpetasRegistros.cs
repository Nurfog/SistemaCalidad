using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaCalidad.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCarpetasRegistros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Removed creation of sige_sam_v3 schema and usuario table as they are external.
            // Removed creation of existing tables (Documentos, etc.) assuming they exist.

            migrationBuilder.CreateTable(
                name: "CarpetasRegistros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Color = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarpetasRegistros", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Add Column to existing table
            migrationBuilder.AddColumn<int>(
                name: "CarpetaRegistroId",
                table: "RegistrosCalidad",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCalidad_CarpetaRegistroId",
                table: "RegistrosCalidad",
                column: "CarpetaRegistroId");

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosCalidad_CarpetasRegistros_CarpetaRegistroId",
                table: "RegistrosCalidad",
                column: "CarpetaRegistroId",
                principalTable: "CarpetasRegistros",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosCalidad_CarpetasRegistros_CarpetaRegistroId",
                table: "RegistrosCalidad");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosCalidad_CarpetaRegistroId",
                table: "RegistrosCalidad");

            migrationBuilder.DropColumn(
                name: "CarpetaRegistroId",
                table: "RegistrosCalidad");

            migrationBuilder.DropTable(
                name: "CarpetasRegistros");
        }
    }
}
