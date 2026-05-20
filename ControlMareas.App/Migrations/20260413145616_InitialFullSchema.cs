using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class InitialFullSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buques",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Matricula = table.Column<int>(type: "INTEGER", nullable: false),
                    IdRadial = table.Column<int>(type: "INTEGER", nullable: false),
                    IMO = table.Column<int>(type: "INTEGER", nullable: true),
                    MMSI = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buques", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "especies",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    CodigoInidep = table.Column<string>(type: "TEXT", nullable: true),
                    DocumentoInformativo = table.Column<string>(type: "TEXT", nullable: true),
                    Especifico = table.Column<string>(type: "TEXT", nullable: true),
                    Familia = table.Column<string>(type: "TEXT", nullable: true),
                    Frecuente = table.Column<int>(type: "INTEGER", nullable: false),
                    Genero = table.Column<string>(type: "TEXT", nullable: true),
                    NombreCientifico = table.Column<string>(type: "TEXT", nullable: true),
                    NombreVulgar = table.Column<string>(type: "TEXT", nullable: true),
                    Orden = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_especies", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    codigo = table.Column<string>(type: "TEXT", nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    categoria = table.Column<string>(type: "TEXT", nullable: false),
                    orden = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mareas",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    AnioInidep = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroInidep = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", nullable: true),
                    Comentarios = table.Column<string>(type: "TEXT", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BuqueID = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mareas", x => x.ID);
                    table.ForeignKey(
                        name: "fk_mareas_buques_buque_id",
                        column: x => x.BuqueID,
                        principalTable: "buques",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "marea_etapas",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    FechaZarpada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaArribo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MareaID = table.Column<string>(type: "TEXT", nullable: true),
                    EspecieObjetivoID = table.Column<string>(type: "TEXT", nullable: true),
                    NombreCapitan = table.Column<string>(type: "TEXT", nullable: true),
                    NombreOficialCubierta = table.Column<string>(type: "TEXT", nullable: true),
                    NombreOficialPesca = table.Column<string>(type: "TEXT", nullable: true),
                    AnioMareaBuque = table.Column<int>(type: "INTEGER", nullable: true),
                    NumeroMareaBuque = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marea_etapas", x => x.ID);
                    table.ForeignKey(
                        name: "fk_marea_etapas_especies_especie_objetivo_id",
                        column: x => x.EspecieObjetivoID,
                        principalTable: "especies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_marea_etapas_mareas_marea_id",
                        column: x => x.MareaID,
                        principalTable: "mareas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lances",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    marea_etapa_id = table.Column<string>(type: "TEXT", nullable: false),
                    nro_lance = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha = table.Column<string>(type: "TEXT", nullable: false),
                    hora_inicio = table.Column<string>(type: "TEXT", nullable: true),
                    hora_final = table.Column<string>(type: "TEXT", nullable: true),
                    latitud_inicio_decimal = table.Column<double>(type: "REAL", nullable: true),
                    longitud_inicio_decimal = table.Column<double>(type: "REAL", nullable: true),
                    latitud_final_decimal = table.Column<double>(type: "REAL", nullable: true),
                    longitud_final_decimal = table.Column<double>(type: "REAL", nullable: true),
                    profundidad_inicio_m = table.Column<int>(type: "INTEGER", nullable: true),
                    profundidad_final_m = table.Column<int>(type: "INTEGER", nullable: true),
                    estado_tiempo_codigo = table.Column<int>(type: "INTEGER", nullable: true),
                    estado_mar_codigo = table.Column<int>(type: "INTEGER", nullable: true),
                    viento_direccion_grados = table.Column<int>(type: "INTEGER", nullable: true),
                    viento_fuerza_beaufort = table.Column<int>(type: "INTEGER", nullable: true),
                    temperatura_aire_c = table.Column<double>(type: "REAL", nullable: true),
                    temperatura_red_c = table.Column<double>(type: "REAL", nullable: true),
                    presion_hpa = table.Column<int>(type: "INTEGER", nullable: true),
                    captura_total_kg = table.Column<double>(type: "REAL", nullable: true),
                    velocidad_arrastre_nudos = table.Column<double>(type: "REAL", nullable: true),
                    rumbo_grados = table.Column<int>(type: "INTEGER", nullable: true),
                    malla_copo_mm = table.Column<int>(type: "INTEGER", nullable: true),
                    malla_alas_mm = table.Column<int>(type: "INTEGER", nullable: true),
                    cable_filado_m = table.Column<int>(type: "INTEGER", nullable: true),
                    abertura_vertical_m = table.Column<double>(type: "REAL", nullable: true),
                    distancia_alas_m = table.Column<double>(type: "REAL", nullable: true),
                    profundidad_arte_m = table.Column<int>(type: "INTEGER", nullable: true),
                    selectividad_si_no = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lances", x => x.id);
                    table.ForeignKey(
                        name: "FK_lances_marea_etapas_marea_etapa_id",
                        column: x => x.marea_etapa_id,
                        principalTable: "marea_etapas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registros_produccion",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    marea_etapa_id = table.Column<string>(type: "TEXT", nullable: false),
                    fecha = table.Column<string>(type: "TEXT", nullable: false),
                    id_producto = table.Column<string>(type: "TEXT", nullable: false),
                    categoria = table.Column<string>(type: "TEXT", nullable: true),
                    kg = table.Column<double>(type: "REAL", nullable: true),
                    comentarios = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registros_produccion", x => x.id);
                    table.ForeignKey(
                        name: "FK_registros_produccion_marea_etapas_marea_etapa_id",
                        column: x => x.marea_etapa_id,
                        principalTable: "marea_etapas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_registros_produccion_productos_id_producto",
                        column: x => x.id_producto,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "items_captura",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    LanceID = table.Column<string>(type: "TEXT", nullable: true),
                    EspecieID = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroOrden = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoDatoCaptura = table.Column<int>(type: "INTEGER", nullable: false),
                    DatoCaptura = table.Column<double>(type: "REAL", nullable: false),
                    TipoDatoDescarte = table.Column<int>(type: "INTEGER", nullable: false),
                    DatoDescarte = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items_captura", x => x.ID);
                    table.ForeignKey(
                        name: "fk_items_captura_especies_especie_id",
                        column: x => x.EspecieID,
                        principalTable: "especies",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "fk_items_captura_lances_lance_id",
                        column: x => x.LanceID,
                        principalTable: "lances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "muestras",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    LanceID = table.Column<string>(type: "TEXT", nullable: true),
                    EspecieID = table.Column<string>(type: "TEXT", nullable: true),
                    Comentarios = table.Column<string>(type: "TEXT", nullable: true),
                    EjemplaresPorKg = table.Column<int>(type: "INTEGER", nullable: false),
                    Intervalo = table.Column<double>(type: "REAL", nullable: false),
                    UnidadMedidaTalla = table.Column<int>(type: "INTEGER", nullable: false),
                    ModoMedicionTalla = table.Column<int>(type: "INTEGER", nullable: false),
                    Origen = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscriminaSexo = table.Column<int>(type: "INTEGER", nullable: false),
                    HayIndeterminados = table.Column<int>(type: "INTEGER", nullable: false),
                    PesoMuestra_PesoGramos = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_muestras", x => x.ID);
                    table.ForeignKey(
                        name: "fk_muestras_especies_especie_id",
                        column: x => x.EspecieID,
                        principalTable: "especies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_muestras_lances_lance_id",
                        column: x => x.LanceID,
                        principalTable: "lances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "frecuencias_de_tallas",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    MuestraID = table.Column<string>(type: "TEXT", nullable: true),
                    Talla = table.Column<double>(type: "REAL", nullable: false),
                    NroMachos = table.Column<int>(type: "INTEGER", nullable: false),
                    NroHembras = table.Column<int>(type: "INTEGER", nullable: false),
                    NroIndeterminados = table.Column<int>(type: "INTEGER", nullable: false),
                    NroLangostinosMachoMaduros = table.Column<int>(type: "INTEGER", nullable: false),
                    NroLangostinosHembraMaduras = table.Column<int>(type: "INTEGER", nullable: false),
                    NroLangostinosHembraImpregnadas = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_frecuencias_de_tallas", x => x.ID);
                    table.ForeignKey(
                        name: "fk_frecuencias_de_tallas_muestras_muestra_id",
                        column: x => x.MuestraID,
                        principalTable: "muestras",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "frecuencias_de_tallas_con_estadio",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    MuestraID = table.Column<string>(type: "TEXT", nullable: true),
                    Talla = table.Column<double>(type: "REAL", nullable: false),
                    EstadiosHembras = table.Column<string>(type: "TEXT", nullable: true),
                    EstadiosMachos = table.Column<string>(type: "TEXT", nullable: true),
                    NroIndeterminados = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_frecuencias_de_tallas_con_estadio", x => x.ID);
                    table.ForeignKey(
                        name: "fk_frecuencias_de_tallas_con_estadio_muestras_muestra_id",
                        column: x => x.MuestraID,
                        principalTable: "muestras",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items_submuestras",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    MuestraID = table.Column<string>(type: "TEXT", nullable: true),
                    NroEjemplar = table.Column<int>(type: "INTEGER", nullable: false),
                    Sexo = table.Column<int>(type: "INTEGER", nullable: true),
                    Estadio = table.Column<int>(type: "INTEGER", nullable: true),
                    ReplecionGastrica = table.Column<int>(type: "INTEGER", nullable: true),
                    Edad = table.Column<double>(type: "REAL", nullable: true),
                    Comentarios = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items_submuestras", x => x.ID);
                    table.ForeignKey(
                        name: "fk_items_submuestras_muestras_muestra_id",
                        column: x => x.MuestraID,
                        principalTable: "muestras",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_contenido_gastrico",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    ItemSubmuestraID = table.Column<string>(type: "TEXT", nullable: true),
                    Grupo = table.Column<int>(type: "INTEGER", nullable: false),
                    Porcentaje = table.Column<double>(type: "REAL", nullable: false),
                    CantPiezas = table.Column<int>(type: "INTEGER", nullable: false),
                    Comentarios = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_contenido_gastrico", x => x.ID);
                    table.ForeignKey(
                        name: "fk_item_contenido_gastrico_items_submuestras_item_submuestra_id",
                        column: x => x.ItemSubmuestraID,
                        principalTable: "items_submuestras",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_buques_IdRadial",
                table: "buques",
                column: "IdRadial",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_buques_Nombre",
                table: "buques",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_especies_codigo_inidep",
                table: "especies",
                column: "CodigoInidep",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_frecuencias_de_tallas_muestra_id",
                table: "frecuencias_de_tallas",
                column: "MuestraID");

            migrationBuilder.CreateIndex(
                name: "ix_frecuencias_de_tallas_con_estadio_muestra_id",
                table: "frecuencias_de_tallas_con_estadio",
                column: "MuestraID");

            migrationBuilder.CreateIndex(
                name: "ix_item_contenido_gastrico_item_submuestra_id",
                table: "item_contenido_gastrico",
                column: "ItemSubmuestraID");

            migrationBuilder.CreateIndex(
                name: "ix_items_captura_especie_id",
                table: "items_captura",
                column: "EspecieID");

            migrationBuilder.CreateIndex(
                name: "ix_items_captura_lance_id",
                table: "items_captura",
                column: "LanceID");

            migrationBuilder.CreateIndex(
                name: "ix_items_submuestras_estadio",
                table: "items_submuestras",
                column: "Estadio");

            migrationBuilder.CreateIndex(
                name: "ix_items_submuestras_muestra_id",
                table: "items_submuestras",
                column: "MuestraID");

            migrationBuilder.CreateIndex(
                name: "ix_items_submuestras_sexo",
                table: "items_submuestras",
                column: "Sexo");

            migrationBuilder.CreateIndex(
                name: "idx_lances_fecha",
                table: "lances",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "idx_lances_marea_etapa_id",
                table: "lances",
                column: "marea_etapa_id");

            migrationBuilder.CreateIndex(
                name: "IX_lances_marea_etapa_id_nro_lance",
                table: "lances",
                columns: new[] { "marea_etapa_id", "nro_lance" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_marea_etapas_especie_objetivo_id",
                table: "marea_etapas",
                column: "EspecieObjetivoID");

            migrationBuilder.CreateIndex(
                name: "ix_marea_etapas_marea_id",
                table: "marea_etapas",
                column: "MareaID");

            migrationBuilder.CreateIndex(
                name: "ix_mareas_buque_id",
                table: "mareas",
                column: "BuqueID");

            migrationBuilder.CreateIndex(
                name: "ix_muestras_especie_id",
                table: "muestras",
                column: "EspecieID");

            migrationBuilder.CreateIndex(
                name: "ix_muestras_lance_id",
                table: "muestras",
                column: "LanceID");

            migrationBuilder.CreateIndex(
                name: "idx_registros_produccion_fecha",
                table: "registros_produccion",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "idx_registros_produccion_marea_etapa_id",
                table: "registros_produccion",
                column: "marea_etapa_id");

            migrationBuilder.CreateIndex(
                name: "idx_registros_produccion_producto",
                table: "registros_produccion",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_registros_produccion_marea_etapa_id_fecha_id_producto",
                table: "registros_produccion",
                columns: new[] { "marea_etapa_id", "fecha", "id_producto" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "frecuencias_de_tallas");

            migrationBuilder.DropTable(
                name: "frecuencias_de_tallas_con_estadio");

            migrationBuilder.DropTable(
                name: "item_contenido_gastrico");

            migrationBuilder.DropTable(
                name: "items_captura");

            migrationBuilder.DropTable(
                name: "registros_produccion");

            migrationBuilder.DropTable(
                name: "items_submuestras");

            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropTable(
                name: "muestras");

            migrationBuilder.DropTable(
                name: "lances");

            migrationBuilder.DropTable(
                name: "marea_etapas");

            migrationBuilder.DropTable(
                name: "especies");

            migrationBuilder.DropTable(
                name: "mareas");

            migrationBuilder.DropTable(
                name: "buques");
        }
    }
}
