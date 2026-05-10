using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OBSArrastre2026.App.Migrations
{
    /// <inheritdoc />
    public partial class AddDescarteTotalToLance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "descarte_total_kg",
                table: "lances",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "descarte_total_kg",
                table: "lances");
        }
    }
}
