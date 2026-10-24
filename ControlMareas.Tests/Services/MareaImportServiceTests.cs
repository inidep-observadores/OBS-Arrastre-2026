using System.IO;
using FluentAssertions;
using NSubstitute;
using ControlMareas.App.Models.Import;
using ControlMareas.App.Services;
using ControlMareas.App.Data;
using ControlMareas.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControlMareas.Tests.Services;

public class MareaImportServiceTests
{
    private readonly IDbfExtractorService _extractor = Substitute.For<IDbfExtractorService>();
    private readonly IMareaReportService _report = Substitute.For<IMareaReportService>();
    private readonly IDbContextFactory<AppDbContext> _dbFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
    private readonly MareaImportService _service;

    public MareaImportServiceTests()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);
        
        _dbFactory.CreateDbContextAsync().Returns(dbContext);
        
        _service = new MareaImportService(_extractor, _report, _dbFactory);
    }

    [Fact]
    public async Task ProcessMareaImportAsync_ShouldOrchestrateCalls()
    {
        // Arrange
        _extractor.ReadCapturasAsync(Arg.Any<string>())
            .Returns(new List<LegacyCaptura> { new() { Lance = 1, Barco = "TEST", Marea = 100 } });
        
        _extractor.ReadMuestrasAsync(Arg.Any<string>())
            .Returns(new List<LegacyMuestra>());
        
        _extractor.ReadSubmuestrasAsync(Arg.Any<string>())
            .Returns(new List<LegacySubmuestra>());

        _report.GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>())
            .Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

        _extractor.DetectEncodingSmartAsync(Arg.Any<string>())
            .Returns(Task.FromResult(System.Text.Encoding.UTF8));

        // Act
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "C10026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "M10026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "P10026.DBF"), "");

        var marea = new Marea 
        { 
            Buque = new Buque { Nombre = "TEST" }, 
            NumeroInidep = 100, 
            AnioInidep = 2026,
            Etapas = new List<MareaEtapa>()
        };
        var selectedFiles = Directory.GetFiles(tempDir);
        var result = await _service.ProcessMareaImportAsync(tempDir, selectedFiles, marea);

        // Assert
        result.Should().NotBeNull();
        await _extractor.Received(1).ReadCapturasAsync(Arg.Is<string>(s => s.Contains("C10026")));
        await _report.Received(1).GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>());
    }

    [Fact]
    public async Task ProcessMareaImportAsync_ShouldCaptureOriginalFilenamesInMetadata()
    {
        // Arrange
        _extractor.ReadCapturasAsync(Arg.Any<string>())
            .Returns(new List<LegacyCaptura> { new() { Lance = 1, Barco = "TEST", Marea = 100 } });
        
        _extractor.ReadMuestrasAsync(Arg.Any<string>())
            .Returns(new List<LegacyMuestra>());
        
        _extractor.ReadSubmuestrasAsync(Arg.Any<string>())
            .Returns(new List<LegacySubmuestra>());

        _report.GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>())
            .Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

        _extractor.DetectEncodingSmartAsync(Arg.Any<string>())
            .Returns(Task.FromResult(System.Text.Encoding.UTF8));

        // Act
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "C0010026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "M0010026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "P0010026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "T0010026.DBF"), ""); // tracking (no debe guardarse)

        var marea = new Marea 
        { 
            Buque = new Buque { Nombre = "TEST" }, 
            NumeroInidep = 100, 
            AnioInidep = 2026,
            Etapas = new List<MareaEtapa>()
        };
        var selectedFiles = Directory.GetFiles(tempDir);
        var result = await _service.ProcessMareaImportAsync(tempDir, selectedFiles, marea);

        // Assert
        result.Should().NotBeNull();
        result.MareaMetadata.Should().NotBeNullOrEmpty();
        
        var meta = MareaMetadataHelper.GetMetadata(result.MareaMetadata);
        meta.OriginalFilenames.Should().NotBeNull();
        meta.OriginalFilenames.Should().ContainKey("C").WhoseValue.Should().Be("C0010026.DBF");
        meta.OriginalFilenames.Should().ContainKey("M").WhoseValue.Should().Be("M0010026.DBF");
        meta.OriginalFilenames.Should().ContainKey("P").WhoseValue.Should().Be("P0010026.DBF");
        meta.OriginalFilenames.Should().NotContainKey("T");
    }

    [Fact]
    public async Task ProcessMareaImportAsync_WithOrphanSubSamplesAndFlagTrue_ShouldReconstructMuestraAndFrecuencias()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var dbFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        dbFactory.CreateDbContextAsync().Returns(_ => new AppDbContext(options));
        
        var service = new MareaImportService(_extractor, _report, dbFactory);

        // Mockear extractor para que devuelva una submuestra huérfana
        _extractor.ReadCapturasAsync(Arg.Any<string>())
            .Returns(new List<LegacyCaptura> { new() { Lance = 1, Barco = "TEST", Marea = 100, Fecha = DateTime.Today } });
        
        _extractor.ReadMuestrasAsync(Arg.Any<string>())
            .Returns(new List<LegacyMuestra>()); // Sin muestras de talla en el DBF
        
        var submuestras = new List<LegacySubmuestra>
        {
            new() { Lance = 1, Especie = "MERLUZA COMUN", Sexo = 1, LargoTot = 35, PesoTot = 0.5, Barco = "TEST", Marea = 100, Fecha = DateTime.Today }, 
            new() { Lance = 1, Especie = "MERLUZA COMUN", Sexo = 2, LargoTot = 35, PesoTot = 0.6, Barco = "TEST", Marea = 100, Fecha = DateTime.Today }, 
            new() { Lance = 1, Especie = "MERLUZA COMUN", Sexo = 1, LargoTot = 36, PesoTot = 0.55, Barco = "TEST", Marea = 100, Fecha = DateTime.Today }
        };
        _extractor.ReadSubmuestrasAsync(Arg.Any<string>())
            .Returns(submuestras);

        _extractor.DetectEncodingSmartAsync(Arg.Any<string>())
            .Returns(Task.FromResult(System.Text.Encoding.UTF8));

        _report.GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>())
            .Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

        // Crear carpeta temporal
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "C10026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "M10026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "P10026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "S10026.DBF"), ""); // DBF de submuestras

        var mareaId = Guid.NewGuid().ToString();
        var etapa = new MareaEtapa 
        { 
            ID = Guid.NewGuid().ToString(),
            MareaID = mareaId,
            FechaZarpada = DateTime.Today.AddDays(-1), 
            FechaArribo = DateTime.Today.AddDays(1) 
        };

        var marea = new Marea 
        { 
            ID = mareaId,
            Buque = new Buque { Nombre = "TEST" }, 
            NumeroInidep = 100, 
            AnioInidep = 2026,
            Etapas = new List<MareaEtapa> { etapa }
        };

        var especie = new Especie 
        { 
            ID = Guid.NewGuid().ToString(),
            CodigoInidep = "12",
            NombreVulgar = "MERLUZA COMUN",
            NombreCientifico = "Merluccius hubbsi"
        };

        // Guardar la marea y la especie en la base de datos
        using (var setupContext = new AppDbContext(options))
        {
            setupContext.Mareas.Add(marea);
            setupContext.Especies.Add(especie);
            await setupContext.SaveChangesAsync();
        }

        // Act
        var selectedFiles = Directory.GetFiles(tempDir);
        var result = await service.ProcessMareaImportAsync(tempDir, selectedFiles, marea, procesarSubmuestrasSinMuestraTalla: true);

        // Assert
        result.Should().NotBeNull();
        if (result.HasFatalErrors)
        {
            var errors = string.Join("\n", result.Issues.Where(i => i.Level == ValidationLevel.Fatal).Select(i => $"{i.Category}: {i.Message}"));
            throw new Exception($"Errores fatales detectados:\n{errors}");
        }
        result.Issues.Any(i => i.Level == ValidationLevel.Warning && i.Message.Contains("Submuestra sin muestra de talla asociada")).Should().BeTrue();

        // Guardar los datos en el DbContext para verificar persistencia
        await service.ImportAsync(marea.ID, result);

        // Verificar que la Muestra fue creada en la base de datos
        using var assertContext = new AppDbContext(options);
        var muestrasEnDb = await assertContext.Muestras
            .Include(m => m.FrecuenciasTallas)
            .Include(m => m.ItemsSubmuestras)
            .ToListAsync();

        muestrasEnDb.Should().HaveCount(1);
        var muestraReconstruida = muestrasEnDb.First();
        muestraReconstruida.LanceID.Should().NotBeNull();
        muestraReconstruida.EspecieID.Should().NotBeNull();
        muestraReconstruida.TipoMuestra.Should().Be(1);
        muestraReconstruida.Comentarios.Should().Be("Muestra reconstruida automáticamente a partir de submuestra.");
        
        // Suma de pesos individuales en gramos: (0.5 + 0.6 + 0.55) kg = 1.65 kg = 1650 g
        muestraReconstruida.PesoMuestra_PesoGramos.Should().BeApproximately(1650.0, 0.001);

        // Verificar Frecuencias de talla reconstruidas
        // Talla 35: 1 macho, 1 hembra, total 2
        var frec35 = muestraReconstruida.FrecuenciasTallas.FirstOrDefault(f => f.Talla == 35);
        frec35.Should().NotBeNull();
        frec35!.NroMachos.Should().Be(1);
        frec35.NroHembras.Should().Be(1);
        frec35.NroIndeterminados.Should().Be(0);
        frec35.NroTotal.Should().Be(2);

        // Talla 36: 1 macho, 0 hembras, total 1
        var frec36 = muestraReconstruida.FrecuenciasTallas.FirstOrDefault(f => f.Talla == 36);
        frec36.Should().NotBeNull();
        frec36!.NroMachos.Should().Be(1);
        frec36.NroHembras.Should().Be(0);
        frec36.NroIndeterminados.Should().Be(0);
        frec36.NroTotal.Should().Be(1);

        // Verificar que las submuestras están correctamente vinculadas a la muestra creada
        muestraReconstruida.ItemsSubmuestras.Should().HaveCount(3);
    }

    [Fact]
    public async Task ProcessMareaImportAsync_WithOrphanSubSamplesAndFlagTrue_ShouldCalculateAlometricWeight()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var dbFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        dbFactory.CreateDbContextAsync().Returns(_ => new AppDbContext(options));
        
        var service = new MareaImportService(_extractor, _report, dbFactory);

        // Mockear extractor para que devuelva una submuestra huérfana
        _extractor.ReadCapturasAsync(Arg.Any<string>())
            .Returns(new List<LegacyCaptura> { new() { Lance = 1, Barco = "TEST", Marea = 100, Fecha = DateTime.Today } });
        
        _extractor.ReadMuestrasAsync(Arg.Any<string>())
            .Returns(new List<LegacyMuestra>()); // Sin muestras de talla en el DBF
        
        var submuestras = new List<LegacySubmuestra>
        {
            new() { Lance = 1, Especie = "MERLUZA COMUN", Sexo = 1, LargoTot = 35, PesoTot = 0.5, Barco = "TEST", Marea = 100, Fecha = DateTime.Today }, 
            new() { Lance = 1, Especie = "MERLUZA COMUN", Sexo = 2, LargoTot = 35, PesoTot = 0.6, Barco = "TEST", Marea = 100, Fecha = DateTime.Today }, 
            new() { Lance = 1, Especie = "MERLUZA COMUN", Sexo = 1, LargoTot = 36, PesoTot = 0.55, Barco = "TEST", Marea = 100, Fecha = DateTime.Today }
        };
        _extractor.ReadSubmuestrasAsync(Arg.Any<string>())
            .Returns(submuestras);

        _extractor.DetectEncodingSmartAsync(Arg.Any<string>())
            .Returns(Task.FromResult(System.Text.Encoding.UTF8));

        _report.GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>())
            .Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

        // Crear carpeta temporal
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "C10026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "M10026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "P10026.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "S10026.DBF"), "");

        var mareaId = Guid.NewGuid().ToString();
        var etapa = new MareaEtapa 
        { 
            ID = Guid.NewGuid().ToString(),
            MareaID = mareaId,
            FechaZarpada = DateTime.Today.AddDays(-1), 
            FechaArribo = DateTime.Today.AddDays(1) 
        };

        var marea = new Marea 
        { 
            ID = mareaId,
            Buque = new Buque { Nombre = "TEST" }, 
            NumeroInidep = 100, 
            AnioInidep = 2026,
            Etapas = new List<MareaEtapa> { etapa }
        };

        var especie = new Especie 
        { 
            ID = Guid.NewGuid().ToString(),
            CodigoInidep = "12",
            NombreVulgar = "MERLUZA COMUN",
            NombreCientifico = "Merluccius hubbsi"
        };

        // Relaciones largo-peso
        var especieLargoPesoMachos = new EspecieLargoPeso
        {
            Especie = especie,
            Sexo = 1,
            ParamA = 0.005,
            ParamB = 3.1
        };
        var especieLargoPesoHembras = new EspecieLargoPeso
        {
            Especie = especie,
            Sexo = 2,
            ParamA = 0.006,
            ParamB = 3.2
        };

        // Guardar la marea, la especie y sus relaciones largo-peso en la base de datos
        using (var setupContext = new AppDbContext(options))
        {
            setupContext.Mareas.Add(marea);
            setupContext.Especies.Add(especie);
            setupContext.EspeciesLargoPeso.Add(especieLargoPesoMachos);
            setupContext.EspeciesLargoPeso.Add(especieLargoPesoHembras);
            await setupContext.SaveChangesAsync();
        }

        // Act
        var selectedFiles = Directory.GetFiles(tempDir);
        var result = await service.ProcessMareaImportAsync(tempDir, selectedFiles, marea, procesarSubmuestrasSinMuestraTalla: true);

        // Guardar los datos en el DbContext para verificar persistencia
        await service.ImportAsync(marea.ID, result);

        // Verificar que la Muestra fue creada con peso alométrico estimado
        using var assertContext = new AppDbContext(options);
        var muestrasEnDb = await assertContext.Muestras
            .Include(m => m.FrecuenciasTallas)
            .ToListAsync();

        muestrasEnDb.Should().HaveCount(1);
        var muestraReconstruida = muestrasEnDb.First();
        muestraReconstruida.Automatica.Should().BeTrue();

        // Verificar el peso calculado mediante la relación alométrica largo-peso inteligente
        // Macho 35 cm: 0.005 * 35^3.1 = 306.359 gramos
        // Hembra 35 cm: 0.006 * 35^3.2 = 516.263 gramos
        // Macho 36 cm: 0.005 * 36^3.1 = 334.135 gramos
        // El total estimado en gramos es ~1163.52, pero se redondea a 2 decimales en KILOS (1.16 kg)
        // Por lo tanto, el peso final en gramos es 1160.
        muestraReconstruida.PesoMuestra_PesoGramos.Should().BeApproximately(1160.0, 0.1);

        // Ejemplares por kilogramo:
        // 3 ejemplares / (1156.757 / 1000) kg = 3 / 1.156757 kg = 2.59 ejemplares/kg -> redondeado a 3 ejemplares por kg
        muestraReconstruida.EjemplaresPorKg.Should().Be(3);
    }

    [Fact]
    public async Task ProcessMareaImportAsync_WithPalangreCPFile_ShouldImportLancesPalangreAndPiezas()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        dbFactory.CreateDbContextAsync().Returns(_ => new AppDbContext(options));

        var extractor = Substitute.For<IDbfExtractorService>();
        var reporter = Substitute.For<IMareaReportService>();
        reporter.GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>())
            .Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

        var service = new MareaImportService(extractor, reporter, dbFactory);

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "CP7726.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "M7726.DBF"), "");
        File.WriteAllText(Path.Combine(tempDir, "P7726.DBF"), "");

        var marea = new Marea
        {
            ID = Guid.NewGuid().ToString(),
            AnioInidep = 2026,
            NumeroInidep = 77,
            TipoMarea = ControlMareas.App.Models.TipoMarea.Palangre,
            FechaInicio = new DateTime(2026, 5, 20),
            FechaFin = new DateTime(2026, 6, 10),
            Buque = new Buque { Nombre = "ARGENOVA XIV" },
            Etapas = new List<MareaEtapa>
            {
                new()
                {
                    ID = Guid.NewGuid().ToString(),
                    FechaZarpada = new DateTime(2026, 5, 20),
                    FechaArribo = new DateTime(2026, 6, 10)
                }
            }
        };

        var especie = new Especie
        {
            ID = Guid.NewGuid().ToString(),
            CodigoInidep = "7218280201",
            NombreVulgar = "Bacalao de profundidad",
            NombreCientifico = "Dissostichus eleginoides"
        };

        using (var setupContext = new AppDbContext(options))
        {
            setupContext.Mareas.Add(marea);
            setupContext.Especies.Add(especie);
            await setupContext.SaveChangesAsync();
        }

        var legacyCapturaPalangre = new LegacyCapturaPalangre
        {
            Lance = 1,
            Barco = "ARGENOVA XIV",
            Marea = 77,
            Identific = "LW8316",
            Fecha = new DateTime(2026, 5, 27),
            FechaVi = new DateTime(2026, 5, 28),
            FechaFin = new DateTime(2026, 5, 28),
            HoraInic = 7.49,
            HoraFinF = 9.12,
            HoraIniV = 9.00,
            HoraFinal = 21.00,
            HoraFinV = 21.00,
            LatInic = 39.172,
            LatFinF = 39.205,
            LongInic = 54.537,
            LongFinF = 54.454,
            LatIniV = 39.182,
            LatFinal = 39.194,
            LatFinV = 39.194,
            LongIniV = 54.391,
            LongFinal = 54.444,
            LongFinV = 54.444,
            ProfIniF = 1144,
            ProfFinF = 1310,
            ProfIniV = 1150,
            ProfFinV = 1280,
            ProfInic = 1215,
            ProfFinal = 1215,
            AnzNTotal = 6000,
            Carnada = "CALAMAR",
            Material = "POLIPROPIL"
        };
        legacyCapturaPalangre.Especies["7218280201"] = 500.0;
        legacyCapturaPalangre.DescartesPorEspecie["7218280201"] = 0;
        legacyCapturaPalangre.PiezasPorEspecie["7218280201"] = 45;
        legacyCapturaPalangre.EspeciesOrder.Add("7218280201");
        legacyCapturaPalangre.EspecieColumnIndex["7218280201"] = 1;
        legacyCapturaPalangre.CaptTotal = 500.0;

        extractor.ReadCapturasPalangreAsync(Arg.Any<string>())
            .Returns(new List<LegacyCapturaPalangre> { legacyCapturaPalangre });
        extractor.ReadMuestrasAsync(Arg.Any<string>())
            .Returns(new List<LegacyMuestra>());
        extractor.ReadProduccionAsync(Arg.Any<string>())
            .Returns(new List<LegacyProduccion>());
        extractor.DetectEncodingSmartAsync(Arg.Any<string>())
            .Returns(System.Text.Encoding.GetEncoding(850));

        // Act
        var selectedFiles = Directory.GetFiles(tempDir);
        var report = await service.ProcessMareaImportAsync(tempDir, selectedFiles, marea);

        // Assert proceso de importación
        report.Should().NotBeNull();
        report.TipoMarea.Should().Be(ControlMareas.App.Models.TipoMarea.Palangre);
        report.Capturas.Should().HaveCount(1);
        report.ArchivosProcesados.Should().Contain(Path.GetFileName(Path.Combine(tempDir, "CP7726.DBF")));

        // Act 2: Guardar en base de datos
        await service.ImportAsync(marea.ID, report);

        // Assert base de datos
        using var assertContext = new AppDbContext(options);
        var lances = await assertContext.Lances
            .Include(l => l.DetallePalangre)
            .Include(l => l.ItemsCaptura)
            .ToListAsync();

        lances.Should().HaveCount(1);
        var lanceDb = lances.First();
        lanceDb.NroLance.Should().Be(1);
        lanceDb.ProfundidadInicioM.Should().Be(1215);
        lanceDb.ProfundidadFinalM.Should().Be(1215);
        lanceDb.DetallePalangre.Should().NotBeNull();
        lanceDb.DetallePalangre!.Identificacion.Should().Be("LW8316");
        lanceDb.DetallePalangre.AnzuelosTotal.Should().Be(6000);
        lanceDb.DetallePalangre.Carnada.Should().Be("CALAMAR");
        lanceDb.DetallePalangre.MaterialLineaMadre.Should().Be("POLIPROPIL");

        lanceDb.ItemsCaptura.Should().HaveCount(1);
        var itemDb = lanceDb.ItemsCaptura.First();
        itemDb.EspecieID.Should().Be(especie.ID);
        itemDb.DatoCaptura.Should().Be(500.0);
        itemDb.Piezas.Should().Be(45);
        itemDb.TipoDatoDescarte.Should().Be(ControlMareas.App.Models.TipoDatoDescarte.Kilogramos);
    }
}

