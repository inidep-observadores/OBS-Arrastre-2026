using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OBSArrastre2026.App.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoMuestraToMuestra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoMuestra",
                table: "muestras",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoMuestra",
                table: "muestras");
        }
    }
}
