using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class AddMareaMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuqueCodigo",
                table: "mareas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservadorApellido",
                table: "mareas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ObservadorCodigo",
                table: "mareas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservadorNombre",
                table: "mareas",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuqueCodigo",
                table: "mareas");

            migrationBuilder.DropColumn(
                name: "ObservadorApellido",
                table: "mareas");

            migrationBuilder.DropColumn(
                name: "ObservadorCodigo",
                table: "mareas");

            migrationBuilder.DropColumn(
                name: "ObservadorNombre",
                table: "mareas");
        }
    }
}
