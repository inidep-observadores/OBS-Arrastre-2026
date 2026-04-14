using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OBSArrastre2026.App.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMareaCodigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "mareas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "mareas",
                type: "TEXT",
                nullable: true);
        }
    }
}
