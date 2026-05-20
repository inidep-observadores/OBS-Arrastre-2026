using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataToMainTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Comentarios",
                table: "lances",
                newName: "comentarios");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "registros_produccion",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TipoMuestra",
                table: "muestras",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "muestras",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "mareas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "marea_etapas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "lances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "items_submuestras",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "items_captura",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "item_contenido_gastrico",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "frecuencias_de_tallas_con_estadio",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "frecuencias_de_tallas",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "registros_produccion");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "muestras");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "mareas");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "marea_etapas");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "items_captura");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "item_contenido_gastrico");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "frecuencias_de_tallas_con_estadio");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "frecuencias_de_tallas");

            migrationBuilder.RenameColumn(
                name: "comentarios",
                table: "lances",
                newName: "Comentarios");

            migrationBuilder.AlterColumn<int>(
                name: "TipoMuestra",
                table: "muestras",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 1);
        }
    }
}
