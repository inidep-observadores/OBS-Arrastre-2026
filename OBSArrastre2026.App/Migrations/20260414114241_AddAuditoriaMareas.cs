using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OBSArrastre2026.App.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaMareas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditoria_mareas_lotes",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    MareaID = table.Column<string>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", nullable: false),
                    Metadatos = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria_mareas_lotes", x => x.ID);
                    table.ForeignKey(
                        name: "fk_auditoria_mareas_lotes_mareas_marea_id",
                        column: x => x.MareaID,
                        principalTable: "mareas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auditoria_mareas_registros",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    LoteID = table.Column<string>(type: "TEXT", nullable: false),
                    Nivel = table.Column<string>(type: "TEXT", nullable: false),
                    Entidad = table.Column<string>(type: "TEXT", nullable: true),
                    EntidadID = table.Column<string>(type: "TEXT", nullable: true),
                    Mensaje = table.Column<string>(type: "TEXT", nullable: false),
                    Metadatos = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria_mareas_registros", x => x.ID);
                    table.ForeignKey(
                        name: "fk_auditoria_mareas_registros_lotes_lote_id",
                        column: x => x.LoteID,
                        principalTable: "auditoria_mareas_lotes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_mareas_lotes_marea_id",
                table: "auditoria_mareas_lotes",
                column: "MareaID");

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_mareas_registros_lote_id",
                table: "auditoria_mareas_registros",
                column: "LoteID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria_mareas_registros");

            migrationBuilder.DropTable(
                name: "auditoria_mareas_lotes");
        }
    }
}
