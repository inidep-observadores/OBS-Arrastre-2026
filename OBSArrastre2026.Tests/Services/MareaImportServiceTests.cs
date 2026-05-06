using System.IO;
using FluentAssertions;
using NSubstitute;
using OBSArrastre2026.App.Models.Import;
using OBSArrastre2026.App.Services;
using OBSArrastre2026.App.Data;
using OBSArrastre2026.App.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OBSArrastre2026.Tests.Services;

public class MareaImportServiceTests
{
    private readonly IDbfExtractorService _extractor = Substitute.For<IDbfExtractorService>();
    private readonly IMareaReportService _report = Substitute.For<IMareaReportService>();
    private readonly IDbContextFactory<AppDbContext> _dbFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
    private readonly MareaImportService _service;

    public MareaImportServiceTests()
    {
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
        var result = await _service.ProcessMareaImportAsync(tempDir, marea);

        // Assert
        result.Should().NotBeNull();
        await _extractor.Received(1).ReadCapturasAsync(Arg.Is<string>(s => s.Contains("C10026")));
        await _report.Received(1).GenerateValidationPdfAsync(Arg.Any<MareaValidationReport>());
    }
}
