using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class AddEspeciesViejas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "especies_viejas",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    CodigoInidep = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentoInformativo = table.Column<string>(type: "TEXT", nullable: true),
                    Especifico = table.Column<string>(type: "TEXT", nullable: true),
                    Familia = table.Column<string>(type: "TEXT", nullable: true),
                    Frecuente = table.Column<int>(type: "INTEGER", nullable: false),
                    Genero = table.Column<string>(type: "TEXT", nullable: true),
                    NombreCientifico = table.Column<string>(type: "TEXT", nullable: true),
                    NombreVulgar = table.Column<string>(type: "TEXT", nullable: true),
                    Orden = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_especies_viejas", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "ix_especies_viejas_codigo_inidep",
                table: "especies_viejas",
                column: "CodigoInidep",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "especies_viejas");
        }
    }
}
