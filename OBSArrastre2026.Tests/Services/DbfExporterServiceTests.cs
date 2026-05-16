using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using NSubstitute;
using Microsoft.EntityFrameworkCore;
using DotNetDBF;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Models;
using OBSArrastre2026.Tests.Fixtures;

namespace OBSArrastre2026.Tests.Services;

public sealed class DbfExporterServiceTests : IDisposable
{
    private readonly DatabaseFixture _fixture;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly DbfExporterService _service;

    public DbfExporterServiceTests()
    {
        _fixture = new DatabaseFixture();
        _factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(_fixture.CreateContext()));
        
        _service = new DbfExporterService(_factory);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public async Task ExportMareaToDbfAsync_ConPesosEnPorcentaje_ExportaEnKilogramos()
    {
        // 1. Arrange: Crear datos de prueba en la base de datos en memoria
        string mareaId = "marea-test-id";
        string buqueId = "buque-test-id";
        string etapaId = "etapa-test-id";
        string lanceId = "lance-test-id";
        string especieId = "especie-test-id";

        using (var db = _fixture.CreateContext())
        {
            var buque = new Buque
            {
                Id = buqueId,
                Nombre = "TEST BARCO",
                Matricula = 1234
            };
            db.Buques.Add(buque);

            var marea = new Marea
            {
                ID = mareaId,
                NumeroInidep = 42,
                AnioInidep = 2026,
                BuqueID = buqueId,
                FechaInicio = new DateTime(2026, 5, 1)
            };
            db.Mareas.Add(marea);

            var etapa = new MareaEtapa
            {
                ID = etapaId,
                MareaID = mareaId,
                FechaZarpada = new DateTime(2026, 5, 1),
                FechaArribo = new DateTime(2026, 5, 10)
            };
            db.MareaEtapas.Add(etapa);

            var especie = new Especie
            {
                ID = especieId,
                NombreVulgar = "MERLUZA",
                CodigoInidep = "33"
            };
            db.Especies.Add(especie);

            var lance = new Lance
            {
                Id = lanceId,
                MareaEtapaId = etapaId,
                NroLance = 1,
                Fecha = "2026-05-02",
                HoraInicio = "10:00",
                HoraFinal = "12:00",
                CapturaTotalKg = 1000.0, // Peso total del lance
                DescarteTotalKg = 50.0
            };
            db.Lances.Add(lance);

            // Item de captura con tipo "Porcentaje"
            var item = new ItemCaptura
            {
                ID = "item-test-id",
                LanceID = lanceId,
                EspecieID = especieId,
                EspecieOriginal = "33",
                DatoCaptura = 10.0, // 10% de captura
                TipoDatoCaptura = TipoDatoCaptura.Porcentaje,
                DatoDescarte = 5.0, // 5% de descarte (sobre la captura de la especie, que sería 100kg -> 5kg descarte)
                TipoDatoDescarte = TipoDatoDescarte.Porcentaje,
                NumeroOrden = 1
            };
            db.ItemsCaptura.Add(item);

            await db.SaveChangesAsync();
        }

        // Obtener la marea persistida completa
        Marea fullMarea;
        using (var db = _fixture.CreateContext())
        {
            fullMarea = await db.Mareas
                .Include(m => m.Buque)
                .Include(m => m.Etapas)
                    .ThenInclude(e => e.Lances)
                        .ThenInclude(l => l.ItemsCaptura)
                .FirstAsync(m => m.ID == mareaId);
        }

        // Crear directorio temporal para la salida
        string tempOutputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempOutputDir);

        try
        {
            // 2. Act: Exportar a DBF
            await _service.ExportMareaToDbfAsync(fullMarea, tempOutputDir);

            // 3. Assert: Verificar archivos generados
            string cDbfPath = Path.Combine(tempOutputDir, "C4226.DBF");
            File.Exists(cDbfPath).Should().BeTrue();

            using var stream = File.OpenRead(cDbfPath);
            var reader = new DBFReader(stream) { CharEncoding = Encoding.GetEncoding(1252) };
            reader.RecordCount.Should().Be(1);

            int especie1Idx = -1;
            int kg1Idx = -1;
            int descar1Idx = -1;

            for (int i = 0; i < reader.Fields.Length; i++)
            {
                if (reader.Fields[i].Name == "ESPECIE_1") especie1Idx = i;
                if (reader.Fields[i].Name == "KG_1") kg1Idx = i;
                if (reader.Fields[i].Name == "DESCAR_1") descar1Idx = i;
            }

            especie1Idx.Should().BeGreaterThan(-1);
            kg1Idx.Should().BeGreaterThan(-1);
            descar1Idx.Should().BeGreaterThan(-1);

            var record = reader.NextRecord();
            
            // Especie
            Convert.ToDouble(record[especie1Idx]).Should().Be(33.0);
            
            // Peso captura: debe ser 100.0 (10% de 1000 kg)
            Convert.ToDouble(record[kg1Idx]).Should().BeApproximately(100.0, 0.001);
            
            // Peso descarte: debe ser 5.0 (5% de 100 kg)
            Convert.ToDouble(record[descar1Idx]).Should().BeApproximately(5.0, 0.001);
        }
        finally
        {
            // Limpieza
            if (Directory.Exists(tempOutputDir))
            {
                Directory.Delete(tempOutputDir, true);
            }
        }
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
