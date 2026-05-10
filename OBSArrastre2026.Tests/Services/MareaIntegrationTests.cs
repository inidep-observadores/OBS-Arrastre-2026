using FluentAssertions;
using NSubstitute;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.Data.Entities;
using OBSArrastre2026.App.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.IO;

namespace OBSArrastre2026.Tests.Services;

public class MareaIntegrationTests
{
    private readonly IDbfExtractorService _extractor = Substitute.For<IDbfExtractorService>();
    private readonly IMareaReportService _reportService = Substitute.For<IMareaReportService>();
    private readonly IDbContextFactory<AppDbContext> _dbFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
    private readonly MareaImportService _importService;

    public MareaIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);
        
        _dbFactory.CreateDbContextAsync().Returns(dbContext);

        _importService = new MareaImportService(_extractor, _reportService, _dbFactory);
    }

    [Fact]
    public async Task FullImportScenario_ShouldCombineFilesAndGenerateAuditReport()
    {
        // Setup temporary directory to satisfy File.Exists checks
        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        
        try
        {
            string suffix = "10026.DBF";
            File.WriteAllText(Path.Combine(tempPath, $"C{suffix}"), "");
            File.WriteAllText(Path.Combine(tempPath, $"M{suffix}"), "");
            File.WriteAllText(Path.Combine(tempPath, $"X{suffix}"), ""); // satisfy logic
            File.WriteAllText(Path.Combine(tempPath, $"S{suffix}"), "");

            // Arrange
            var capturas = new List<LegacyCaptura>
            {
                new() { Lance = 1, Barco = "TEST", Marea = 100, CaptTotal = 500, LatInic = 43.15, LongInic = 55.30 },
                new() { Lance = 2, Barco = "TEST", Marea = 100, CaptTotal = 300, ProfInic = 100, ProfFinal = 110 }
            };
            capturas[0].Especies["1"] = 500;
            capturas[1].Especies["1"] = 300;

            var muestras = new List<LegacyMuestra>
            {
                new() { Lance = 1, Barco = "TEST", Marea = 100, CodEspec = "1", PrimTalla = 10, UltTalla = 12 }
            };
            muestras[0].Tallies.Add(new DecodedTally(10, 0, 0, 0, 5));

            var muestrasX = new List<LegacyMuestra>
            {
                new() { Lance = 1, Barco = "TEST", Marea = 100, CodEspec = "1", PrimTalla = 15, UltTalla = 15 }
            };
            muestrasX[0].Tallies.Add(new DecodedTally(15, 0, 0, 0, 2));

            _extractor.ReadCapturasAsync(Arg.Any<string>()).Returns(capturas);
            _extractor.ReadMuestrasAsync(Arg.Is<string>(s => s.Contains("M10026"))).Returns(muestras);
            _extractor.ReadMuestrasAsync(Arg.Is<string>(s => s.Contains("X10026"))).Returns(muestrasX);
            _extractor.ReadSubmuestrasAsync(Arg.Any<string>()).Returns(new List<LegacySubmuestra>());
            _extractor.ReadLgAsync(Arg.Any<string>()).Returns(new List<LegacyLg>());

            _reportService.GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>()).Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

            // Act
            var marea = new Marea 
            { 
                Buque = new Buque { Nombre = "TEST" }, 
                NumeroInidep = 100, 
                AnioInidep = 2026,
                Etapas = new List<MareaEtapa>()
            };
            var result = await _importService.ProcessMareaImportAsync(tempPath, marea);


            // Assert
            result.Should().NotBeNull();
            
            var lance1Muestra = muestras.First(m => (int)m.Lance == 1);
            lance1Muestra.Tallies.Should().HaveCount(2);
            lance1Muestra.UltTalla.Should().Be(15);

            await _reportService.Received(1).GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>());
        }
        finally
        {
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task ImportScenario_WithInconsistencies_ShouldReportErrors()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        try
        {
            File.WriteAllText(Path.Combine(tempPath, "C10026.DBF"), "");

            // Arrange
            var capturas = new List<LegacyCaptura>
            {
                new() { Lance = 1, Barco = "TEST", Marea = 100, CaptTotal = 999 }
            };
            capturas[0].Especies["1"] = 500;

            _extractor.ReadCapturasAsync(Arg.Any<string>()).Returns(capturas);
            _extractor.ReadMuestrasAsync(Arg.Any<string>()).Returns(new List<LegacyMuestra>());
            _extractor.ReadSubmuestrasAsync(Arg.Any<string>()).Returns(new List<LegacySubmuestra>());
            _extractor.ReadLgAsync(Arg.Any<string>()).Returns(new List<LegacyLg>());
            
            _reportService.GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>()).Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

            // Act
            var marea = new Marea 
            { 
                Buque = new Buque { Nombre = "TEST" }, 
                NumeroInidep = 100, 
                AnioInidep = 2026,
                Etapas = new List<MareaEtapa>()
            };
            var result = await _importService.ProcessMareaImportAsync(tempPath, marea);


            // Assert
            result.Issues.Should().Contain(i => i.Category == "Captura" && i.Level == ValidationLevel.AutoFixed);
        }
        finally
        {
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        }
    }
}
