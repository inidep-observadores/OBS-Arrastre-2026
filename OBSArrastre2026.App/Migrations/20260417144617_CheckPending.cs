using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OBSArrastre2026.App.Migrations
{
    /// <inheritdoc />
    public partial class CheckPending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "especie",
                table: "registros_produccion",
                newName: "especie_id");

            migrationBuilder.CreateIndex(
                name: "IX_registros_produccion_especie_id",
                table: "registros_produccion",
                column: "especie_id");

            migrationBuilder.AddForeignKey(
                name: "FK_registros_produccion_especies_especie_id",
                table: "registros_produccion",
                column: "especie_id",
                principalTable: "especies",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_registros_produccion_especies_especie_id",
                table: "registros_produccion");

            migrationBuilder.DropIndex(
                name: "IX_registros_produccion_especie_id",
                table: "registros_produccion");

            migrationBuilder.RenameColumn(
                name: "especie_id",
                table: "registros_produccion",
                newName: "especie");
        }
    }
}
