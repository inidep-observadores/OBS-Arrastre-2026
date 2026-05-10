using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OBSArrastre2026.App.Migrations
{
    /// <inheritdoc />
    public partial class AddEspecieLargoPeso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "especies_largo_peso",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    EspecieID = table.Column<string>(type: "TEXT", nullable: false),
                    Sexo = table.Column<int>(type: "INTEGER", nullable: false),
                    ParamA = table.Column<double>(type: "REAL", nullable: false),
                    ParamB = table.Column<double>(type: "REAL", nullable: false),
                    TipoMedida = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_especies_largo_peso", x => x.ID);
                    table.ForeignKey(
                        name: "FK_especies_largo_peso_especies_EspecieID",
                        column: x => x.EspecieID,
                        principalTable: "especies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_especies_largo_peso_especie_sexo",
                table: "especies_largo_peso",
                columns: new[] { "EspecieID", "Sexo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "especies_largo_peso");
        }
    }
}
