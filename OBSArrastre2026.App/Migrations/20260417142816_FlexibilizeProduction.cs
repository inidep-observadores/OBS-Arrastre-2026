using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OBSArrastre2026.App.Migrations
{
    /// <inheritdoc />
    public partial class FlexibilizeProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_registros_produccion_marea_etapa_id_fecha_id_producto",
                table: "registros_produccion");

            migrationBuilder.DropColumn(
                name: "categoria",
                table: "productos");

            migrationBuilder.CreateIndex(
                name: "idx_registros_produccion_unico_logico",
                table: "registros_produccion",
                columns: new[] { "marea_etapa_id", "fecha", "id_producto", "categoria" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_registros_produccion_unico_logico",
                table: "registros_produccion");

            migrationBuilder.AddColumn<string>(
                name: "categoria",
                table: "productos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_registros_produccion_marea_etapa_id_fecha_id_producto",
                table: "registros_produccion",
                columns: new[] { "marea_etapa_id", "fecha", "id_producto" },
                unique: true);
        }
    }
}
