using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceProfundidadArteWithDistanciaPortones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profundidad_arte_m",
                table: "lances");

            migrationBuilder.AddColumn<double>(
                name: "distancia_portones_m",
                table: "lances",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "distancia_portones_m",
                table: "lances");

            migrationBuilder.AddColumn<int>(
                name: "profundidad_arte_m",
                table: "lances",
                type: "INTEGER",
                nullable: true);
        }
    }
}
