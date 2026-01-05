using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaCalidad.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCarpetasDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CarpetaDocumentoId",
                table: "Documentos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarpetasDocumentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Color = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarpetasDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarpetasDocumentos_CarpetasDocumentos_ParentId",
                        column: x => x.ParentId,
                        principalTable: "CarpetasDocumentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_CarpetaDocumentoId",
                table: "Documentos",
                column: "CarpetaDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_CarpetasDocumentos_ParentId",
                table: "CarpetasDocumentos",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documentos_CarpetasDocumentos_CarpetaDocumentoId",
                table: "Documentos",
                column: "CarpetaDocumentoId",
                principalTable: "CarpetasDocumentos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documentos_CarpetasDocumentos_CarpetaDocumentoId",
                table: "Documentos");

            migrationBuilder.DropTable(
                name: "CarpetasDocumentos");

            migrationBuilder.DropIndex(
                name: "IX_Documentos_CarpetaDocumentoId",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "CarpetaDocumentoId",
                table: "Documentos");
        }
    }
}
