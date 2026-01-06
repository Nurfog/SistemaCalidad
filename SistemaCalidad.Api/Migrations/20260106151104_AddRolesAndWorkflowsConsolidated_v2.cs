using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaCalidad.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndWorkflowsConsolidated_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoRevision",
                table: "VersionesDocumento",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ObservacionesRevision",
                table: "VersionesDocumento",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RevisadoPorId",
                table: "VersionesDocumento",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EmailExterno",
                table: "usuariospermisos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NombreCompleto",
                table: "usuariospermisos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "usuariospermisos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoRevision",
                table: "VersionesDocumento");

            migrationBuilder.DropColumn(
                name: "ObservacionesRevision",
                table: "VersionesDocumento");

            migrationBuilder.DropColumn(
                name: "RevisadoPorId",
                table: "VersionesDocumento");

            migrationBuilder.DropColumn(
                name: "EmailExterno",
                table: "usuariospermisos");

            migrationBuilder.DropColumn(
                name: "NombreCompleto",
                table: "usuariospermisos");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "usuariospermisos");
        }
    }
}
