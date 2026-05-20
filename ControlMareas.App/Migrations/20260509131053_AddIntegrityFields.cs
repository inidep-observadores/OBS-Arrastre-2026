using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EspecieOriginal",
                table: "registros_produccion",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroOrden",
                table: "registros_produccion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Area",
                table: "muestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EspecieOriginal",
                table: "muestras",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FactPond",
                table: "muestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Fuente",
                table: "muestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroOrden",
                table: "muestras",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PrimTalla",
                table: "muestras",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Tarte",
                table: "muestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UltTalla",
                table: "muestras",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AreaBarrida",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ArteNro",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ArteTipo",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EdadLuna",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EstacionGral",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Estrato",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Luz",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MallaSobre",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Mus",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TmpAHum",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TmpMarS",
                table: "lances",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Area",
                table: "items_submuestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EspecieOriginal",
                table: "items_submuestras",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Fuente",
                table: "items_submuestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroOrden",
                table: "items_submuestras",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "PesoGon",
                table: "items_submuestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PesoHig",
                table: "items_submuestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PesoVac",
                table: "items_submuestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RTotal",
                table: "items_submuestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Tarte",
                table: "items_submuestras",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EspecieOriginal",
                table: "items_captura",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EspecieOriginal",
                table: "registros_produccion");

            migrationBuilder.DropColumn(
                name: "NumeroOrden",
                table: "registros_produccion");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "muestras");

            migrationBuilder.DropColumn(
                name: "EspecieOriginal",
                table: "muestras");

            migrationBuilder.DropColumn(
                name: "FactPond",
                table: "muestras");

            migrationBuilder.DropColumn(
                name: "Fuente",
                table: "muestras");

            migrationBuilder.DropColumn(
                name: "NumeroOrden",
                table: "muestras");

            migrationBuilder.DropColumn(
                name: "PrimTalla",
                table: "muestras");

            migrationBuilder.DropColumn(
                name: "Tarte",
                table: "muestras");

            migrationBuilder.DropColumn(
                name: "UltTalla",
                table: "muestras");

            migrationBuilder.DropColumn(
                name: "AreaBarrida",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "ArteNro",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "ArteTipo",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "EdadLuna",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "EstacionGral",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "Estrato",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "Luz",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "MallaSobre",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "Mus",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "TmpAHum",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "TmpMarS",
                table: "lances");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "EspecieOriginal",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "Fuente",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "NumeroOrden",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "PesoGon",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "PesoHig",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "PesoVac",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "RTotal",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "Tarte",
                table: "items_submuestras");

            migrationBuilder.DropColumn(
                name: "EspecieOriginal",
                table: "items_captura");
        }
    }
}
