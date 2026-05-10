using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OBSArrastre2026.App.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "especie",
                table: "registros_produccion",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "factor_conversion",
                table: "registros_produccion",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "operarios",
                table: "registros_produccion",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "especie",
                table: "registros_produccion");

            migrationBuilder.DropColumn(
                name: "factor_conversion",
                table: "registros_produccion");

            migrationBuilder.DropColumn(
                name: "operarios",
                table: "registros_produccion");
        }
    }
}
