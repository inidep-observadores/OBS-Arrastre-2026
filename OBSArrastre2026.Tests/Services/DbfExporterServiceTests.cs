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

    [Fact]
    public async Task ExportMareaToDbfAsync_MuestraConAmplitudGrande_GeneraArchivoXPerfectamenteAlineado()
    {
        // 1. Arrange: Crear datos de prueba para una muestra con amplitud de tallas grande (> 90)
        string mareaId = "marea-amplitud-id";
        string buqueId = "buque-amplitud-id";
        string etapaId = "etapa-amplitud-id";
        string lanceId = "lance-amplitud-id";
        string especieId = "especie-amplitud-id";
        string muestraId = "muestra-amplitud-id";

        using (var db = _fixture.CreateContext())
        {
            var buque = new Buque
            {
                Id = buqueId,
                Nombre = "TEST BARCO 2",
                Matricula = 9999
            };
            db.Buques.Add(buque);

            var marea = new Marea
            {
                ID = mareaId,
                NumeroInidep = 43,
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
                NombreVulgar = "MERLUZA NEGRA",
                CodigoInidep = "34"
            };
            db.Especies.Add(especie);

            var lance = new Lance
            {
                Id = lanceId,
                MareaEtapaId = etapaId,
                NroLance = 2,
                Fecha = "2026-05-02",
                HoraInicio = "14:00",
                HoraFinal = "16:00",
                CapturaTotalKg = 5000.0,
                DescarteTotalKg = 200.0
            };
            db.Lances.Add(lance);

            var muestra = new Muestra
            {
                ID = muestraId,
                LanceID = lanceId,
                EspecieID = especieId,
                EspecieOriginal = "34",
                PrimTalla = 10,
                UltTalla = 110,
                Intervalo = 1,
                TipoMuestra = 1, // Muestra normal (va a M*.DBF y desborde a X*.DBF)
                PesoMuestra_PesoGramos = 25000,
                FactPond = 1.5,
                NumeroOrden = 1
            };
            db.Muestras.Add(muestra);

            // Agregar frecuencias de talla dispersas
            // Talla 10 (cabe en M, columna TALLA_1)
            db.FrecuenciasTallas.Add(new FrecuenciaTalla
            {
                ID = "ft-1",
                MuestraID = muestraId,
                Talla = 10,
                NroMachos = 5,
                NroHembras = 2,
                NroIndeterminados = 1,
                NroTotal = 8
            });

            // Talla 105 (excede límite de M de 90 columnas, debe ir al archivo X, columna TALLA_96)
            db.FrecuenciasTallas.Add(new FrecuenciaTalla
            {
                ID = "ft-2",
                MuestraID = muestraId,
                Talla = 105,
                NroMachos = 3,
                NroHembras = 4,
                NroIndeterminados = 10,
                NroTotal = 17
            });

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
                        .ThenInclude(l => l.Muestras)
                            .ThenInclude(m => m.FrecuenciasTallas)
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
            string mDbfPath = Path.Combine(tempOutputDir, "M4326.DBF");
            string xDbfPath = Path.Combine(tempOutputDir, "X4326.DBF");
            
            File.Exists(mDbfPath).Should().BeTrue();
            File.Exists(xDbfPath).Should().BeTrue("El archivo X debe generarse ya que la amplitud de tallas excede 90, aunque freqs.Count <= 90");

            // Validar archivo M
            using (var mStream = File.OpenRead(mDbfPath))
            {
                var mReader = new DBFReader(mStream) { CharEncoding = Encoding.GetEncoding(1252) };
                mReader.RecordCount.Should().Be(1);
                var mRecord = mReader.NextRecord();

                int talla1Idx = -1;
                int ultTallaMIdx = -1;
                for (int i = 0; i < mReader.Fields.Length; i++)
                {
                    if (mReader.Fields[i].Name == "TALLA_1") talla1Idx = i;
                    if (mReader.Fields[i].Name == "ULT_TALLA") ultTallaMIdx = i;
                }
                talla1Idx.Should().BeGreaterThan(-1);
                ultTallaMIdx.Should().BeGreaterThan(-1);

                // En el archivo M, ULT_TALLA debe limitarse a la columna TALLA_90 (10 + 89 * 1 = 99)
                Convert.ToDouble(mRecord[ultTallaMIdx]).Should().Be(99.0);

                // Talla 10 codificada en TALLA_1: "010005002001008" -> 10005002001008
                double expectedTalla1Value = 10005002001008.0;
                Convert.ToDouble(mRecord[talla1Idx]).Should().Be(expectedTalla1Value);
            }

            // Validar archivo X
            using (var xStream = File.OpenRead(xDbfPath))
            {
                var xReader = new DBFReader(xStream) { CharEncoding = Encoding.GetEncoding(1252) };
                xReader.RecordCount.Should().Be(1);
                var xRecord = xReader.NextRecord();

                // Verificar campos de cabecera idénticos o actualizados (PRIM_TALLA y ULT_TALLA)
                int mareaIdx = -1;
                int lanceIdx = -1;
                int especIdx = -1;
                int primTallaIdx = -1;
                int ultTallaIdx = -1;
                int talla96Idx = -1; // Corresponde al índice i = 95 -> Talla 105
                int talla91Idx = -1; // Corresponde al índice i = 90 -> Talla 100

                for (int i = 0; i < xReader.Fields.Length; i++)
                {
                    if (xReader.Fields[i].Name == "MAREA") mareaIdx = i;
                    if (xReader.Fields[i].Name == "LANCE") lanceIdx = i;
                    if (xReader.Fields[i].Name == "COD_ESPEC") especIdx = i;
                    if (xReader.Fields[i].Name == "PRIM_TALLA") primTallaIdx = i;
                    if (xReader.Fields[i].Name == "ULT_TALLA") ultTallaIdx = i;
                    if (xReader.Fields[i].Name == "TALLA_91") talla91Idx = i;
                    if (xReader.Fields[i].Name == "TALLA_96") talla96Idx = i;
                }

                mareaIdx.Should().BeGreaterThan(-1);
                lanceIdx.Should().BeGreaterThan(-1);
                especIdx.Should().BeGreaterThan(-1);
                primTallaIdx.Should().BeGreaterThan(-1);
                ultTallaIdx.Should().BeGreaterThan(-1);
                talla91Idx.Should().BeGreaterThan(-1);
                talla96Idx.Should().BeGreaterThan(-1);

                Convert.ToDouble(xRecord[mareaIdx]).Should().Be(43.0);
                Convert.ToDouble(xRecord[lanceIdx]).Should().Be(2.0);
                Convert.ToDouble(xRecord[especIdx]).Should().Be(34.0);
                
                // PRIM_TALLA en el archivo X debe ser igual a la talla correspondiente a TALLA_91 (10 + 90 * 1 = 100)
                Convert.ToDouble(xRecord[primTallaIdx]).Should().Be(100.0);

                // ULT_TALLA en el archivo X conserva el valor real total de la muestra (110)
                Convert.ToDouble(xRecord[ultTallaIdx]).Should().Be(110.0);

                // Talla 100 (i = 90) no tiene frecuencia pero está dentro del rango observado [10, 110].
                // Debe contener un tally con ceros: "100000000000000" -> 100000000000000
                Convert.ToDouble(xRecord[talla91Idx]).Should().Be(100000000000000.0);

                // Talla 105 (i = 95) en TALLA_96:
                // Machos: 3, Hembras: 4, Indeterminados: 10, Total: 17
                // Codificado: "105003004010017" -> 105003004010017
                double expectedTalla105Value = 105003004010017.0;
                Convert.ToDouble(xRecord[talla96Idx]).Should().Be(expectedTalla105Value);
            }
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
