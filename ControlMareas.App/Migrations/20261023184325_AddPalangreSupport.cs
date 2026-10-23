using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class AddPalangreSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tipo_marea",
                table: "mareas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "piezas",
                table: "items_captura",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lances_palangre",
                columns: table => new
                {
                    lance_id = table.Column<string>(type: "TEXT", nullable: false),
                    identificacion = table.Column<string>(type: "TEXT", nullable: true),
                    hora_inicio_filado = table.Column<string>(type: "TEXT", nullable: true),
                    hora_fin_filado = table.Column<string>(type: "TEXT", nullable: true),
                    latitud_inicio_filado_decimal = table.Column<double>(type: "REAL", nullable: true),
                    longitud_inicio_filado_decimal = table.Column<double>(type: "REAL", nullable: true),
                    latitud_fin_filado_decimal = table.Column<double>(type: "REAL", nullable: true),
                    longitud_fin_filado_decimal = table.Column<double>(type: "REAL", nullable: true),
                    profundidad_inicio_filado_m = table.Column<int>(type: "INTEGER", nullable: true),
                    profundidad_fin_filado_m = table.Column<int>(type: "INTEGER", nullable: true),
                    fecha_virado = table.Column<string>(type: "TEXT", nullable: true),
                    hora_inicio_virado = table.Column<string>(type: "TEXT", nullable: true),
                    hora_fin_virado = table.Column<string>(type: "TEXT", nullable: true),
                    latitud_inicio_virado_decimal = table.Column<double>(type: "REAL", nullable: true),
                    longitud_inicio_virado_decimal = table.Column<double>(type: "REAL", nullable: true),
                    latitud_fin_virado_decimal = table.Column<double>(type: "REAL", nullable: true),
                    longitud_fin_virado_decimal = table.Column<double>(type: "REAL", nullable: true),
                    profundidad_inicio_virado_m = table.Column<int>(type: "INTEGER", nullable: true),
                    profundidad_fin_virado_m = table.Column<int>(type: "INTEGER", nullable: true),
                    rumbo_virado_grados = table.Column<int>(type: "INTEGER", nullable: true),
                    tipo_palangre = table.Column<string>(type: "TEXT", nullable: true),
                    material_linea_madre = table.Column<string>(type: "TEXT", nullable: true),
                    largo_linea_madre_m = table.Column<int>(type: "INTEGER", nullable: true),
                    tramos_linea_madre = table.Column<int>(type: "INTEGER", nullable: true),
                    largo_brazolada_cm = table.Column<int>(type: "INTEGER", nullable: true),
                    separacion_brazoladas_m = table.Column<int>(type: "INTEGER", nullable: true),
                    numero_brazoladas_por_tramo = table.Column<int>(type: "INTEGER", nullable: true),
                    tipo_anzuelo = table.Column<string>(type: "TEXT", nullable: true),
                    largo_anzuelo_mm = table.Column<int>(type: "INTEGER", nullable: true),
                    ancho_anzuelo_mm = table.Column<int>(type: "INTEGER", nullable: true),
                    anzuelos_total = table.Column<int>(type: "INTEGER", nullable: true),
                    anzuelos_con_carnada = table.Column<int>(type: "INTEGER", nullable: true),
                    anzuelos_sin_carnada = table.Column<int>(type: "INTEGER", nullable: true),
                    carnada = table.Column<string>(type: "TEXT", nullable: true),
                    observaciones = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lances_palangre", x => x.lance_id);
                    table.ForeignKey(
                        name: "FK_lances_palangre_lances_lance_id",
                        column: x => x.lance_id,
                        principalTable: "lances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lances_palangre");

            migrationBuilder.DropColumn(
                name: "tipo_marea",
                table: "mareas");

            migrationBuilder.DropColumn(
                name: "piezas",
                table: "items_captura");
        }
    }
}
