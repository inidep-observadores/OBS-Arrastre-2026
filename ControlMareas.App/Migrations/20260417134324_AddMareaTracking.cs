using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlMareas.App.Migrations
{
    /// <inheritdoc />
    public partial class AddMareaTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackingPoints",
                columns: table => new
                {
                    ID = table.Column<string>(type: "TEXT", nullable: false),
                    MareaID = table.Column<string>(type: "TEXT", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Latitud = table.Column<double>(type: "REAL", nullable: false),
                    Longitud = table.Column<double>(type: "REAL", nullable: false),
                    Rumbo = table.Column<double>(type: "REAL", nullable: false),
                    Velocidad = table.Column<double>(type: "REAL", nullable: false),
                    Matricula = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingPoints", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TrackingPoints_mareas_MareaID",
                        column: x => x.MareaID,
                        principalTable: "mareas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingPoints_MareaID",
                table: "TrackingPoints",
                column: "MareaID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackingPoints");
        }
    }
}
