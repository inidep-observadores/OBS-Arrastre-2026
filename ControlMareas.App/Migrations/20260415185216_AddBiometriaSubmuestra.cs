using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class AddBiometriaSubmuestra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LargoEstandarMm",
                table: "items_submuestras",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LargoTotalMm",
                table: "items_submuestras",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PesoTotalGramos",
                table: "items_submuestras",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LargoEstandarMm",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "LargoTotalMm",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "PesoTotalGramos",
                table: "items_submuestras");
        }
    }
}
