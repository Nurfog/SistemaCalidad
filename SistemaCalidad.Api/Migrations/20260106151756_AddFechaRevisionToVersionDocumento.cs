using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaCalidad.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFechaRevisionToVersionDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRevision",
                table: "VersionesDocumento",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaRevision",
                table: "VersionesDocumento");
        }
    }
}
