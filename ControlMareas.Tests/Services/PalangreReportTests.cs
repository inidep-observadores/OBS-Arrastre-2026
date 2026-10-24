using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ControlMareas.App.Data.Entities;
using ControlMareas.App.Models;
using ControlMareas.App.Models.Reports;
using ControlMareas.App.Services;
using Xunit;

namespace ControlMareas.Tests.Services;

public class PalangreReportTests
{
    [Fact]
    public async Task ExcelReportService_Generates9Columns_ForPalangre()
    {
        // Arrange
        var service = new ExcelReportService();
        var tempFile = Path.Combine(Path.GetTempPath(), $"palangre_test_{Guid.NewGuid():N}.xlsx");

        var marea = new Marea
        {
            ID = "marea-palangre-1",
            NumeroInidep = 10,
            AnioInidep = 2026,
            TipoMarea = TipoMarea.Palangre,
            Buque = new Buque { Id = "buque-1", Nombre = "VIKINGO" }
        };

        var especie = new Especie
        {
            ID = "esp-1",
            NombreCientifico = "Dissostichus eleginoides",
            NombreVulgar = "Merluza negra",
            CodigoInidep = "7218280201"
        };

        var lance = new Lance
        {
            Id = "lance-1",
            NroLance = 1,
            Fecha = "2026-03-01",
            HoraInicio = "04:00",
            HoraFinal = "12:00",
            DetallePalangre = new LancePalangre
            {
                LanceId = "lance-1",
                HoraInicioFilado = "04:00",
                HoraFinFilado = "05:00",
                FechaVirado = "2026-03-01",
                HoraInicioVirado = "08:00",
                HoraFinVirado = "12:00",
                AnzuelosTotal = 5000,
                LatitudFinViradoDecimal = -54.5,
                LongitudFinViradoDecimal = -60.2
            },
            ItemsCaptura = new List<ItemCaptura>
            {
                new ItemCaptura
                {
                    EspecieID = "esp-1",
                    Especie = especie,
                    TipoDatoCaptura = TipoDatoCaptura.Kilogramos,
                    DatoCaptura = 2500,
                    Piezas = 250,
                    TipoDatoDescarte = TipoDatoDescarte.Kilogramos,
                    DatoDescarte = 50
                }
            }
        };

        try
        {
            // Act
            await service.GenerateTablasExcelAsync(marea, new[] { lance }, new List<RegistroProduccion>(), tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));
            using var workbook = new XLWorkbook(tempFile);

            var wsEspecies = workbook.Worksheet("Especies");
            Assert.NotNull(wsEspecies);
            // La fila 4 tiene los encabezados porque AddMetadataHeader inserta 3 filas arriba
            Assert.Equal("Especie", wsEspecies.Cell(4, 1).GetString());
            Assert.Equal("Captura (kg)", wsEspecies.Cell(4, 2).GetString());
            Assert.Equal("Piezas", wsEspecies.Cell(4, 3).GetString());
            Assert.Equal("Descarte (kg)", wsEspecies.Cell(4, 4).GetString());
            Assert.Equal("Lances", wsEspecies.Cell(4, 5).GetString());
            Assert.Equal("Días", wsEspecies.Cell(4, 6).GetString());
            Assert.Equal("Horas", wsEspecies.Cell(4, 7).GetString());
            Assert.Equal("Captura miles anzuelos", wsEspecies.Cell(4, 8).GetString());
            Assert.Equal("Piezas miles anzuelos", wsEspecies.Cell(4, 9).GetString());

            var wsAreas = workbook.Worksheet("Áreas");
            Assert.NotNull(wsAreas);
            Assert.Equal("Área", wsAreas.Cell(4, 1).GetString());
            Assert.Equal("Lances", wsAreas.Cell(4, 2).GetString());
            Assert.Equal("Días", wsAreas.Cell(4, 3).GetString());
            Assert.Equal("Horas", wsAreas.Cell(4, 4).GetString());
            Assert.Equal("Captura (kg)", wsAreas.Cell(4, 5).GetString());
            Assert.Equal("Piezas", wsAreas.Cell(4, 6).GetString());
            Assert.Equal("Descarte (kg)", wsAreas.Cell(4, 7).GetString());
            Assert.Equal("Captura miles anzuelos", wsAreas.Cell(4, 8).GetString());
            Assert.Equal("Piezas miles anzuelos", wsAreas.Cell(4, 9).GetString());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
