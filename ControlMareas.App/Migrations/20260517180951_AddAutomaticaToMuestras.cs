using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticaToMuestras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "automatica",
                table: "muestras",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "automatica",
                table: "muestras");
        }
    }
}
