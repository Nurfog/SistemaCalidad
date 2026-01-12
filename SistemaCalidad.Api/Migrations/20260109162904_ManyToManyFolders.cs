using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaCalidad.Api.Migrations
{
    /// <inheritdoc />
    public partial class ManyToManyFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documentos_CarpetasDocumentos_CarpetaDocumentoId",
                table: "Documentos");

            migrationBuilder.DropIndex(
                name: "IX_Documentos_CarpetaDocumentoId",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "CarpetaDocumentoId",
                table: "Documentos");

            migrationBuilder.CreateTable(
                name: "DocumentoCarpetas",
                columns: table => new
                {
                    CarpetasId = table.Column<int>(type: "int", nullable: false),
                    DocumentosId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoCarpetas", x => new { x.CarpetasId, x.DocumentosId });
                    table.ForeignKey(
                        name: "FK_DocumentoCarpetas_CarpetasDocumentos_CarpetasId",
                        column: x => x.CarpetasId,
                        principalTable: "CarpetasDocumentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentoCarpetas_Documentos_DocumentosId",
                        column: x => x.DocumentosId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoCarpetas_DocumentosId",
                table: "DocumentoCarpetas",
                column: "DocumentosId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentoCarpetas");

            migrationBuilder.AddColumn<int>(
                name: "CarpetaDocumentoId",
                table: "Documentos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_CarpetaDocumentoId",
                table: "Documentos",
                column: "CarpetaDocumentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documentos_CarpetasDocumentos_CarpetaDocumentoId",
                table: "Documentos",
                column: "CarpetaDocumentoId",
                principalTable: "CarpetasDocumentos",
                principalColumn: "Id");
        }
    }
}
