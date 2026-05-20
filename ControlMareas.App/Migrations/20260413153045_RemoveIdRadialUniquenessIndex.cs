using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdRadialUniquenessIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_buques_IdRadial",
                table: "buques");

            migrationBuilder.CreateIndex(
                name: "IX_buques_IdRadial",
                table: "buques",
                column: "IdRadial");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_buques_IdRadial",
                table: "buques");

            migrationBuilder.CreateIndex(
                name: "IX_buques_IdRadial",
                table: "buques",
                column: "IdRadial",
                unique: true);
        }
    }
}
